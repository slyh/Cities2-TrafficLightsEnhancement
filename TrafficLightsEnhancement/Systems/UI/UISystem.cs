using System.Collections.Generic;
using System.Globalization;
using C2VM.TrafficLightsEnhancement.Components;
using C2VM.TrafficLightsEnhancement.Systems.Overlay;
using C2VM.TrafficLightsEnhancement.Systems.Update;
using C2VM.TrafficLightsEnhancement.Utils;
using Game;
using Game.Common;
using Game.Rendering;
using Game.SceneFlow;
using Game.UI;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace C2VM.TrafficLightsEnhancement.Systems.UI;

public partial class UISystem : UISystemBase
{
    public enum MainPanelState : int
    {
        Hidden = 0,
        Empty = 1,
        Main = 2,
        CustomPhase = 3,
    }

    private bool m_ShowNotificationUnsaved;

    public MainPanelState m_MainPanelState { get; private set; }

    public Entity m_SelectedEntity { get; private set; }

    private CustomTrafficLights m_CustomTrafficLights;

    private Game.City.CityConfigurationSystem m_CityConfigurationSystem;

    private RenderSystem m_RenderSystem;

    private Tool.ToolSystem m_ToolSystem;

    private Update.ModificationUpdateSystem m_ModificationUpdateSystem;

    private SimulationUpdateSystem m_SimulationUpdateSystem;

    private Camera m_Camera;

    private int m_ScreenHeight;

    private CameraUpdateSystem m_CameraUpdateSystem;

    private float3 m_CameraPosition;

    private List<UITypes.WorldPosition> m_WorldPositionList;

    private Dictionary<Entity, NativeArray<NodeUtils.EdgeInfo>> m_EdgeInfoDictionary;

    private int m_DebugDisplayGroup;

    private UITypes.ScreenPoint m_MainPanelPosition;

    public TypeHandle m_TypeHandle;

    protected override void OnCreate()
    {
        base.OnCreate();
        m_TypeHandle.AssignHandles(ref base.CheckedStateRef);

        m_Camera = Camera.main;
        m_ScreenHeight = Screen.height;
        m_MainPanelPosition = new(-999999, -999999);

        m_WorldPositionList = [];
        m_EdgeInfoDictionary = [];

        m_DebugDisplayGroup = -1;

        m_CameraUpdateSystem = World.GetOrCreateSystemManaged<CameraUpdateSystem>();
        m_CityConfigurationSystem = World.GetOrCreateSystemManaged<Game.City.CityConfigurationSystem>();
        m_RenderSystem = World.GetOrCreateSystemManaged<RenderSystem>();
        m_ToolSystem = World.GetOrCreateSystemManaged<Tool.ToolSystem>();
        m_ModificationUpdateSystem = World.GetOrCreateSystemManaged<Update.ModificationUpdateSystem>();
        m_SimulationUpdateSystem = World.GetOrCreateSystemManaged<SimulationUpdateSystem>();

        m_ModificationUpdateSystem.Enabled = false;
        m_SimulationUpdateSystem.Enabled = false;

        AddUIBindings();
        SetupKeyBindings();
        UpdateLocale();

        GameManager.instance.localizationManager.onActiveDictionaryChanged += UpdateLocale;
    }

    protected override void OnUpdate()
    {
        if (m_WorldPositionList.Count > 0 && !m_CameraPosition.Equals(m_CameraUpdateSystem.position))
        {
            m_CameraPosition = m_CameraUpdateSystem.position;
            m_ScreenPointBinding.Update();
        }
    }

    protected override void OnDestroy()
    {
        ClearEdgeInfo();
    }

    protected override void OnGameLoadingComplete(Colossal.Serialization.Entities.Purpose purpose, GameMode mode)
    {
        m_MainPanelBinding.Update();
        m_CityConfigurationBinding.Update();
    }

    public void SimulationUpdate()
    {
        if (m_MainPanelState == MainPanelState.CustomPhase)
        {
            m_MainPanelBinding.Update();
        }
        RedrawGizmo();
    }

    public void SetMainPanelState(MainPanelState state)
    {
        UpdateEntity();
        m_MainPanelState = state;
        m_MainPanelBinding.Update();
        RedrawIcon();
        UpdateManualSignalGroup(0);
        if (m_MainPanelState != MainPanelState.CustomPhase)
        {
            UpdateActiveEditingCustomPhaseIndex(-1);
            UpdateActiveViewingCustomPhaseIndex(-1);
        }
        if (m_MainPanelState == MainPanelState.Hidden)
        {
            SaveSelectedEntity();
            m_ToolSystem.Disable();
        }
        else if (m_MainPanelState == MainPanelState.Empty)
        {
            m_ToolSystem.Enable();
        }
        else
        {
            m_ToolSystem.Suspend();
        }
        m_ModificationUpdateSystem.Enabled = m_MainPanelState != MainPanelState.Hidden;
        m_SimulationUpdateSystem.Enabled = m_MainPanelState != MainPanelState.Hidden;
        m_RenderSystem.ClearLineMesh();
    }

