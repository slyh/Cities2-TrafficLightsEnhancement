// Colossal.Core, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null
// Colossal.GizmosSystem
// Decompiled with ICSharpCode.Decompiler 8.1.1.7464

using System.Collections.Generic;
using System.IO;
using Colossal.Mathematics;
using Game;
using Game.Prefabs;
using Game.Rendering;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Rendering;

namespace C2VM.TrafficLightsEnhancement.Systems.Overlay
{
    public partial class RenderSystem : GameSystemBase
    {
        public const int kIconWidth = 256;

        public const int kIconHeight = 256;

        public enum Icon : uint
        {
            TrafficLightWrench = 0,

            TrafficLightLink = 1,

            TrafficLight = 2
        }

        private PrefabSystem m_PrefabSystem;

        private MaterialPropertyBlock m_Block;

        private Mesh m_LineMesh;

        private Material m_LineMaterial;

        private List<Color> m_LineColors;

        private List<int> m_LineIndices;

        private List<Vector3> m_LineVertices;

        private Mesh m_IconMesh;

        private Material m_IconMaterial;

        private uint[] m_IconArgsArray;

        private ComputeBuffer m_IconArgsBuffer;

        private ComputeBuffer m_IconComputeBuffer;

        private int m_IconComputeBufferID;

        private Texture2DArray m_IconTextureArray;

        private Bounds m_IconBounds;

        private List<NotificationIconBufferSystem.InstanceData> m_IconInstanceData;

        private Vector3 m_CameraPosition;

        protected override void OnCreate()
        {
            base.OnCreate();

            m_PrefabSystem = base.World.GetOrCreateSystemManaged<PrefabSystem>();

            m_Block = new MaterialPropertyBlock();
            m_LineColors = new(128);
            m_LineVertices = new(128);
            m_LineIndices = new(384);
            m_LineMesh = new Mesh();
            m_LineMesh.hideFlags = HideFlags.HideAndDontSave;
            m_LineMesh.indexFormat = IndexFormat.UInt32;
            m_LineMesh.MarkDynamic();
            m_LineMaterial = new Material(Shader.Find("Coherent/ViewShader"));

            IconSetup();

            RenderPipelineManager.beginContextRendering += Render;
        }

        protected unsafe void IconSetup()
        {
            m_IconInstanceData = [];
            m_IconArgsArray = new uint[5];
            m_IconArgsBuffer = new ComputeBuffer(1, m_IconArgsArray.Length * 4, ComputeBufferType.IndirectArguments);

            var iconValues = System.Enum.GetValues(typeof(Icon));
            m_IconTextureArray = new Texture2DArray(kIconWidth, kIconHeight, iconValues.Length, TextureFormat.ARGB32, mipChain: true);
            foreach (Icon icon in iconValues)
            {
                var iconTexture = new Texture2D(kIconWidth, kIconHeight);
                string imageResourceName = $"C2VM.TrafficLightsEnhancement.Resources.Textures.Icons.{System.Enum.GetName(typeof(Icon), icon)}.png";
                using Stream imageStream = System.Reflection.Assembly.GetExecutingAssembly().GetManifestResourceStream(imageResourceName);
                if (imageStream != null)
                {
                    byte[] image = new byte[imageStream.Length];
                    imageStream.Read(image, 0, image.Length);
                    ImageConversion.LoadImage(iconTexture, image);
                }
                else
                {
                    Mod.m_Log.Error($"{imageResourceName} does not exist.");
                }
                Graphics.CopyTexture(iconTexture, 0, m_IconTextureArray, (int)icon);
                Object.Destroy(iconTexture);
            }

            var configurationQuery = GetEntityQuery(ComponentType.ReadOnly<IconConfigurationData>());
            var singletonEntity = configurationQuery.GetSingletonEntity();
            var prefab = m_PrefabSystem.GetPrefab<IconConfigurationPrefab>(singletonEntity);
            m_IconMaterial = new Material(prefab.m_Material);
            m_IconMaterial.mainTexture = m_IconTextureArray;

            m_IconMesh = new Mesh();
            m_IconMesh.vertices = new Vector3[4]
            {
                new Vector3(-1f, -1f, 0f),
                new Vector3(-1f, 1f, 0f),
                new Vector3(1f, 1f, 0f),
                new Vector3(1f, -1f, 0f)
            };
            m_IconMesh.uv = new Vector2[4]
            {
                new Vector2(0f, 0f),
                new Vector2(0f, 1f),
                new Vector2(1f, 1f),
                new Vector2(1f, 0f)
            };
            m_IconMesh.triangles = new int[6] { 0, 1, 2, 2, 3, 0 };

            m_IconArgsArray[0] = m_IconMesh.GetIndexCount(0);
            m_IconArgsArray[1] = (uint)m_IconInstanceData.Count;
            m_IconArgsArray[2] = m_IconMesh.GetIndexStart(0);
            m_IconArgsArray[3] = m_IconMesh.GetBaseVertex(0);
            m_IconArgsArray[4] = 0u;

            m_IconComputeBufferID = Shader.PropertyToID("instanceBuffer");
            m_IconComputeBuffer = new ComputeBuffer(64, sizeof(NotificationIconBufferSystem.InstanceData), ComputeBufferType.Default, ComputeBufferMode.Dynamic);

            ClearIconList(); // Reset m_IconBounds
        }

