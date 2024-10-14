using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using C2VM.CommonLibraries.LaneSystem;
using C2VM.TrafficLightsEnhancement.Components;
using C2VM.TrafficLightsEnhancement.Systems.TrafficLightInitializationSystem;
using C2VM.TrafficLightsEnhancement.Utils;
using Colossal.Entities;
using Colossal.UI.Binding;
using Game;
using Game.Common;
using Game.Net;
using Game.Rendering;
using Game.SceneFlow;
using Game.UI;
using Newtonsoft.Json;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace C2VM.TrafficLightsEnhancement.Systems.UISystem;

public partial class UISystem : UISystemBase
{
    public enum MainPanelState : int
    {
        Hidden = 0,
        Empty = 1,
        Main = 2,
        CustomPhase = 3,
    }

    public bool m_ShowNotificationUnsaved;

    private MainPanelState m_MainPanelState;

    public Entity m_SelectedEntity;

    private CustomTrafficLights m_CustomTrafficLights;

    private Game.City.CityConfigurationSystem m_CityConfigurationSystem;

    private LdtRetirementSystem m_LdtRetirementSystem;

    private RenderSystem.RenderSystem m_RenderSystem;

    private Entity m_TrafficLightsAssetEntity = Entity.Null;

    private Camera m_Camera;

    private int m_ScreenHeight;

    private CameraUpdateSystem m_CameraUpdateSystem;

    private float3 m_CameraPosition;

    private List<UITypes.WorldPosition> m_WorldPositionList;

    private Dictionary<Entity, NativeArray<NodeUtils.EdgeInfo>> m_EdgeInfoDictionary;

    private int m_DebugDisplayGroup;

    private GetterValueBinding<string> m_MainPanelBinding;

    private static GetterValueBinding<string> m_LocaleBinding;

    private GetterValueBinding<string> m_CityConfigurationBinding;

    private GetterValueBinding<Dictionary<string, UITypes.ScreenPoint>> m_ScreenPointBinding;

    private GetterValueBinding<Dictionary<Entity, NativeArray<NodeUtils.EdgeInfo>>> m_EdgeInfoBinding;

    private ValueBinding<int> m_ActiveEditingCustomPhaseIndexBinding;

    public TypeHandle m_TypeHandle;

    protected override void OnCreate()
    {
        base.OnCreate();

        m_Camera = Camera.main;
        m_ScreenHeight = Screen.height;

        m_WorldPositionList = [];
        m_EdgeInfoDictionary = [];

        m_DebugDisplayGroup = -1;

        m_CameraUpdateSystem = World.GetOrCreateSystemManaged<CameraUpdateSystem>();
        m_CityConfigurationSystem = World.GetOrCreateSystemManaged<Game.City.CityConfigurationSystem>();
        m_LdtRetirementSystem = World.GetOrCreateSystemManaged<LdtRetirementSystem>();
        m_RenderSystem = World.GetOrCreateSystemManaged<RenderSystem.RenderSystem>();

        AddBinding(m_MainPanelBinding = new GetterValueBinding<string>("C2VM.TLE", "GetMainPanel", GetMainPanel));
        AddBinding(m_LocaleBinding = new GetterValueBinding<string>("C2VM.TLE", "GetLocale", GetLocale));
        AddBinding(m_CityConfigurationBinding = new GetterValueBinding<string>("C2VM.TLE", "GetCityConfiguration", GetCityConfiguration));
        AddBinding(m_ScreenPointBinding = new GetterValueBinding<Dictionary<string, UITypes.ScreenPoint>>("C2VM.TLE", "GetScreenPoint", GetScreenPoint, new DictionaryWriter<string, UITypes.ScreenPoint>(null, new ValueWriter<UITypes.ScreenPoint>()), new JsonWriter.FalseEqualityComparer<Dictionary<string, UITypes.ScreenPoint>>()));
        AddBinding(m_EdgeInfoBinding = new GetterValueBinding<Dictionary<Entity, NativeArray<NodeUtils.EdgeInfo>>>("C2VM.TLE", "GetEdgeInfo", GetEdgeInfo, new JsonWriter.EdgeInfoWriter(), new JsonWriter.FalseEqualityComparer<Dictionary<Entity, NativeArray<NodeUtils.EdgeInfo>>>()));
        AddBinding(m_ActiveEditingCustomPhaseIndexBinding = new ValueBinding<int>("C2VM.TLE", "GetActiveEditingCustomPhaseIndex", -1));

        AddBinding(new CallBinding<string, string>("C2VM.TLE", "CallMainPanelUpdatePattern", CallMainPanelUpdatePattern));
        AddBinding(new CallBinding<string, string>("C2VM.TLE", "CallMainPanelUpdateOption", CallMainPanelUpdateOption));
        AddBinding(new CallBinding<string, string>("C2VM.TLE", "CallMainPanelUpdateValue", CallMainPanelUpdateValue));
        AddBinding(new CallBinding<string, string>("C2VM.TLE", "CallMainPanelSave", CallMainPanelSave));
        AddBinding(new CallBinding<string, string>("C2VM.TLE", "CallLaneDirectionToolReset", CallLaneDirectionToolReset));

        AddBinding(new CallBinding<string, string>("C2VM.TLE", "CallSetMainPanelState", CallSetMainPanelState));
        AddBinding(new CallBinding<string, string>("C2VM.TLE", "CallAddCustomPhase", CallAddCustomPhase));
        AddBinding(new CallBinding<string, string>("C2VM.TLE", "CallRemoveCustomPhase", CallRemoveCustomPhase));
        AddBinding(new CallBinding<string, string>("C2VM.TLE", "CallSwapCustomPhase", CallSwapCustomPhase));
        AddBinding(new CallBinding<string, string>("C2VM.TLE", "CallSetActiveEditingCustomPhaseIndex", CallSetActiveEditingCustomPhaseIndex));
        AddBinding(new CallBinding<string, string>("C2VM.TLE", "CallUpdateEdgeGroupMask", CallUpdateEdgeGroupMask));
        AddBinding(new CallBinding<string, string>("C2VM.TLE", "CallUpdateSubLaneGroupMask", CallUpdateSubLaneGroupMask));
        AddBinding(new CallBinding<string, string>("C2VM.TLE", "CallUpdateCustomPhaseData", CallUpdateCustomPhaseData));

        AddBinding(new CallBinding<string, string>("C2VM.TLE", "CallKeyPress", CallKeyPress));

        AddBinding(new CallBinding<string, string>("C2VM.TLE", "CallAddWorldPosition", CallAddWorldPosition));
        AddBinding(new CallBinding<string, string>("C2VM.TLE", "CallRemoveWorldPosition", CallRemoveWorldPosition));

        AddBinding(new CallBinding<string, string>("C2VM.TLE", "CallOpenBrowser", CallOpenBrowser));

        AddBinding(new TriggerBinding<int>("C2VM.TLE", "SetDebugDisplayGroup", (group) => { m_DebugDisplayGroup = group; RedrawGizmo(); }));
    }

