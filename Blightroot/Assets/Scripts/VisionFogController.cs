using UnityEngine;

[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class VisionFogController : MonoBehaviour
{
    public static VisionFogController Instance { get; private set; }

    [Header("Visibility Settings")]
    [Tooltip("Number of rays cast in a full 360° circle")]
    public int rayCount = 128;
    [Tooltip("How far the player can see")]
    public float viewRadius = 10f;
    [Tooltip("Which layers block sight (e.g. walls)")]
    public LayerMask obstacleMask;

    [Header("Mask Rendering")]
    [Tooltip("Material used to draw the visibility mesh into the mask RT")]
    public Material maskMat;
    
    [HideInInspector]
    public RenderTexture maskTexture;

    private Mesh viewMesh;
    private Vector3[] vertices;
    private int[] triangles;

    void OnEnable()
    {
        // Initialize the mesh and mask
        viewMesh = new Mesh { name = "View Mesh" };
        GetComponent<MeshFilter>().mesh = viewMesh;
        CreateMaskTexture();
    }

    void OnDisable()
    {
        if (maskTexture != null)
        {
            maskTexture.Release();
            maskTexture = null;
        }
    }

    void LateUpdate()
    {
        BuildViewMesh();
        RenderMask();
    }

    void Awake()
    {
        // if you ever have multiple, you can warn or destroy extras
        if (Instance != null && Instance != this)
            Debug.LogWarning("Multiple FogControllers in scene!");
        Instance = this;
    }

    void BuildViewMesh()
    {
        float stepAngle = 360f / rayCount;
        vertices = new Vector3[rayCount + 1];
        triangles = new int[rayCount * 3];

        // Center at player
        vertices[0] = Vector3.zero;

        for (int i = 0; i < rayCount; i++)
        {
            float angle = stepAngle * i;
            Vector3 dir = DirFromAngle(angle);
            RaycastHit2D hit = Physics2D.Raycast(transform.position, dir, viewRadius, obstacleMask);

            Vector3 point = hit.collider != null
                ? hit.point
                : transform.position + dir * viewRadius;

            vertices[i + 1] = transform.InverseTransformPoint(point);

            int ti = i * 3;
            triangles[ti + 0] = 0;
            triangles[ti + 1] = i + 1;
            triangles[ti + 2] = (i + 2) <= rayCount ? i + 2 : 1;
        }

        viewMesh.Clear();
        viewMesh.vertices = vertices;
        viewMesh.triangles = triangles;
        viewMesh.RecalculateNormals();
    }

    void CreateMaskTexture()
    {
        maskTexture = new RenderTexture(Screen.width, Screen.height, 0, RenderTextureFormat.R8)
        {
            filterMode = FilterMode.Bilinear
        };
        maskTexture.Create();
    }

    void RenderMask()
    {
        // Resize if needed
        if (maskTexture.width != Screen.width || maskTexture.height != Screen.height)
        {
            maskTexture.Release();
            maskTexture.width = Screen.width;
            maskTexture.height = Screen.height;
            maskTexture.Create();
        }

        var prev = RenderTexture.active;
        RenderTexture.active = maskTexture;
        GL.Clear(true, true, Color.black);

        maskMat.SetPass(0);
        Graphics.DrawMeshNow(viewMesh, transform.localToWorldMatrix);

        RenderTexture.active = prev;
    }

    Vector3 DirFromAngle(float angleDeg)
    {
        float rad = angleDeg * Mathf.Deg2Rad;
        return new Vector3(Mathf.Cos(rad), Mathf.Sin(rad), 0f);
    }
}