        protected override void OnDestroy()
        {
            RenderPipelineManager.beginContextRendering -= Render;

            Object.Destroy(m_LineMesh);
            Object.Destroy(m_LineMaterial);

            Object.Destroy(m_IconMesh);
            Object.Destroy(m_IconMaterial);
            Object.Destroy(m_IconTextureArray);
            m_IconArgsBuffer.Release();
            m_IconComputeBuffer.Release();
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
                    if (m_IconInstanceData.Count > 0)
                    {
                        var cameraPosition = camera.transform.position;
                        if (!m_CameraPosition.Equals(cameraPosition))
                        {
                            m_CameraPosition = cameraPosition;
                            float distance = math.distance(m_IconBounds.center, m_CameraPosition);
                            for (int i = 0; i < m_IconInstanceData.Count; i++)
                            {
                                var icon = m_IconInstanceData[i];
                                icon.m_Distance = distance;
                                m_IconInstanceData[i] = icon;
                            }
                            m_IconArgsArray[1] = (uint)m_IconInstanceData.Count;
                            m_IconArgsBuffer.SetData(m_IconArgsArray);
                            m_IconComputeBuffer.SetData(m_IconInstanceData, 0, 0, m_IconInstanceData.Count);
                            m_IconMaterial.SetBuffer(m_IconComputeBufferID, m_IconComputeBuffer);
                        }
                        Graphics.DrawMeshInstancedIndirect(m_IconMesh, 0, m_IconMaterial, m_IconBounds, m_IconArgsBuffer, 0, null, ShadowCastingMode.Off, receiveShadows: false, 0, camera);
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

        public void AddBounds(Bounds3 bounds, Color color, float scale = 1f)
        {
            Vector3 centre = (bounds.min + bounds.max) / 2;
            Vector3 vector = (centre - (Vector3)bounds.min) * scale;
            Quaternion quaternion = Quaternion.AngleAxis(10, Vector3.up);
            int verticesCount = m_LineVertices.Count;
            m_LineVertices.Add(centre);
            m_LineColors.Add(color);
            for (int i = 0; i < 36; i++)
            {
                vector = quaternion * vector;
                m_LineVertices.Add(centre + vector);
                m_LineColors.Add(color);
                m_LineIndices.Add(verticesCount);
                m_LineIndices.Add(verticesCount + i);
                m_LineIndices.Add(verticesCount + (i < 35 ? i : 0) + 1);
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

        public unsafe void AddIcon(Vector3 position, Icon type)
        {
            var icon = new NotificationIconBufferSystem.InstanceData
            {
                m_Position = position,
                m_Icon = (float)type,
                m_Params = new float4(2f, 0f, 1f, 1f),
                m_Distance = 10000,
            };
            m_IconInstanceData.Add(icon);
            m_IconBounds.Encapsulate(position);
            if (m_IconInstanceData.Count >= m_IconComputeBuffer.count)
            {
                m_IconComputeBuffer.Release();
                m_IconComputeBuffer = new ComputeBuffer(m_IconInstanceData.Count * 2, sizeof(NotificationIconBufferSystem.InstanceData), ComputeBufferType.Default, ComputeBufferMode.Dynamic);
            }
            InvalidateBuffer();
        }

        public void ClearIconList()
        {
            m_IconInstanceData.Clear();
            m_IconBounds.SetMinMax(Vector3.positiveInfinity, Vector3.negativeInfinity);
        }

        public void InvalidateBuffer()
        {
            m_CameraPosition = Vector3.positiveInfinity;
        }
    }
}