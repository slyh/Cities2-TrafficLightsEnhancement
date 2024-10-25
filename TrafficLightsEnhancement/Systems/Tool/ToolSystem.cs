using C2VM.TrafficLightsEnhancement.Systems.Rendering;
using C2VM.TrafficLightsEnhancement.Systems.UI;
using Colossal.Entities;
using Game.Net;
using Game.Prefabs;
using Game.Tools;
using HarmonyLib;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;

namespace C2VM.TrafficLightsEnhancement.Systems.Tool;

public partial class ToolSystem : NetToolSystem
{
    public override string toolID => "C2VMTLE Tool";

    private RenderSystem m_RenderSystem;

    private UISystem m_UISystem;

    private NativeList<ControlPoint> m_ParentControlPoints;

    private Entity m_AssetEntity = Entity.Null;

    private Entity m_RaycastResult = Entity.Null;

    public bool m_Suspended { get; private set; }

    protected override void OnCreate()
    {
        base.OnCreate();
        m_RenderSystem = World.GetOrCreateSystemManaged<RenderSystem>();
        m_UISystem = World.GetOrCreateSystemManaged<UISystem>();
        m_ParentControlPoints = GetControlPoints(out JobHandle _);
        m_ToolSystem.EventToolChanged += ToolChanged;
    }

    protected override JobHandle OnUpdate(JobHandle inputDeps)
    {
        if (m_Suspended)
        {
            m_ToolRaycastSystem.raycastFlags |= Game.Common.RaycastFlags.UIDisable;
        }
        var result = base.OnUpdate(inputDeps);
        if (!m_Suspended && (m_ToolRaycastSystem.raycastFlags & Game.Common.RaycastFlags.UIDisable) == 0)
        {
            if (m_ParentControlPoints.Length >= 4 && m_ParentControlPoints[m_ParentControlPoints.Length - 3].m_OriginalEntity == m_ParentControlPoints[m_ParentControlPoints.Length - 2].m_OriginalEntity)
            {
                Entity originalEntity = m_ParentControlPoints[m_ParentControlPoints.Length - 3].m_OriginalEntity;
                if (originalEntity != m_RaycastResult)
                {
                    m_RaycastResult = originalEntity;
                    m_RenderSystem.ClearLineMesh();
                    if (!EntityManager.HasComponent<Roundabout>(m_RaycastResult) && EntityManager.TryGetComponent<NodeGeometry>(m_RaycastResult, out var nodeGeometry))
                    {
                        m_RenderSystem.AddBounds(nodeGeometry.m_Bounds, new UnityEngine.Color(0.2941f, 0.7647f, 0.9451f, 1.0f), 0.5f);
                        m_RenderSystem.BuildLineMesh();
                    }
                }
            }
            else
            {
                m_RaycastResult = Entity.Null;
                m_RenderSystem.ClearLineMesh();
            }
            if (applyAction.action.WasReleasedThisFrame())
            {
                Entity entity = Traverse.Create(this).Field("m_AppliedUpgrade").Property("value").Field("m_Entity").GetValue<Entity>();
                CompositionFlags flags = Traverse.Create(this).Field("m_AppliedUpgrade").Property("value").Field("m_Flags").GetValue<CompositionFlags>();
                if (entity != Entity.Null && (flags.m_General & CompositionFlags.General.TrafficLights) != 0)
                {
                    m_UISystem.ChangeSelectedEntity(entity);
                }
            }
        }
        return result;
    }

    protected override void OnGameLoadingComplete(Colossal.Serialization.Entities.Purpose purpose, Game.GameMode mode)
    {
        NativeArray<Entity> placeablePrefabsList = GetEntityQuery(ComponentType.ReadOnly<PlaceableNetData>()).ToEntityArray(Allocator.Temp);
        m_AssetEntity = Entity.Null;
        for (int i = 0; i < placeablePrefabsList.Length; i++)
        {
            Entity entity = placeablePrefabsList[i];
            PlaceableNetData placeableNetData = EntityManager.GetComponentData<PlaceableNetData>(entity);
            if ((placeableNetData.m_SetUpgradeFlags.m_General & CompositionFlags.General.TrafficLights) != 0)
            {
                m_AssetEntity = entity;
                break;
            }
        }
        if (m_AssetEntity == Entity.Null)
        {
            Mod.m_Log.Error($"Traffic lights prefab asset entity not found. The tool system will not work.");
        }
    }

    protected override bool GetAllowApply()
    {
        return !(secondaryApplyAction.action.WasReleasedThisFrame() || secondaryApplyAction.action.WasPressedThisFrame());
    }

    public override bool TrySetPrefab(PrefabBase prefab)
    {
        return false;
    }

    public override PrefabBase GetPrefab()
    {
        return null;
    }

    public void Enable()
    {
        PrefabBase prefab = m_PrefabSystem.GetPrefab<PrefabBase>(m_AssetEntity);
        if (prefab is NetPrefab netPrefab)
        {
            m_Suspended = false;
            this.prefab = netPrefab;
            this.underground = m_ToolSystem.activeTool.requireUnderground;
            m_ToolSystem.activeTool = this;
        }
    }

    public void Suspend()
    {
        m_Suspended = true;
    }

    public void Disable()
    {
        if (m_ToolSystem.activeTool == this)
        {
            m_ToolSystem.activeTool = m_DefaultToolSystem;
        }
    }

    private void ToolChanged(ToolBaseSystem system)
    {
        if (system != this)
        {
            m_UISystem.SetMainPanelState(UISystem.MainPanelState.Hidden);
        }
    }
}