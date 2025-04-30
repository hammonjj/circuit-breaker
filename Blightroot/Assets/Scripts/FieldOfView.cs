using UnityEngine;
using System.Collections.Generic;

[RequireComponent(typeof(MeshFilter))]
public class FieldOfView : MonoBehaviour
{
    [SerializeField] private LayerMask layerMask;
    [SerializeField] private float width = 10f;
    [SerializeField] private float height = 5f;
    [SerializeField] private float viewDistance = 10f;
    [SerializeField] private int rayResolution = 5; // Rays per edge
    [SerializeField] private bool drawDebug = true;

    private Mesh mesh;
    private Vector3 forward = Vector3.right;

    private void Start()
    {
        mesh = new Mesh();
        GetComponent<MeshFilter>().mesh = mesh;
    }

    private void LateUpdate()
    {
        Vector3 origin = transform.position;
        Vector3 dir = forward.normalized;

        Vector3 right = new Vector3(dir.y, -dir.x).normalized;
        Vector3 up = new Vector3(-right.y, right.x).normalized;

        Vector3 center = origin + dir * (viewDistance / 2f);
        Vector3 halfWidth = right * (width / 2f);
        Vector3 halfHeight = up * (height / 2f);

        // Rectangle corners
        Vector3 tl = center - halfWidth + halfHeight;
        Vector3 tr = center + halfWidth + halfHeight;
        Vector3 br = center + halfWidth - halfHeight;
        Vector3 bl = center - halfWidth - halfHeight;

        // Interpolate perimeter points clockwise
        List<Vector3> perimeter = new();
        AddInterpolated(perimeter, tl, tr, rayResolution);
        AddInterpolated(perimeter, tr, br, rayResolution);
        AddInterpolated(perimeter, br, bl, rayResolution);
        AddInterpolated(perimeter, bl, tl, rayResolution);

        // Raycast to each perimeter point
        List<Vector3> finalPoints = new() { origin };
        foreach (var target in perimeter)
        {
            var hit = RaycastLimit(origin, target);
            finalPoints.Add(hit);
            if (drawDebug) Debug.DrawLine(origin, hit, Color.magenta);
        }

        // Build triangle fan mesh
        Vector3[] vertices = new Vector3[finalPoints.Count];
        int[] triangles = new int[(finalPoints.Count - 2) * 3];

        for (int i = 0; i < finalPoints.Count; i++)
            vertices[i] = transform.InverseTransformPoint(finalPoints[i]);

        int triIndex = 0;
        for (int i = 1; i < finalPoints.Count - 1; i++)
        {
            triangles[triIndex++] = 0;
            triangles[triIndex++] = i;
            triangles[triIndex++] = i + 1;
        }

        mesh.Clear();
        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.RecalculateBounds();
    }

    private void AddInterpolated(List<Vector3> list, Vector3 a, Vector3 b, int segments)
    {
        for (int i = 0; i <= segments; i++)
        {
            float t = i / (float)segments;
            list.Add(Vector3.Lerp(a, b, t));
        }
    }

    private Vector3 RaycastLimit(Vector3 from, Vector3 to)
    {
        Vector3 dir = (to - from).normalized;
        float dist = Vector3.Distance(from, to);
        RaycastHit2D hit = Physics2D.Raycast(from, dir, dist, layerMask);
        return hit.collider ? hit.point : to;
    }

    public void SetAimDirection(Vector3 aimDirection)
    {
        forward = aimDirection.normalized;
    }

    public void SetViewDistance(float distance)
    {
        viewDistance = distance;
    }
}
