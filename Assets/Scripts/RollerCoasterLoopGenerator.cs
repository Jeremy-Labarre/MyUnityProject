using System;
using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

[ExecuteAlways]
[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer), typeof(MeshCollider))]
public class CylindricalTunnelWithFloorGaps : MonoBehaviour
{
    [Header("Tunnel Dimensions")]
    public float tubeRadius = 5f;
    public float wallThickness = 1f;
    public float tunnelLength = 20f;

    [Header("Resolution")]
    public int circleSegments = 30;
    public int lengthSegments = 10;

    [Header("Floor Gap Settings")]
    public float floorArcStart = 210f;
    public float floorArcEnd = 330f;
    public float gapProbability = 0.5f;
    public int randomSeed = 0;

    [Header("Curve Settings")]
    public float curveAngleX = 0f;  // ✅ Angle of curve (left/right)
    public float curveAngleY = 0f;  // ✅ Angle of curve (up/down)
    public float curveStrength = 1f; // ✅ Higher = smoother, Lower = sharper

    [Header("Rotation Settings")]
    public Vector3 rotationSpeed = new Vector3(0, 10f, 0);

    private Mesh mesh;
    private List<Vector3> vertices;
    private List<int> triangles;
    private List<Vector2> uvs;
    private System.Random randomInstance;

    void Update()
    {
        // ✅ Rotate tunnel only in Play Mode
        if (Application.isPlaying)
        {
            transform.Rotate(rotationSpeed * Time.deltaTime);
        }
    }

    void OnValidate()
    {
        tubeRadius = Mathf.Max(1f, tubeRadius);
        wallThickness = Mathf.Clamp(wallThickness, 0.01f, tubeRadius - 0.01f);
        tunnelLength = Mathf.Max(1f, tunnelLength);
        circleSegments = Mathf.Max(3, circleSegments);
        lengthSegments = Mathf.Max(1, lengthSegments);
        gapProbability = Mathf.Clamp01(gapProbability);
        floorArcStart = NormalizeAngle(floorArcStart);
        floorArcEnd = NormalizeAngle(floorArcEnd);

        MeshFilter mf = GetComponent<MeshFilter>();
        if (mf == null)
            return;

        if (mesh == null)
        {
            mesh = new Mesh();
            mf.sharedMesh = mesh;
        }

#if UNITY_EDITOR
        EditorApplication.delayCall -= DelayedUpdate;
        EditorApplication.delayCall += DelayedUpdate;
#else
        GenerateTunnel();
        UpdateMesh();
#endif
    }

#if UNITY_EDITOR
    void DelayedUpdate()
    {
        if (this == null) return;
        if (!Application.isPlaying)
        {
            GenerateTunnel();
            UpdateMesh();
        }
    }
#endif

    void Start()
    {
        MeshFilter mf = GetComponent<MeshFilter>();
        mesh = new Mesh();
        mf.mesh = mesh;
        GenerateTunnel();
        UpdateMesh();
    }

    void GenerateTunnel()
    {
        if (mesh == null)
            mesh = new Mesh();
        else
            mesh.Clear();

        vertices = new List<Vector3>();
        triangles = new List<int>();
        uvs = new List<Vector2>();
        randomInstance = new System.Random(randomSeed);

        float outerRadius = tubeRadius;
        float innerRadius = tubeRadius - wallThickness;
        int ringVerts = (circleSegments + 1) * 2;
        int totalSlices = lengthSegments + 1;

        for (int slice = 0; slice < totalSlices; slice++)
        {
            float tSlice = slice / (float)lengthSegments;
            float z = tSlice * tunnelLength;

            // ✅ Apply Curving Using Angles & Strength
            float curveFactor = Mathf.Pow(tSlice, curveStrength);
            float curveX = Mathf.Sin(curveAngleX * Mathf.Deg2Rad) * z * curveFactor;
            float curveY = Mathf.Sin(curveAngleY * Mathf.Deg2Rad) * z * curveFactor;

            for (int i = 0; i <= circleSegments; i++)
            {
                float angleDeg = (i / (float)circleSegments) * 360f;
                float angleRad = angleDeg * Mathf.Deg2Rad;

                Vector3 outerVertex = new Vector3(
                    outerRadius * Mathf.Cos(angleRad) + curveX,
                    outerRadius * Mathf.Sin(angleRad) + curveY,
                    z
                );

                Vector3 innerVertex = new Vector3(
                    innerRadius * Mathf.Cos(angleRad) + curveX,
                    innerRadius * Mathf.Sin(angleRad) + curveY,
                    z
                );

                vertices.Add(outerVertex);
                vertices.Add(innerVertex);

                float uvX = (float)i / circleSegments;
                float uvY = (float)slice / lengthSegments;
                uvs.Add(new Vector2(uvX, uvY));
                uvs.Add(new Vector2(uvX, uvY));
            }
        }

        for (int slice = 0; slice < lengthSegments; slice++)
        {
            int sliceBase = slice * ringVerts;
            int nextSliceBase = (slice + 1) * ringVerts;

            for (int i = 0; i < circleSegments; i++)
            {
                float segCenterAngle = ((i + 0.5f) / circleSegments) * 360f;
                bool inFloor = IsAngleInRange(segCenterAngle, floorArcStart, floorArcEnd);
                bool skip = inFloor && slice > 0 && (randomInstance.NextDouble() < gapProbability);

                if (skip)
                    continue;

                int outerCurrent = sliceBase + i * 2;
                int innerCurrent = sliceBase + i * 2 + 1;
                int outerNext = sliceBase + ((i + 1) % (circleSegments + 1)) * 2;
                int innerNext = sliceBase + ((i + 1) % (circleSegments + 1)) * 2 + 1;

                int outerCurrentNext = nextSliceBase + i * 2;
                int innerCurrentNext = nextSliceBase + i * 2 + 1;
                int outerNextNext = nextSliceBase + ((i + 1) % (circleSegments + 1)) * 2;
                int innerNextNext = nextSliceBase + ((i + 1) % (circleSegments + 1)) * 2 + 1;

                triangles.AddRange(new int[]
                {
                    outerCurrent, outerCurrentNext, outerNext,
                    outerNext, outerCurrentNext, outerNextNext,

                    innerCurrent, innerNext, innerCurrentNext,
                    innerNext, innerNextNext, innerCurrentNext,

                    outerCurrent, innerNext, innerCurrent,
                    outerCurrent, outerNext, innerNext
                });
            }
        }

        UpdateMesh();
    }

    void UpdateMesh()
    {
        mesh.Clear();
        mesh.SetVertices(vertices);
        mesh.SetTriangles(triangles, 0);
        mesh.SetUVs(0, uvs);
        mesh.RecalculateNormals();

        MeshCollider mc = GetComponent<MeshCollider>();
        if (mc != null && mc.sharedMesh != mesh)
        {
            mc.sharedMesh = null;
            mc.sharedMesh = mesh;
        }
    }

    float NormalizeAngle(float angle)
    {
        angle %= 360f;
        return angle < 0f ? angle + 360f : angle;
    }

    bool IsAngleInRange(float angle, float start, float end)
    {
        angle = NormalizeAngle(angle);
        start = NormalizeAngle(start);
        end = NormalizeAngle(end);
        return start < end ? (angle >= start && angle <= end) : (angle >= start || angle <= end);
    }
}
