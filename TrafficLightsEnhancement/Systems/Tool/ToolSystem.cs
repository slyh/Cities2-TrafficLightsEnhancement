using System.Reflection;
using C2VM.TrafficLightsEnhancement.Components;
using C2VM.TrafficLightsEnhancement.Systems.Overlay;
using Colossal.Entities;
using Game.Net;
using Game.Prefabs;
using Game.Tools;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;

namespace C2VM.TrafficLightsEnhancement.Systems.Tool;

public partial class ToolSystem : NetToolSystem
{
    public override string toolID => "C2VMTLE Tool";

    private RenderSystem m_RenderSystem;

    private UI.UISystem m_UISystem;

    private NativeList<ControlPoint> m_ParentControlPoints;

    private NativeReference<AppliedUpgrade> m_ParentAppliedUpgrade;

    private Entity m_PrefabEntity = Entity.Null;

    private Entity m_RaycastResult = Entity.Null;

    public bool m_Suspended { get; private set; }

    protected override void OnCreate()
    {
        base.OnCreate();
        m_RenderSystem = World.GetOrCreateSystemManaged<RenderSystem>();
        m_UISystem = World.GetOrCreateSystemManaged<UI.UISystem>();
        m_ParentControlPoints = GetControlPoints(out JobHandle _);
        m_ParentAppliedUpgrade = (NativeReference<AppliedUpgrade>)typeof(NetToolSystem).GetField("m_AppliedUpgrade", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(this);
        m_ToolSystem.EventToolChanged += ToolChanged;
    }

    protected override JobHandle OnUpdate(JobHandle inputDeps)
    {
        if (m_Suspended)
        {
            m_ToolRaycastSystem.raycastFlags |= Game.Common.RaycastFlags.UIDisable;
        }
        var result = base.OnUpdate(inputDeps);
        base.applyAction.enabled = !m_Suspended;
        base.secondaryApplyAction.enabled = !m_Suspended;
        if ((m_ToolRaycastSystem.raycastFlags & Game.Common.RaycastFlags.UIDisable) == 0)
        {
            if (m_ParentControlPoints.Length >= 4)
            {
                Entity originalEntity = m_ParentControlPoints[m_ParentControlPoints.Length - 3].m_OriginalEntity;
                if (originalEntity != m_RaycastResult)
                {
                    m_RaycastResult = originalEntity;
                    m_RenderSystem.ClearLineMesh();
                    if (!EntityManager.HasComponent<Roundabout>(m_RaycastResult) && EntityManager.TryGetComponent<NodeGeometry>(m_RaycastResult, out var nodeGeometry))
                    {
                        m_RenderSystem.AddBounds(nodeGeometry.m_Bounds, new UnityEngine.Color(0.5f, 1.0f, 2.0f, 1.0f), 0.5f);
                        m_RenderSystem.BuildLineMesh();
                    }
                }
            }
            else
            {
                m_RaycastResult = Entity.Null;
                m_RenderSystem.ClearLineMesh();
            }
            if (applyAction.WasReleasedThisFrame())
            {
                Entity entity = m_ParentAppliedUpgrade.Value.m_Entity;
                CompositionFlags flags = m_ParentAppliedUpgrade.Value.m_Flags;
                if (entity != Entity.Null && (flags.m_General & CompositionFlags.General.TrafficLights) != 0)
                {
                    m_UISystem.ChangeSelectedEntity(entity);
                }
            }
            else if (secondaryApplyAction.WasPressedThisFrame())
            {
                if (m_RaycastResult != Entity.Null)
                {
                    if (EntityManager.HasComponent<CustomTrafficLights>(m_RaycastResult))
                    {
                        EntityManager.RemoveComponent<CustomTrafficLights>(m_RaycastResult);
                        m_UISystem.RedrawIcon();
                    }
                }
            }
        }
        return result;
    }

    protected override void OnGameLoadingComplete(Colossal.Serialization.Entities.Purpose purpose, Game.GameMode mode)
    {
        Mod.m_Log.Info($"Searching for traffic light prefab asset entities");
        m_PrefabEntity = Entity.Null;
        EntityQuery query = GetEntityQuery(ComponentType.ReadOnly<PlaceableNetData>());
        NativeArray<Entity> entityArray = query.ToEntityArray(Allocator.Temp);
        NativeArray<PlaceableNetData> placeableNetDataArray = query.ToComponentDataArray<PlaceableNetData>(Allocator.Temp);
        for (int i = 0; i < entityArray.Length; i++)
        {
            if ((placeableNetDataArray[i].m_SetUpgradeFlags.m_General & CompositionFlags.General.TrafficLights) == 0)
            {
                continue;
            }
            if (m_PrefabSystem.TryGetPrefab(entityArray[i], out PrefabBase prefabBase) && prefabBase is NetPrefab)
            {
                m_PrefabEntity = entityArray[i];
                Mod.m_Log.Info($"{m_PrefabEntity} prefabBase.uiTag: {prefabBase.uiTag}");
            }
        }
        Mod.Assert(m_PrefabEntity != Entity.Null, "Traffic lights prefab asset entity not found. The tool system will not work.");
    }

    protected override bool GetAllowApply()
    {
        return !(secondaryApplyAction.WasReleasedThisFrame() || secondaryApplyAction.WasPressedThisFrame());
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
        if (m_PrefabSystem.TryGetPrefab(m_PrefabEntity, out NetPrefab netPrefab))
        {
            this.prefab = netPrefab;
            this.underground = m_ToolSystem.activeTool.requireUnderground;
            m_Suspended = false;
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
            m_UISystem.SetMainPanelState(UI.UISystem.MainPanelState.Hidden);
        }
    }
}