// Colossal.Core, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null
// Colossal.GizmosSystem
// Decompiled with ICSharpCode.Decompiler 8.1.1.7464

using System.Collections.Generic;
using System.IO;
using Colossal.Mathematics;
using Game;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Rendering;

namespace C2VM.TrafficLightsEnhancement.Systems.Rendering;

public partial class RenderSystem : GameSystemBase
{
    public enum Icon : uint
    {
        TrafficLightWrench = 0,

        TrafficLightLink = 1,

        TrafficLight = 2
    }

    private MaterialPropertyBlock m_Block;

    private Mesh m_LineMesh;

    private Material m_LineMaterial;

    private List<Color> m_LineColors;

    private List<int> m_LineIndices;

    private List<Vector3> m_LineVertices;

    private Mesh m_IconMesh;

    private Material m_IconMaterial;

    private Texture2D m_IconTexture;

    private List<int> m_IconIndices;

    private List<Vector3> m_IconVertices;

    private List<Vector2> m_IconUVs;

    protected override void OnCreate()
    {
        base.OnCreate();
        m_Block = new MaterialPropertyBlock();

        m_LineColors = new(128);
        m_LineVertices = new(128);
        m_LineIndices = new(384);
        m_LineMesh = new Mesh();
        m_LineMesh.hideFlags = HideFlags.HideAndDontSave;
        m_LineMesh.indexFormat = IndexFormat.UInt32;
        m_LineMesh.MarkDynamic();
        m_LineMaterial = new Material(Shader.Find("Coherent/ViewShader"));

        m_IconVertices = new(128);
        m_IconIndices = new(384);
        m_IconUVs = new(384);
        m_IconMesh = new Mesh();
        m_IconMesh.hideFlags = HideFlags.HideAndDontSave;
        m_IconMesh.indexFormat = IndexFormat.UInt32;
        m_IconMesh.MarkDynamic();
        m_IconTexture = new(768, 256);
        string imageResourceName = "C2VM.TrafficLightsEnhancement.Resources.Textures.TrafficLightIcons.png";
        using Stream imageStream = System.Reflection.Assembly.GetExecutingAssembly().GetManifestResourceStream(imageResourceName);
        if (imageStream != null)
        {
            byte[] image = new byte[imageStream.Length];
            imageStream.Read(image, 0, image.Length);
            ImageConversion.LoadImage(m_IconTexture, image);
        }
        else
        {
            Mod.m_Log.Error($"{imageResourceName} does not exist.");
        }
        m_IconMaterial = new Material(Shader.Find("Coherent/ViewShader"));
        m_IconMaterial.SetTexture("_MainTex", m_IconTexture);

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
            if (camera.cameraType == CameraType.Game)
            {
                if (m_LineMesh.subMeshCount > 0)
                {
                    Graphics.DrawMesh(m_LineMesh, Matrix4x4.identity, m_LineMaterial, 0, camera, 0, m_Block, castShadows: false, receiveShadows: false);
                }
                if (m_IconMesh.subMeshCount > 0)
                {
                    Graphics.DrawMesh(m_IconMesh, Matrix4x4.identity, m_IconMaterial, 0, camera, 0, m_Block, castShadows: false, receiveShadows: false);
                }
            }
        }
    }

    public void AddBezier(Bezier4x3 bezier, Color color, float length = 1f, float thickness = 0.25f)
    {
        int segmentCount = (int)math.min(math.max(4f, length * 2f), 16f);
        int maxSegmentIndex = segmentCount - 1;
        int verticesCount = m_LineVertices.Count;
        Vector3 p1 = new(), p2 = new(), v1 = new(), v2 = new();
        for (int i = 0; i < maxSegmentIndex; i++)
        {
            p1 = MathUtils.Position(bezier, (float)i / maxSegmentIndex);
            p2 = MathUtils.Position(bezier, (float)(i + 1) / maxSegmentIndex);
            v1 = Quaternion.AngleAxis(-90, Vector3.up) * (p2 - p1).normalized * thickness;
            v2 = v1 * -1.0f;
            m_LineVertices.Add(p1 + v1);
            m_LineVertices.Add(p1 + v2);
            m_LineColors.Add(color);
            m_LineColors.Add(color);
        }
        m_LineVertices.Add(p2 + v1);
        m_LineVertices.Add(p2 + v2);
        m_LineColors.Add(color);
        m_LineColors.Add(color);
        for (int i = 0; i < maxSegmentIndex; i++)
        {
            m_LineIndices.Add(verticesCount + i * 2);
            m_LineIndices.Add(verticesCount + i * 2 + 1);
            m_LineIndices.Add(verticesCount + i * 2 + 2);
            m_LineIndices.Add(verticesCount + i * 2 + 1);
            m_LineIndices.Add(verticesCount + i * 2 + 2);
            m_LineIndices.Add(verticesCount + i * 2 + 3);
        }
    }

    public void BuildLineMesh()
    {
        m_LineMesh.Clear();
        m_LineMesh.SetVertices(m_LineVertices, 0, m_LineVertices.Count);
        m_LineMesh.SetColors(m_LineColors, 0, m_LineColors.Count);
        m_LineMesh.SetIndices(m_LineIndices, 0, m_LineIndices.Count, MeshTopology.Triangles, 0);
    }

    public void ClearLineMesh()
    {
        m_LineMesh.Clear();
        m_LineColors.Clear();
        m_LineIndices.Clear();
        m_LineVertices.Clear();
    }

    public void AddIcon(Vector3 position, Quaternion rotation, Icon type)
    {
        int verticesCount = m_IconVertices.Count;
        Quaternion finalRotation = rotation * Quaternion.Euler(0, 45, 0);
        Vector3 topLeft = finalRotation * new Vector3(-5f, 0, 5f);
        Vector3 topRight = finalRotation * new Vector3(5f, 0, 5f);
        Vector3 bottomLeft = topRight * -1;
        Vector3 bottomRight = topLeft * -1;
        m_IconVertices.Add(position + topLeft);
        m_IconVertices.Add(position + topRight);
        m_IconVertices.Add(position + bottomLeft);
        m_IconVertices.Add(position + bottomRight);
        m_IconIndices.Add(verticesCount);
        m_IconIndices.Add(verticesCount + 1);
        m_IconIndices.Add(verticesCount + 2);
        m_IconIndices.Add(verticesCount + 1);
        m_IconIndices.Add(verticesCount + 2);
        m_IconIndices.Add(verticesCount + 3);
        m_IconUVs.Add(new Vector2((float)type * 0.3333f, 0f));
        m_IconUVs.Add(new Vector2(((float)type + 1f) * 0.3333f, 0f));
        m_IconUVs.Add(new Vector2((float)type * 0.3333f, 1f));
        m_IconUVs.Add(new Vector2(((float)type + 1f) * 0.3333f, 1f));
    }

    public void BuildIconMesh()
    {
        m_IconMesh.Clear();
        m_IconMesh.SetVertices(m_IconVertices, 0, m_IconVertices.Count);
        m_IconMesh.SetIndices(m_IconIndices, 0, m_IconIndices.Count, MeshTopology.Triangles, 0);
        m_IconMesh.SetUVs(0, m_IconUVs);
    }

    public void ClearIconMesh()
    {
        m_IconMesh.Clear();
        m_IconVertices.Clear();
        m_IconIndices.Clear();
        m_IconUVs.Clear();
    }
}