using UnityEngine;

[RequireComponent(typeof(MeshFilter))]
public class FieldOfViewCone : MonoBehaviourBase
{
    [Header("Field of View Settings")]
    [SerializeField, Range(30f, 120f), Tooltip("Width of the FOV cone in degrees.")]
    private float fov = 90f;

    [SerializeField, Min(0.1f), Tooltip("Length of the FOV cone.")]
    private float viewDistance = 5f;

    [SerializeField, Tooltip("Layers that block vision (e.g. walls).")]
    private LayerMask layerMask;

    private Mesh mesh;
    private Vector3 origin;
    private Vector3 aimDirection;
    private Vector3[] raycastVertices;

    protected override void Awake()
    {
        base.Awake();
        mesh = new Mesh();
        GetComponent<MeshFilter>().mesh = mesh;
        LogInfo("FieldOfViewCone initialized.");
    }

    private void LateUpdate()
    {
        origin = transform.position;

        if (TryGetComponent<Rigidbody2D>(out var rb))
        {
            Vector2 velocity = rb.linearVelocity.normalized;
            if (velocity.sqrMagnitude > 0.01f)
                aimDirection = velocity;
        }

        UpdateMesh();
    }

    private void UpdateMesh()
    {
        int rayCount = 50;
        float angle = Utils.GetAngleFromVector(aimDirection) + fov / 2f;
        float angleIncrease = fov / rayCount;

        Vector3[] vertices = new Vector3[rayCount + 2];
        Vector2[] uv = new Vector2[vertices.Length];
        int[] triangles = new int[rayCount * 3];

        vertices[0] = Vector3.zero; // relative to origin
        raycastVertices = new Vector3[rayCount + 1];

        int vertexIndex = 1;
        int triangleIndex = 0;

        for (int i = 0; i <= rayCount; i++)
        {
            Vector3 dir = Utils.GetVectorFromAngle(angle);
            RaycastHit2D hit = Physics2D.Raycast(origin, dir, viewDistance, layerMask);

            Vector3 vertex = hit.collider == null
                ? dir * viewDistance
                : (Vector3)(hit.point - (Vector2)origin);

            vertices[vertexIndex] = vertex;
            raycastVertices[i] = origin + vertex;

            if (i > 0)
            {
                triangles[triangleIndex++] = 0;
                triangles[triangleIndex++] = vertexIndex - 1;
                triangles[triangleIndex++] = vertexIndex;
            }

            vertexIndex++;
            angle -= angleIncrease;
        }

        mesh.Clear();
        mesh.vertices = vertices;
        mesh.uv = uv;
        mesh.triangles = triangles;
        mesh.RecalculateBounds();
    }

    public void SetFoV(float fov) => this.fov = fov;
    public void SetViewDistance(float viewDistance) => this.viewDistance = viewDistance;
}
