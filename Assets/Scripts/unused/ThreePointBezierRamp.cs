using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class ThreePointBezierRamp : MonoBehaviour
{
    [Header("Bezier Points")]
    [Tooltip("Start corner of the curve.")]
    public Vector3 startPoint = new Vector3(0f, 0f, 0f);
    [Tooltip("Middle corner of the curve.")]
    public Vector3 midPoint = new Vector3(2f, 0f, 5f);
    [Tooltip("End corner of the curve.")]
    public Vector3 endPoint = new Vector3(5f, 0f, 10f);

    [Header("Mesh Settings")]
    [Tooltip("Number of segments used to sample the curve (higher = smoother).")]
    public int resolution = 20;
    [Tooltip("Width of the generated ramp or ribbon.")]
    public float width = 2f;

    // We'll store the generated mesh here
    private Mesh rampMesh;

    void Start()
    {
        // Create a new mesh and assign it to our MeshFilter
        rampMesh = new Mesh();
        GetComponent<MeshFilter>().mesh = rampMesh;

        GenerateRamp();
    }

    void GenerateRamp()
    {
        // 1. Sample the Quadratic Bezier curve
        List<Vector3> curvePoints = new List<Vector3>();
        for (int i = 0; i <= resolution; i++)
        {
            float t = i / (float)resolution;
            // Quadratic Bezier formula:
            // B(t) = (1 - t)^2 * P0 + 2(1 - t)t * P1 + t^2 * P2
            Vector3 point = CalculateQuadraticBezierPoint(t, startPoint, midPoint, endPoint);
            curvePoints.Add(point);
        }

        // 2. Build the vertices (left/right) and UVs
        List<Vector3> vertices = new List<Vector3>();
        List<Vector2> uvs = new List<Vector2>();
        int count = curvePoints.Count;

        for (int i = 0; i < count; i++)
        {
            Vector3 center = curvePoints[i];

            // Determine the forward direction along the curve
            Vector3 forward;
            if (i < count - 1)
                forward = (curvePoints[i + 1] - center).normalized;
            else
                forward = (center - curvePoints[i - 1]).normalized;

            // Calculate a perpendicular vector (assuming Y-up)
            Vector3 leftDir = Vector3.Cross(Vector3.up, forward).normalized;
            Vector3 rightDir = -leftDir;

            // Create left/right vertices
            Vector3 leftVertex = center + leftDir * (width * 0.5f);
            Vector3 rightVertex = center + rightDir * (width * 0.5f);

            vertices.Add(leftVertex);
            vertices.Add(rightVertex);

            // UV coordinates: u = 0 or 1 (left/right), v = i/(count-1)
            float vCoord = i / (float)(count - 1);
            uvs.Add(new Vector2(0f, vCoord));  // left
            uvs.Add(new Vector2(1f, vCoord));  // right
        }

        // 3. Create triangles between consecutive pairs of vertices
        List<int> triangles = new List<int>();
        for (int i = 0; i < count - 1; i++)
        {
            int index = i * 2;
            // First triangle
            triangles.Add(index);
            triangles.Add(index + 2);
            triangles.Add(index + 1);

            // Second triangle
            triangles.Add(index + 1);
            triangles.Add(index + 2);
            triangles.Add(index + 3);
        }

        // 4. Assign data to the mesh
        rampMesh.Clear();
        rampMesh.vertices = vertices.ToArray();
        rampMesh.triangles = triangles.ToArray();
        rampMesh.uv = uvs.ToArray();
        rampMesh.RecalculateNormals();

        Debug.Log("Ramp generated with " + rampMesh.vertexCount + " vertices, "
            + (rampMesh.triangles.Length / 3) + " triangles.");
    }

    // Quadratic Bezier calculation
    Vector3 CalculateQuadraticBezierPoint(float t, Vector3 p0, Vector3 p1, Vector3 p2)
    {
        float u = 1f - t;
        float tt = t * t;
        float uu = u * u;

        // (1 - t)^2 * p0 + 2(1 - t)*t * p1 + t^2 * p2
        Vector3 point = uu * p0;
        point += 2f * u * t * p1;
        point += tt * p2;
        return point;
    }
}
