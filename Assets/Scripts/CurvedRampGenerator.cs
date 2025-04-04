using UnityEngine;
using System.Collections.Generic;
#if UNITY_EDITOR
using UnityEditor;
#endif

[ExecuteAlways]
[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer), typeof(MeshCollider))]
public class ExtrudedCurvedRampGenerator : MonoBehaviour
{
    private Mesh mesh;
    private List<Vector3> vertexList;
    private List<int> triangleList;

    [Header("Ramp Shape Settings")]
    [Tooltip("Number of segments along the arc (the 'length' of the curve). Must be > 0.")]
    public int arcSegments = 20;
    [Tooltip("Number of segments across the width. Must be > 0.")]
    public int widthSegments = 5;
    [Tooltip("Total arc angle in degrees (e.g., 90 for a quarter-circle).")]
    public float arcAngle = 90f;
    [Tooltip("Rotation offset in degrees. Adjust to change the ramp's facing direction.")]
    public float rotationOffset = 0f;
    [Tooltip("Radius from the center to the middle of the ramp.")]
    public float radius = 5f;
    [Tooltip("Total width of the ramp.")]
    public float rampWidth = 2f;

    [Header("Vertical Slope Settings")]
    [Tooltip("If true, height is computed from the slope angle; if false, a fixed max height is used.")]
    public bool useSlopeAngle = false;
    [Tooltip("Slope angle in degrees (used if useSlopeAngle is true).")]
    public float slopeAngle = 10f;
    [Tooltip("Max height of the ramp (used if useSlopeAngle is false).")]
    public float maxHeight = 3f;

    [Header("Banking (Curvy Tilt) Settings")]
    [Tooltip("Additional height at the outer edge for a banking/tilt effect. (0 = no tilt)")]
    public float bankHeight = 2f;

    [Header("Mirror Ramp")]
    [Tooltip("If true, the ramp is mirrored horizontally.")]
    public bool mirrorRamp = false;

    [Header("Extrusion Settings")]
    [Tooltip("Thickness to extrude the ramp downward.")]
    public float thickness = 0.2f;

    [Header("Side Wall Options")]
    [Tooltip("If false, side wall triangles are not generated so that only the top (and bottom) surfaces are used for collisions.")]
    public bool includeSideWalls = false;

    void OnValidate()
    {
        // Ensure we have at least 1 segment.
        arcSegments = Mathf.Max(1, arcSegments);
        widthSegments = Mathf.Max(1, widthSegments);

        MeshFilter mf = GetComponent<MeshFilter>();
        if (mf == null) return;

        if (mesh == null)
        {
            mesh = new Mesh();
            mf.sharedMesh = mesh;
        }

#if UNITY_EDITOR
        EditorApplication.delayCall -= DelayedUpdate;
        EditorApplication.delayCall += DelayedUpdate;
#else
        GenerateRamp();
        UpdateMesh();
#endif
    }

#if UNITY_EDITOR
    void DelayedUpdate()
    {
        if (this == null) return;
        if (!Application.isPlaying)
        {
            GenerateRamp();
            UpdateMesh();
        }
    }
#endif

    // In Play mode, always regenerate the mesh.
    void Start()
    {
        MeshFilter mf = GetComponent<MeshFilter>();
        mesh = new Mesh();
        mf.mesh = mesh;
        GenerateRamp();
        UpdateMesh();
    }

