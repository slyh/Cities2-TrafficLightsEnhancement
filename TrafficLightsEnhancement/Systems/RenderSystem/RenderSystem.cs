// Colossal.Core, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null
// Colossal.GizmosSystem
// Decompiled with ICSharpCode.Decompiler 8.1.1.7464

using System.Collections.Generic;
using Colossal.Mathematics;
using Game;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Rendering;

namespace C2VM.TrafficLightsEnhancement.Systems.RenderSystem;

public partial class RenderSystem : GameSystemBase
{
    private MaterialPropertyBlock m_Block;

    private Mesh m_Mesh;

    private Material m_Material;

    private List<Color> m_Colors;

    private List<int> m_Indices;

    private List<Vector3> m_Vertices;

    protected override void OnCreate()
    {
        base.OnCreate();
        m_Colors = new(128);
        m_Indices = new(384);
        m_Vertices = new(128);
        m_Block = new MaterialPropertyBlock();
        m_Mesh = new Mesh();
        m_Mesh.hideFlags = HideFlags.HideAndDontSave;
        m_Mesh.indexFormat = IndexFormat.UInt32;
        m_Mesh.MarkDynamic();
        m_Material = new Material(Shader.Find("Coherent/ViewShader"));
        RenderPipelineManager.beginContextRendering += Render;
    }

    protected override void OnDestroy()
    {
        RenderPipelineManager.beginContextRendering -= Render;
    }

    protected override void OnUpdate()
    {
    }

    private void Render(ScriptableRenderContext context, List<Camera> cameras)
    {
        foreach (Camera camera in cameras)
        {
            if (camera.cameraType == CameraType.Game && m_Mesh.subMeshCount > 0)
            {
                Graphics.DrawMesh(m_Mesh, Matrix4x4.identity, m_Material, 0, camera, 0, m_Block, castShadows: false, receiveShadows: false);
            }
        }
    }

    public void AddBezier(Bezier4x3 bezier, Color color, float length = 1f, float thickness = 0.25f)
    {
        int segmentCount = (int)math.min(math.max(4f, length * 2f), 16f);
        int maxSegmentIndex = segmentCount - 1;
        int verticesCount = m_Vertices.Count;
        Vector3 p1 = new(), p2 = new(), v1 = new(), v2 = new();
        for (int i = 0; i < maxSegmentIndex; i++)
        {
            p1 = MathUtils.Position(bezier, (float)i / maxSegmentIndex);
            p2 = MathUtils.Position(bezier, (float)(i + 1) / maxSegmentIndex);
            v1 = Quaternion.AngleAxis(-90, Vector3.up) * (p2 - p1).normalized * thickness;
            v2 = Quaternion.AngleAxis(90, Vector3.up) * (p2 - p1).normalized * thickness;
            m_Vertices.Add(p1 + v1);
            m_Vertices.Add(p1 + v2);
            m_Colors.Add(color);
            m_Colors.Add(color);
        }
        m_Vertices.Add(p2 + v1);
        m_Vertices.Add(p2 + v2);
        m_Colors.Add(color);
        m_Colors.Add(color);
        for (int i = 0; i < maxSegmentIndex; i++)
        {
            m_Indices.Add(verticesCount + i * 2);
            m_Indices.Add(verticesCount + i * 2 + 1);
            m_Indices.Add(verticesCount + i * 2 + 2);
            m_Indices.Add(verticesCount + i * 2 + 1);
            m_Indices.Add(verticesCount + i * 2 + 2);
            m_Indices.Add(verticesCount + i * 2 + 3);
        }
    }

    public void BuildMesh()
    {
        m_Mesh.Clear();
        m_Mesh.SetVertices(m_Vertices, 0, m_Vertices.Count);
        m_Mesh.SetColors(m_Colors, 0, m_Colors.Count);
        m_Mesh.SetIndices(m_Indices, 0, m_Indices.Count, MeshTopology.Triangles, 0);
    }

    public void ClearMesh()
    {
        m_Mesh.Clear();
        m_Colors.Clear();
        m_Indices.Clear();
        m_Vertices.Clear();
    }
}