    public static string GetLocaleCode()
    {
        string locale = Utils.LocalisationUtils.GetAutoLocale(GameManager.instance.localizationManager.activeLocaleId, CultureInfo.CurrentCulture.Name);
        if (Mod.m_Settings != null && Mod.m_Settings.m_Locale != "auto")
        {
            locale = Mod.m_Settings.m_Locale;
        }
        return locale;
    }

    public static void UpdateLocale()
    {
        LocalisationUtils localisationsHelper = new LocalisationUtils(GetLocaleCode());
        localisationsHelper.AddToDictionary(GameManager.instance.localizationManager.activeDictionary);

        if (m_LocaleBinding != null)
        {
            m_LocaleBinding.Update();
        }
    }

    public void UpdateEdgeInfo(Entity node)
    {
        if (node == Entity.Null)
        {
            return;
        }
        if (m_EdgeInfoDictionary.ContainsKey(node))
        {
            NodeUtils.Dispose(m_EdgeInfoDictionary[node]);
        }
        m_EdgeInfoDictionary[node] = NodeUtils.GetEdgeInfoList(Allocator.Persistent, node, this).AsArray();
        m_EdgeInfoBinding.Update();
        m_MainPanelBinding.Update();
    }

    public void ClearEdgeInfo()
    {
        foreach (var kV in m_EdgeInfoDictionary)
        {
            NodeUtils.Dispose(kV.Value);
        }
        m_EdgeInfoDictionary.Clear();
    }

    public void SaveSelectedEntity()
    {
        UpdateEntity();
        ChangeSelectedEntity(Entity.Null);
        m_MainPanelBinding.Update();
    }

    public void UpdateEntity(bool keepTimer = true, bool addUpdated = true)
    {
        if (m_SelectedEntity != Entity.Null)
        {
            if (!EntityManager.HasComponent<CustomTrafficLights>(m_SelectedEntity))
            {
                EntityManager.AddComponentData(m_SelectedEntity, m_CustomTrafficLights);
            }
            else
            {
                if (keepTimer)
                {
                    var customTrafficLights = EntityManager.GetComponentData<CustomTrafficLights>(m_SelectedEntity);
                    m_CustomTrafficLights.m_Timer = customTrafficLights.m_Timer;
                }
                EntityManager.SetComponentData<CustomTrafficLights>(m_SelectedEntity, m_CustomTrafficLights);
            }

            if (!EntityManager.HasComponent<Game.Net.TrafficLights>(m_SelectedEntity))
            {
                EntityManager.RemoveComponent<CustomTrafficLights>(m_SelectedEntity);
            }
            else if (m_CustomTrafficLights.GetPatternOnly() == CustomTrafficLights.Patterns.ModDefault)
            {
                EntityManager.RemoveComponent<CustomTrafficLights>(m_SelectedEntity);
            }

            if (addUpdated)
            {
                EntityManager.AddComponentData(m_SelectedEntity, default(Updated));
            }
        }
    }

    public void ChangeSelectedEntity(Entity entity)
    {
        UpdateManualSignalGroup(0);

        if (entity != m_SelectedEntity && entity != Entity.Null && m_SelectedEntity != Entity.Null)
        {
            m_ShowNotificationUnsaved = true;
            m_MainPanelBinding.Update();
            return;
        }

        if (entity != m_SelectedEntity)
        {
            m_ShowNotificationUnsaved = false;
            m_RenderSystem.ClearLineMesh();
            ClearEdgeInfo();

            if (!entity.Equals(Entity.Null))
            {
                UpdateEdgeInfo(entity);
                SetMainPanelState(MainPanelState.Main);

                if (EntityManager.HasComponent<CustomTrafficLights>(entity))
                {
                    m_CustomTrafficLights = EntityManager.GetComponentData<CustomTrafficLights>(entity);
                }
                else
                {
                    m_CustomTrafficLights = new CustomTrafficLights(CustomTrafficLights.Patterns.Vanilla);
                }
            }
            else if (m_MainPanelState != MainPanelState.Hidden)
            {
                SetMainPanelState(MainPanelState.Empty);
            }

            m_SelectedEntity = entity;
        }
    }
}