    void GenerateRamp()
    {
        vertexList = new List<Vector3>();
        triangleList = new List<int>();

        int rows = arcSegments + 1;
        int cols = widthSegments + 1;
        int topCount = rows * cols;

        // 1. Generate Top Surface Vertices.
        for (int i = 0; i < rows; i++)
        {
            float tArc = i / (float)arcSegments;
            float angleRad = (tArc * arcAngle + rotationOffset) * Mathf.Deg2Rad;
            float baseY = useSlopeAngle ?
                tArc * (arcAngle * Mathf.Deg2Rad * radius) * Mathf.Tan(slopeAngle * Mathf.Deg2Rad) :
                Mathf.Lerp(0f, maxHeight, tArc);

            for (int j = 0; j < cols; j++)
            {
                float tWidth = j / (float)widthSegments;
                float widthOffset = (tWidth - 0.5f) * rampWidth;
                float currentRadius = radius + widthOffset;
                float yBank = Mathf.Lerp(0f, bankHeight, tWidth);
                float y = baseY + yBank;
                float x = currentRadius * Mathf.Cos(angleRad);
                float z = currentRadius * Mathf.Sin(angleRad);
                if (mirrorRamp) { x = -x; }
                vertexList.Add(new Vector3(x, y, z));
            }
        }

        // 2. Generate Bottom Surface Vertices.
        for (int i = 0; i < topCount; i++)
        {
            Vector3 topVertex = vertexList[i];
            vertexList.Add(topVertex - Vector3.up * thickness);
        }

        // 3. Create Triangles for Top Surface.
        for (int i = 0; i < arcSegments; i++)
        {
            for (int j = 0; j < widthSegments; j++)
            {
                int rowSize = cols;
                int topLeft = i * rowSize + j;
                int bottomLeft = (i + 1) * rowSize + j;

                triangleList.Add(topLeft);
                triangleList.Add(bottomLeft);
                triangleList.Add(topLeft + 1);

                triangleList.Add(topLeft + 1);
                triangleList.Add(bottomLeft);
                triangleList.Add(bottomLeft + 1);
            }
        }

        // 4. Create Triangles for Bottom Surface (reverse winding).
        int bottomStart = topCount;
        for (int i = 0; i < arcSegments; i++)
        {
            for (int j = 0; j < widthSegments; j++)
            {
                int rowSize = cols;
                int topLeft = bottomStart + i * rowSize + j;
                int bottomLeft = bottomStart + (i + 1) * rowSize + j;

                triangleList.Add(topLeft);
                triangleList.Add(topLeft + 1);
                triangleList.Add(bottomLeft);

                triangleList.Add(topLeft + 1);
                triangleList.Add(bottomLeft + 1);
                triangleList.Add(bottomLeft);
            }
        }

        // 5. Create Side Walls (only if includeSideWalls is true).
        if (includeSideWalls)
        {
            System.Action<int, int> AddSideQuad = (int topA, int topB) =>
            {
                int botA = topA + topCount;
                int botB = topB + topCount;
                triangleList.Add(topA);
                triangleList.Add(topB);
                triangleList.Add(botA);

                triangleList.Add(botA);
                triangleList.Add(topB);
                triangleList.Add(botB);
            };

            // Front edge.
            for (int j = 0; j < cols - 1; j++)
            {
                int topA = 0 * cols + j;
                int topB = 0 * cols + j + 1;
                AddSideQuad(topA, topB);
            }
            // Back edge.
            for (int j = 0; j < cols - 1; j++)
            {
                int topA = (rows - 1) * cols + j;
                int topB = (rows - 1) * cols + j + 1;
                AddSideQuad(topA, topB);
            }
            // Left edge.
            for (int i = 0; i < rows - 1; i++)
            {
                int topA = i * cols + 0;
                int topB = (i + 1) * cols + 0;
                AddSideQuad(topA, topB);
            }
            // Right edge.
            for (int i = 0; i < rows - 1; i++)
            {
                int topA = i * cols + (cols - 1);
                int topB = (i + 1) * cols + (cols - 1);
                AddSideQuad(topA, topB);
            }
        }
    }

    void UpdateMesh()
    {
        if (mesh == null || vertexList == null || vertexList.Count == 0)
            return;

        mesh.Clear();
        mesh.SetVertices(vertexList);
        mesh.SetTriangles(triangleList, 0);
        mesh.RecalculateNormals();

        MeshCollider mc = GetComponent<MeshCollider>();
        if (mc != null)
        {
            mc.sharedMesh = null;
            mc.sharedMesh = mesh;
            mc.convex = false;
        }
    }
}