    protected override void OnCreateForCompiler()
    {
        base.OnCreateForCompiler();
        m_TypeHandle.AssignHandles(ref base.CheckedStateRef);
    }

    protected override void OnUpdate()
    {
        if (m_WorldPositionList.Count > 0 && !m_CameraPosition.Equals(m_CameraUpdateSystem.position))
        {
            m_CameraPosition = m_CameraUpdateSystem.position;
            m_ScreenPointBinding.Update();
        }
    }

    protected override void OnGameLoadingComplete(Colossal.Serialization.Entities.Purpose purpose, GameMode mode)
    {
        NativeArray<Entity> placeablePrefabsList = GetEntityQuery(ComponentType.ReadOnly<Game.Prefabs.PlaceableNetData>()).ToEntityArray(Allocator.Temp);
        for (int i = 0; i < placeablePrefabsList.Length; i++)
        {
            Entity entity = placeablePrefabsList[i];
            Game.Prefabs.PlaceableNetData placeableNetData = EntityManager.GetComponentData<Game.Prefabs.PlaceableNetData>(entity);
            if ((placeableNetData.m_SetUpgradeFlags.m_General & Game.Prefabs.CompositionFlags.General.TrafficLights) != 0)
            {
                m_TrafficLightsAssetEntity = entity;
                break;
            }
        }
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

    public void RedrawGizmo()
    {
        if (m_SelectedEntity != Entity.Null)
        {
            m_RenderSystem.ClearLineMesh();
            if (EntityManager.TryGetBuffer<SubLane>(m_SelectedEntity, true, out var subLaneBuffer))
            {
                int displayGroup = 16;
                if (m_ActiveEditingCustomPhaseIndexBinding.value >= 0)
                {
                    displayGroup = m_ActiveEditingCustomPhaseIndexBinding.value;
                }
                else if (EntityManager.TryGetComponent<TrafficLights>(m_SelectedEntity, out var trafficLights))
                {
                    displayGroup = trafficLights.m_CurrentSignalGroup - 1;
                }
                if (m_DebugDisplayGroup >= 0)
                {
                    displayGroup = m_DebugDisplayGroup;
                }
                foreach (var subLane in subLaneBuffer)
                {
                    Entity subLaneEntity = subLane.m_SubLane;
                    bool isPedestrian = EntityManager.TryGetComponent<PedestrianLane>(subLaneEntity, out var pedestrianLane);
                    if (EntityManager.HasComponent<MasterLane>(subLaneEntity))
                    {
                        continue;
                    }
                    if (!EntityManager.HasComponent<CarLane>(subLaneEntity) && !EntityManager.HasComponent<TrackLane>(subLaneEntity) && !isPedestrian)
                    {
                        continue;
                    }
                    if (isPedestrian && (pedestrianLane.m_Flags & PedestrianLaneFlags.Crosswalk) == 0)
                    {
                        continue;
                    }
                    if (EntityManager.TryGetComponent<LaneSignal>(subLaneEntity, out var laneSignal) && EntityManager.TryGetComponent<Curve>(subLaneEntity, out var curve))
                    {
                        Color color = Color.green;
                        if (EntityManager.TryGetComponent<ExtraLaneSignal>(subLaneEntity, out var extraLaneSignal) && (extraLaneSignal.m_YieldGroupMask & 1 << displayGroup) != 0)
                        {
                            color = Color.yellow;
                        }
                        if ((laneSignal.m_GroupMask & 1 << displayGroup) != 0)
                        {
                            m_RenderSystem.AddBezier(curve.m_Bezier, color, curve.m_Length, 0.25f);
                        }
                    }
                }
            }
            m_RenderSystem.BuildLineMesh();
        }
    }

    public void RedrawIcon()
    {
        m_RenderSystem.ClearIconMesh();
        if (m_MainPanelState == MainPanelState.Empty)
        {
            var entityQuery = EntityManager.CreateEntityQuery(ComponentType.ReadOnly<Node>(), ComponentType.ReadOnly<CustomTrafficLights>());
            var nodeArray = entityQuery.ToComponentDataArray<Node>(Allocator.Temp);
            var customTrafficLightsArray = entityQuery.ToComponentDataArray<CustomTrafficLights>(Allocator.Temp);
            for (int i = 0; i < nodeArray.Length; i++)
            {
                var node = nodeArray[i];
                var customTrafficLights = customTrafficLightsArray[i];
                RenderSystem.RenderSystem.Icon icon = RenderSystem.RenderSystem.Icon.TrafficLight;
                if (customTrafficLights.GetPatternOnly() == CustomTrafficLights.Patterns.CustomPhase)
                {
                    icon = RenderSystem.RenderSystem.Icon.TrafficLightWrench;
                }
                m_RenderSystem.AddIcon(node.m_Position, node.m_Rotation, icon);
            }
        }
        m_RenderSystem.BuildIconMesh();
    }

    public void SetMainPanelState(MainPanelState state)
    {
        UpdateEntity();
        m_MainPanelState = state;
        m_MainPanelBinding.Update();
        RedrawIcon();
        if (m_MainPanelState != MainPanelState.CustomPhase)
        {
            m_ActiveEditingCustomPhaseIndexBinding.Update(-1);
        }
        if (m_MainPanelState == MainPanelState.Hidden)
        {
            CallMainPanelSave("");
        }
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

    public static string GetLocale()
    {
        var result = new
        {
            locale = GetLocaleCode(),
        };

        return JsonConvert.SerializeObject(result);
    }

    public static void UpdateLocale()
    {
        Utils.LocalisationUtils localisationsHelper = new Utils.LocalisationUtils(GetLocaleCode());
        localisationsHelper.AddToDictionary(GameManager.instance.localizationManager.activeDictionary);

        if (m_LocaleBinding != null)
        {
            m_LocaleBinding.Update();
        }
    }

    public string GetCityConfiguration()
    {
        var result = new
        {
            leftHandTraffic = m_CityConfigurationSystem.leftHandTraffic,
        };

        return JsonConvert.SerializeObject(result);
    }

    protected string CallOpenBrowser(string jsonString)
    {
        var keyDefinition = new { key = "", value = "" };
        var parsedKey = JsonConvert.DeserializeAnonymousType(jsonString, keyDefinition);
        System.Diagnostics.Process.Start(parsedKey.value);
        return "";
    }

    protected string CallMainPanelUpdatePattern(string input)
    {
        UITypes.ItemRadio pattern = JsonConvert.DeserializeObject<UITypes.ItemRadio>(input);
        m_CustomTrafficLights.SetPattern(((uint)m_CustomTrafficLights.GetPattern() & 0xFFFF0000) | uint.Parse(pattern.value));
        if (m_CustomTrafficLights.GetPatternOnly() != CustomTrafficLights.Patterns.Vanilla)
        {
            m_CustomTrafficLights.SetPattern(m_CustomTrafficLights.GetPattern() & ~CustomTrafficLights.Patterns.CentreTurnGiveWay);
        }
        if (m_CustomTrafficLights.GetPatternOnly() == CustomTrafficLights.Patterns.CustomPhase)
        {
            if (!EntityManager.HasBuffer<CustomPhaseData>(m_SelectedEntity))
            {
                EntityManager.AddComponent<CustomPhaseData>(m_SelectedEntity);
            }
            if (!EntityManager.HasBuffer<EdgeGroupMask>(m_SelectedEntity))
            {
                EntityManager.AddComponent<EdgeGroupMask>(m_SelectedEntity);
            }
            if (!EntityManager.HasBuffer<SubLaneGroupMask>(m_SelectedEntity))
            {
                EntityManager.AddComponent<SubLaneGroupMask>(m_SelectedEntity);
            }
            m_CustomTrafficLights.SetPattern(CustomTrafficLights.Patterns.CustomPhase);
        }
        UpdateEntity();
        m_MainPanelBinding.Update();
        return "";
    }

    protected string CallMainPanelUpdateOption(string input)
    {
        UITypes.ItemCheckbox option = JsonConvert.DeserializeObject<UITypes.ItemCheckbox>(input);
        foreach (CustomTrafficLights.Patterns pattern in Enum.GetValues(typeof(CustomTrafficLights.Patterns)))
        {
            if (((uint) pattern & 0xFFFF0000) != 0)
            {
                if (uint.Parse(option.key) == (uint)pattern)
                {
                    // Toggle the option
                    m_CustomTrafficLights.SetPattern(m_CustomTrafficLights.GetPattern() ^ pattern);
                }
            }
        }
        UpdateEntity();
        m_MainPanelBinding.Update();
        return "";
    }

    protected string CallMainPanelUpdateValue(string jsonString)
    {
        var keyDefinition = new { key = "" };
        var parsedKey = JsonConvert.DeserializeAnonymousType(jsonString, keyDefinition);
        if (parsedKey.key == "CustomPedestrianDurationMultiplier")
        {
            var valueDefinition = new { value = 0.0f };
            var parsedValue = JsonConvert.DeserializeAnonymousType(jsonString, valueDefinition);
            m_CustomTrafficLights.SetPedestrianPhaseDurationMultiplier(parsedValue.value);
        }
        UpdateEntity();
        m_MainPanelBinding.Update();
        return "";
    }

    protected string CallMainPanelSave(string value)
    {
        ChangeSelectedEntity(Entity.Null);
        m_MainPanelBinding.Update();
        return "";
    }

    protected string GetMainPanel()
    {
        var menu = new {
            title = Mod.IsCanary() ? "TLE Canary" : "Traffic Lights Enhancement",
            image = "coui://GameUI/Media/Game/Icons/TrafficLights.svg",
            showPanel = m_MainPanelState != MainPanelState.Hidden,
            showFloatingButton = Mod.m_Settings != null && Mod.m_Settings.m_ShowFloatingButton,
            state = m_MainPanelState,
            trafficLightsAssetEntityIndex = m_TrafficLightsAssetEntity.Index,
            trafficLightsAssetEntityVersion = m_TrafficLightsAssetEntity.Version,
            items = new ArrayList()
        };
        if (m_MainPanelState == MainPanelState.Main && m_SelectedEntity != Entity.Null)
        {
            menu.items.Add(new UITypes.ItemTitle{title = "TrafficSignal"});
            menu.items.Add(UITypes.MainPanelItemPattern("ModDefault", (uint)CustomTrafficLights.Patterns.ModDefault, (uint)m_CustomTrafficLights.GetPattern()));
            menu.items.Add(UITypes.MainPanelItemPattern("Vanilla", (uint)CustomTrafficLights.Patterns.Vanilla, (uint)m_CustomTrafficLights.GetPattern()));
            if (PredefinedPatternsProcessor.IsValidPattern(m_EdgeInfoDictionary[m_SelectedEntity], CustomTrafficLights.Patterns.SplitPhasing))
            {
                menu.items.Add(UITypes.MainPanelItemPattern("SplitPhasing", (uint)CustomTrafficLights.Patterns.SplitPhasing, (uint)m_CustomTrafficLights.GetPattern()));
            }
            if (PredefinedPatternsProcessor.IsValidPattern(m_EdgeInfoDictionary[m_SelectedEntity], CustomTrafficLights.Patterns.ProtectedCentreTurn))
            {
                if (m_CityConfigurationSystem.leftHandTraffic)
                {
                    menu.items.Add(UITypes.MainPanelItemPattern("ProtectedRightTurns", (uint)CustomTrafficLights.Patterns.ProtectedCentreTurn, (uint)m_CustomTrafficLights.GetPattern()));
                }
                else
                {
                    menu.items.Add(UITypes.MainPanelItemPattern("ProtectedLeftTurns", (uint)CustomTrafficLights.Patterns.ProtectedCentreTurn, (uint)m_CustomTrafficLights.GetPattern()));
                }
            }
            menu.items.Add(UITypes.MainPanelItemPattern("CustomPhase", (uint)CustomTrafficLights.Patterns.CustomPhase, (uint)m_CustomTrafficLights.GetPattern()));
            if (m_CustomTrafficLights.GetPatternOnly() == CustomTrafficLights.Patterns.CustomPhase)
            {
                menu.items.Add(default(UITypes.ItemDivider));
                menu.items.Add(new UITypes.ItemTitle{title = "CustomPhase"});
                menu.items.Add(new UITypes.ItemButton{label = "Setup", key = "state", value = "3", engineEventName = "C2VM.TLE.CallSetMainPanelState"});
            }
            if (m_CustomTrafficLights.GetPatternOnly() < CustomTrafficLights.Patterns.ModDefault && !NodeUtils.HasTrainTrack(m_EdgeInfoDictionary[m_SelectedEntity]))
            {
                menu.items.Add(default(UITypes.ItemDivider));
                menu.items.Add(new UITypes.ItemTitle{title = "Options"});
                menu.items.Add(UITypes.MainPanelItemOption("AllowTurningOnRed", (uint)CustomTrafficLights.Patterns.AlwaysGreenKerbsideTurn, (uint)m_CustomTrafficLights.GetPattern()));
                if (m_CustomTrafficLights.GetPatternOnly() == CustomTrafficLights.Patterns.Vanilla)
                {
                    menu.items.Add(UITypes.MainPanelItemOption("GiveWayToOncomingVehicles", (uint)CustomTrafficLights.Patterns.CentreTurnGiveWay, (uint)m_CustomTrafficLights.GetPattern()));
                }
                menu.items.Add(UITypes.MainPanelItemOption("ExclusivePedestrianPhase", (uint)CustomTrafficLights.Patterns.ExclusivePedestrian, (uint)m_CustomTrafficLights.GetPattern()));
                if (((uint)m_CustomTrafficLights.GetPattern() & (uint)CustomTrafficLights.Patterns.ExclusivePedestrian) != 0)
                {
                    menu.items.Add(default(UITypes.ItemDivider));
                    menu.items.Add(new UITypes.ItemRange
                    {
                        key = "CustomPedestrianDurationMultiplier",
                        label = "CustomPedestrianDurationMultiplier",
                        valueSuffix = "CustomPedestrianDurationMultiplierSuffix",
                        min = 0.5f,
                        max = 10,
                        step = 0.5f,
                        value = m_CustomTrafficLights.m_PedestrianPhaseDurationMultiplier,
                        engineEventName = "C2VM.TLE.CallMainPanelUpdateValue"
                    });
                }
            }
            menu.items.Add(default(UITypes.ItemDivider));
            if (m_LdtRetirementSystem.m_UnmigratedNodeCount > 0)
            {
                menu.items.Add(new UITypes.ItemTitle{title = "LaneDirectionTool"});
                menu.items.Add(new UITypes.ItemButton{label = "Reset", key = "status", value = "0", engineEventName = "C2VM.TLE.CallLaneDirectionToolReset"});
                menu.items.Add(default(UITypes.ItemDivider));
                menu.items.Add(new UITypes.ItemNotification{label = "LdtMigrationNotice", notificationType = "notice", value = LdtRetirementSystem.kRetirementNoticeLink, engineEventName = "C2VM.TLE.CallOpenBrowser"});
                menu.items.Add(default(UITypes.ItemDivider));
            }
            else if (Mod.m_Settings != null && !Mod.m_Settings.m_HasReadLdtRetirementNotice)
            {
                menu.items.Add(new UITypes.ItemNotification{label = "LdtRetirementNotice", notificationType = "notice"});
                menu.items.Add(default(UITypes.ItemDivider));
            }
            menu.items.Add(new UITypes.ItemButton{label = "Save", key = "save", value = "1", engineEventName = "C2VM.TLE.CallMainPanelSave"});
            if (m_ShowNotificationUnsaved)
            {
                menu.items.Add(default(UITypes.ItemDivider));
                menu.items.Add(new UITypes.ItemNotification{label = "PleaseSave", notificationType = "warning"});
            }
        }
        else if (m_MainPanelState == MainPanelState.CustomPhase)
        {
            DynamicBuffer<CustomPhaseData> customPhaseDataBuffer;
            if (!EntityManager.TryGetBuffer(m_SelectedEntity, true, out customPhaseDataBuffer))
            {
                customPhaseDataBuffer = EntityManager.AddBuffer<CustomPhaseData>(m_SelectedEntity);
            }
            EntityManager.TryGetComponent(m_SelectedEntity, out TrafficLights trafficLights);
            for (int i = 0; i < customPhaseDataBuffer.Length; i++)
            {
                menu.items.Add(new UITypes.ItemCustomPhase{
                    activeIndex = m_ActiveEditingCustomPhaseIndexBinding.value,
                    currentSignalGroup = trafficLights.m_CurrentSignalGroup,
                    index = i,
                    length = customPhaseDataBuffer.Length,
                    minimumDurationMultiplier = customPhaseDataBuffer[i].m_MinimumDurationMultiplier
                });
            }
            if (customPhaseDataBuffer.Length < 16)
            {
                menu.items.Add(new UITypes.ItemButton{label = "Add", key = "add", value = "add", engineEventName = "C2VM.TLE.CallAddCustomPhase"});
            }
            menu.items.Add(new UITypes.ItemButton{label = "Save", key = "state", value = "2", engineEventName = "C2VM.TLE.CallSetMainPanelState"});
        }
        else if (m_MainPanelState == MainPanelState.Empty)
        {
            menu.items.Add(new UITypes.ItemMessage{message = "PleaseSelectJunction"});
        }
        if (Mod.IsCanary() && Mod.m_Settings != null && Mod.m_Settings.m_SuppressCanaryWarningVersion != Mod.m_InformationalVersion)
        {
            menu.items.Add(default(UITypes.ItemDivider));
            menu.items.Add(new UITypes.ItemNotification{label = "CanaryBuildWarning", notificationType = "warning"});
        }
        string result = JsonConvert.SerializeObject(menu);
        return result;
    }

    protected Dictionary<Entity, NativeArray<NodeUtils.EdgeInfo>> GetEdgeInfo()
    {
        return m_EdgeInfoDictionary;
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

    protected string CallSetMainPanelState(string input)
    {
        var definition = new { key = "", value = "" };
        var value = JsonConvert.DeserializeAnonymousType(input, definition);
        MainPanelState state = (MainPanelState)Int32.Parse(value.value);
        SetMainPanelState(state);
        return "";
    }

    protected string CallSetActiveEditingCustomPhaseIndex(string input)
    {
        var definition = new { index = 0 };
        var value = JsonConvert.DeserializeAnonymousType(input, definition);
        m_ActiveEditingCustomPhaseIndexBinding.Update(value.index);
        m_MainPanelBinding.Update();
        UpdateEntity();
        return "";
    }

    protected string CallAddCustomPhase(string input)
    {
        if (!m_SelectedEntity.Equals(Entity.Null))
        {
            DynamicBuffer<CustomPhaseData> customPhaseDataBuffer;
            if (!EntityManager.TryGetBuffer(m_SelectedEntity, false, out customPhaseDataBuffer))
            {
                customPhaseDataBuffer = EntityManager.AddBuffer<CustomPhaseData>(m_SelectedEntity);
            }
            customPhaseDataBuffer.Add(new CustomPhaseData());
            m_ActiveEditingCustomPhaseIndexBinding.Update(customPhaseDataBuffer.Length - 1);
            m_MainPanelBinding.Update();
            UpdateEdgeInfo(m_SelectedEntity);
            UpdateEntity();
        }
        return "";
    }

    protected string CallRemoveCustomPhase(string input)
    {        
        var definition = new { index = 0 };
        var value = JsonConvert.DeserializeAnonymousType(input, definition);
        if (!m_SelectedEntity.Equals(Entity.Null))
        {
            DynamicBuffer<CustomPhaseData> customPhaseDataBuffer;
            if (!EntityManager.TryGetBuffer(m_SelectedEntity, false, out customPhaseDataBuffer))
            {
                customPhaseDataBuffer = EntityManager.AddBuffer<CustomPhaseData>(m_SelectedEntity);
            }
            customPhaseDataBuffer.RemoveAt(value.index);

            DynamicBuffer<EdgeGroupMask> edgeGroupMaskBuffer;
            DynamicBuffer<SubLaneGroupMask> subLaneGroupMaskBuffer;
            if (!EntityManager.TryGetBuffer(m_SelectedEntity, false, out edgeGroupMaskBuffer))
            {
                edgeGroupMaskBuffer = EntityManager.AddBuffer<EdgeGroupMask>(m_SelectedEntity);
            }
            if (!EntityManager.TryGetBuffer(m_SelectedEntity, false, out subLaneGroupMaskBuffer))
            {
                subLaneGroupMaskBuffer = EntityManager.AddBuffer<SubLaneGroupMask>(m_SelectedEntity);
            }
            for (int i = value.index; i < 16; i++)
            {
                CustomPhaseUtils.SwapBit(subLaneGroupMaskBuffer, i, i + 1);
                CustomPhaseUtils.SwapBit(edgeGroupMaskBuffer, i, i + 1);
            }

            if (m_ActiveEditingCustomPhaseIndexBinding.value >= customPhaseDataBuffer.Length)
            {
                m_ActiveEditingCustomPhaseIndexBinding.Update(customPhaseDataBuffer.Length - 1);
            }

            m_MainPanelBinding.Update();
            UpdateEdgeInfo(m_SelectedEntity);

            UpdateEntity();
        }
        return "";
    }

    protected string CallSwapCustomPhase(string input)
    {        
        var definition = new { index1 = 0, index2 = 0 };
        var value = JsonConvert.DeserializeAnonymousType(input, definition);
        if (!m_SelectedEntity.Equals(Entity.Null))
        {
            DynamicBuffer<CustomPhaseData> customPhaseDataBuffer;
            if (!EntityManager.TryGetBuffer(m_SelectedEntity, false, out customPhaseDataBuffer))
            {
                customPhaseDataBuffer = EntityManager.AddBuffer<CustomPhaseData>(m_SelectedEntity);
            }
            (customPhaseDataBuffer[value.index2], customPhaseDataBuffer[value.index1]) = (customPhaseDataBuffer[value.index1], customPhaseDataBuffer[value.index2]);

            DynamicBuffer<EdgeGroupMask> edgeGroupMaskBuffer;
            if (!EntityManager.TryGetBuffer(m_SelectedEntity, false, out edgeGroupMaskBuffer))
            {
                edgeGroupMaskBuffer = EntityManager.AddBuffer<EdgeGroupMask>(m_SelectedEntity);
            }
            CustomPhaseUtils.SwapBit(edgeGroupMaskBuffer, value.index1, value.index2);

            DynamicBuffer<SubLaneGroupMask> subLaneGroupMaskBuffer;
            if (!EntityManager.TryGetBuffer(m_SelectedEntity, false, out subLaneGroupMaskBuffer))
            {
                subLaneGroupMaskBuffer = EntityManager.AddBuffer<SubLaneGroupMask>(m_SelectedEntity);
            }
            CustomPhaseUtils.SwapBit(subLaneGroupMaskBuffer, value.index1, value.index2);

            m_ActiveEditingCustomPhaseIndexBinding.Update(value.index2);
            m_MainPanelBinding.Update();
            UpdateEdgeInfo(m_SelectedEntity);

            UpdateEntity();
        }
        return "";
    }

    protected string CallUpdateCustomPhaseData(string jsonString)
    {
        var keyDefinition = new { key = "" };
        var parsedKey = JsonConvert.DeserializeAnonymousType(jsonString, keyDefinition);
        if (parsedKey.key == "MinimumDurationMultiplier")
        {
            var valueDefinition = new { value = 0.0f };
            var parsedValue = JsonConvert.DeserializeAnonymousType(jsonString, valueDefinition);
            if (!m_SelectedEntity.Equals(Entity.Null))
            {
                DynamicBuffer<CustomPhaseData> customPhaseDataBuffer;
                if (!EntityManager.TryGetBuffer(m_SelectedEntity, false, out customPhaseDataBuffer))
                {
                    customPhaseDataBuffer = EntityManager.AddBuffer<CustomPhaseData>(m_SelectedEntity);
                }
                var newValue = customPhaseDataBuffer[m_ActiveEditingCustomPhaseIndexBinding.value];
                newValue.m_MinimumDurationMultiplier = parsedValue.value;
                customPhaseDataBuffer[m_ActiveEditingCustomPhaseIndexBinding.value] = newValue;

                m_MainPanelBinding.Update();
                UpdateEdgeInfo(m_SelectedEntity);
                UpdateEntity();
            }
        }
        return "";
    }

    protected string CallUpdateEdgeGroupMask(string input)
    {
        if (m_SelectedEntity.Equals(Entity.Null))
        {
            return "";
        }

        EdgeGroupMask[] groupMaskArray = JsonConvert.DeserializeObject<EdgeGroupMask[]>(input);
        DynamicBuffer<EdgeGroupMask> groupMaskBuffer;
        if (EntityManager.HasBuffer<EdgeGroupMask>(m_SelectedEntity))
        {
            groupMaskBuffer = EntityManager.GetBuffer<EdgeGroupMask>(m_SelectedEntity, false);
        }
        else
        {
            groupMaskBuffer = EntityManager.AddBuffer<EdgeGroupMask>(m_SelectedEntity);
        }

        foreach (var newValue in groupMaskArray)
        {
            int index = CustomPhaseUtils.TryGet(groupMaskBuffer, newValue, out EdgeGroupMask oldValue);
            if (index >= 0)
            {
                groupMaskBuffer[index] = new EdgeGroupMask(oldValue, newValue);
            }
            else
            {
                groupMaskBuffer.Add(new EdgeGroupMask(oldValue, newValue));
            }
        }

        UpdateEdgeInfo(m_SelectedEntity);
        UpdateEntity();

        return "";
    }

    protected string CallUpdateSubLaneGroupMask(string input)
    {
        if (m_SelectedEntity.Equals(Entity.Null))
        {
            return "";
        }

        SubLaneGroupMask[] groupMaskArray = JsonConvert.DeserializeObject<SubLaneGroupMask[]>(input);
        DynamicBuffer<SubLaneGroupMask> groupMaskBuffer;
        if (EntityManager.HasBuffer<SubLaneGroupMask>(m_SelectedEntity))
        {
            groupMaskBuffer = EntityManager.GetBuffer<SubLaneGroupMask>(m_SelectedEntity, false);
        }
        else
        {
            groupMaskBuffer = EntityManager.AddBuffer<SubLaneGroupMask>(m_SelectedEntity);
        }

        foreach (var newValue in groupMaskArray)
        {
            int index = CustomPhaseUtils.TryGet(groupMaskBuffer, newValue, out SubLaneGroupMask oldValue);
            if (index >= 0)
            {
                groupMaskBuffer[index] = new SubLaneGroupMask(oldValue, newValue);
            }
            else
            {
                groupMaskBuffer.Add(new SubLaneGroupMask(oldValue, newValue));
            }
        }

        UpdateEdgeInfo(m_SelectedEntity);
        UpdateEntity();

        return "";
    }

    protected string CallLaneDirectionToolReset(string input)
    {
        if (m_SelectedEntity != Entity.Null)
        {
            EntityManager.RemoveComponent<CustomLaneDirection>(m_SelectedEntity);
        }
        return "";
    }

    protected string CallKeyPress(string value)
    {
        var definition = new { ctrlKey = false, key = "" };
        var keyPressEvent = JsonConvert.DeserializeAnonymousType(value, definition);
        if (keyPressEvent.ctrlKey && keyPressEvent.key == "S")
        {
            if (!m_SelectedEntity.Equals(Entity.Null))
            {
                CallMainPanelSave("");
            }
        }
        return "";
    }

    protected string CallAddWorldPosition(string input)
    {
        UITypes.WorldPosition[] posArray = JsonConvert.DeserializeObject<UITypes.WorldPosition[]>(input);
        foreach (var pos in posArray)
        {
            m_WorldPositionList.Add(pos);
        }
        m_CameraPosition = float.MaxValue; // Trigger binding update
        return "";
    }

    protected string CallRemoveWorldPosition(string input)
    {
        UITypes.WorldPosition[] posArray = JsonConvert.DeserializeObject<UITypes.WorldPosition[]>(input);
        foreach (var pos in posArray)
        {
            m_WorldPositionList.Remove(pos);
        }
        m_CameraPosition = float.MaxValue; // Trigger binding update
        return "";
    }

    protected Dictionary<string, UITypes.ScreenPoint> GetScreenPoint()
    {
        Dictionary<string, UITypes.ScreenPoint> screenPointDictionary = [];
        m_Camera = Camera.main;
        m_ScreenHeight = Screen.height;
        foreach (var wp in m_WorldPositionList)
        {
            if (!screenPointDictionary.ContainsKey(wp))
            {
                screenPointDictionary[wp] = new UITypes.ScreenPoint(m_Camera.WorldToScreenPoint(wp), m_ScreenHeight);
            }
        }
        return screenPointDictionary;
    }

    public void UpdateEntity()
    {
        if (m_SelectedEntity != Entity.Null)
        {
            if (!EntityManager.HasComponent<CustomTrafficLights>(m_SelectedEntity))
            {
                EntityManager.AddComponentData(m_SelectedEntity, m_CustomTrafficLights);
            }
            else
            {
                EntityManager.SetComponentData<CustomTrafficLights>(m_SelectedEntity, m_CustomTrafficLights);
            }

            if (m_CustomTrafficLights.GetPatternOnly() == CustomTrafficLights.Patterns.ModDefault)
            {
                EntityManager.RemoveComponent<CustomTrafficLights>(m_SelectedEntity);
            }

            EntityManager.AddComponentData(m_SelectedEntity, default(Updated));
        }
    }

    protected void ResetMainPanelState()
    {
        m_CustomTrafficLights = new CustomTrafficLights(CustomTrafficLights.Patterns.ModDefault);

        if (EntityManager.HasComponent<CustomTrafficLights>(m_SelectedEntity))
        {
            m_CustomTrafficLights = EntityManager.GetComponentData<CustomTrafficLights>(m_SelectedEntity);
        }
    }

    public void ChangeSelectedEntity(Entity entity)
    {
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
                SetMainPanelState(MainPanelState.Main);
            }
            else
            {
                SetMainPanelState(MainPanelState.Empty);
            }

            m_SelectedEntity = entity;

            ResetMainPanelState();

            UpdateEdgeInfo(m_SelectedEntity);
        }
    }
}