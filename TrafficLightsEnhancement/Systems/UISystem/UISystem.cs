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

    private int m_Ways;

    private CustomTrafficLights m_CustomTrafficLights;

    private Game.City.CityConfigurationSystem m_CityConfigurationSystem;

    private LDTRetirementSystem m_LdtRetirementSystem;

    private Game.Simulation.SimulationSystem m_SimulationSystem;

    private RenderSystem.RenderSystem m_RenderSystem;

    private Entity m_TrafficLightsAssetEntity = Entity.Null;

    private Camera m_Camera;

    private int m_ScreenHeight;

    private CameraUpdateSystem m_CameraUpdateSystem;

    private float3 m_CameraPosition;

    private List<Types.WorldPosition> m_WorldPositionList;

    private uint m_LastUpdateFrameIndex;

    private GetterValueBinding<string> m_MainPanelBinding;

    private static GetterValueBinding<string> m_LocaleBinding;

    private GetterValueBinding<string> m_CityConfigurationBinding;

    private GetterValueBinding<Dictionary<string, Types.ScreenPoint>> m_ScreenPointBinding;

    private GetterValueBinding<string> m_EdgeInfoBinding;

    private ValueBinding<int> m_ActiveEditingCustomPhaseIndexBinding;

    protected override void OnCreate()
    {
        base.OnCreate();

        m_Camera = Camera.main;
        m_ScreenHeight = Screen.height;

        m_WorldPositionList = [];

        m_CameraUpdateSystem = World.GetOrCreateSystemManaged<CameraUpdateSystem>();
        m_CityConfigurationSystem = World.GetOrCreateSystemManaged<Game.City.CityConfigurationSystem>();
        m_LdtRetirementSystem = World.GetOrCreateSystemManaged<LDTRetirementSystem>();
        m_RenderSystem = World.GetOrCreateSystemManaged<RenderSystem.RenderSystem>();
        m_SimulationSystem = World.GetOrCreateSystemManaged<Game.Simulation.SimulationSystem>();

        AddBinding(m_MainPanelBinding = new GetterValueBinding<string>("C2VM.TLE", "GetMainPanel", GetMainPanel));
        AddBinding(m_LocaleBinding = new GetterValueBinding<string>("C2VM.TLE", "GetLocale", GetLocale));
        AddBinding(m_CityConfigurationBinding = new GetterValueBinding<string>("C2VM.TLE", "GetCityConfiguration", GetCityConfiguration));
        AddBinding(m_ScreenPointBinding = new GetterValueBinding<Dictionary<string, Types.ScreenPoint>>("C2VM.TLE", "GetScreenPoint", GetScreenPoint, new DictionaryWriter<string, Types.ScreenPoint>(null, new ValueWriter<Types.ScreenPoint>())));
        AddBinding(m_EdgeInfoBinding = new GetterValueBinding<string>("C2VM.TLE", "GetEdgeInfo", GetEdgeInfo));
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
    }

    protected override void OnUpdate()
    {
        if (m_WorldPositionList.Count > 0 && !m_CameraPosition.Equals(m_CameraUpdateSystem.position))
        {
            m_CameraPosition = m_CameraUpdateSystem.position;
            m_ScreenPointBinding.Update();
        }
        if (math.abs(m_SimulationSystem.frameIndex - m_LastUpdateFrameIndex) >= 15)
        {
            RedrawGizmo();
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

    public void RedrawGizmo()
    {
        if (m_SelectedEntity != Entity.Null)
        {
            m_LastUpdateFrameIndex = m_SimulationSystem.frameIndex;
            if (m_MainPanelState == MainPanelState.CustomPhase)
            {
                m_MainPanelBinding.Update();
            }
            m_RenderSystem.ClearMesh();
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
            m_RenderSystem.BuildMesh();
        }
    }

    public void SetMainPanelState(MainPanelState state)
    {
        UpdateEntity();
        m_MainPanelState = state;
        m_MainPanelBinding.Update();
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
        string locale = Localisations.Helper.GetAutoLocale(GameManager.instance.localizationManager.activeLocaleId, CultureInfo.CurrentCulture.Name);
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
        Localisations.Helper localisationsHelper = new Localisations.Helper(GetLocaleCode());
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
        var keyDefinition = new {
            data = new { key = "", value = "" }
        };
        var parsedKey = JsonConvert.DeserializeAnonymousType(jsonString, keyDefinition);
        System.Diagnostics.Process.Start(parsedKey.data.value);
        return "";
    }

    protected string CallMainPanelUpdatePattern(string input)
    {
        Types.ItemRadio pattern = JsonConvert.DeserializeObject<Types.ItemRadio>(input);
        m_CustomTrafficLights.SetPattern(((uint)m_CustomTrafficLights.GetPattern(m_Ways) & 0xFFFF0000) | uint.Parse(pattern.value));
        if (m_CustomTrafficLights.GetPatternOnly(m_Ways) != TrafficLightPatterns.Pattern.Vanilla)
        {
            m_CustomTrafficLights.SetPattern(m_CustomTrafficLights.GetPattern(m_Ways) & ~TrafficLightPatterns.Pattern.CentreTurnGiveWay);
        }
        if (m_CustomTrafficLights.GetPatternOnly(m_Ways) == TrafficLightPatterns.Pattern.CustomPhase)
        {
            if (!EntityManager.HasBuffer<CustomPhaseData>(m_SelectedEntity))
            {
                EntityManager.AddBuffer<CustomPhaseData>(m_SelectedEntity);
            }
            if (!EntityManager.HasBuffer<EdgeGroupMask>(m_SelectedEntity))
            {
                EntityManager.AddBuffer<EdgeGroupMask>(m_SelectedEntity);
            }
            m_CustomTrafficLights.SetPattern(TrafficLightPatterns.Pattern.CustomPhase);
        }
        UpdateEntity();
        m_MainPanelBinding.Update();
        return "";
    }

    protected string CallMainPanelUpdateOption(string input)
    {
        Types.ItemCheckbox option = JsonConvert.DeserializeObject<Types.ItemCheckbox>(input);
        foreach (TrafficLightPatterns.Pattern pattern in Enum.GetValues(typeof(TrafficLightPatterns.Pattern)))
        {
            if (((uint) pattern & 0xFFFF0000) != 0)
            {
                if (uint.Parse(option.key) == (uint)pattern)
                {
                    // Toggle the option
                    m_CustomTrafficLights.SetPattern(m_CustomTrafficLights.GetPattern(m_Ways) ^ pattern);
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
            menu.items.Add(new Types.ItemTitle{title = "TrafficSignal"});
            menu.items.Add(Types.MainPanelItemPattern("ModDefault", (uint)TrafficLightPatterns.Pattern.ModDefault, (uint)m_CustomTrafficLights.GetPattern(m_Ways)));
            menu.items.Add(Types.MainPanelItemPattern("Vanilla", (uint)TrafficLightPatterns.Pattern.Vanilla, (uint)m_CustomTrafficLights.GetPattern(m_Ways)));
            if (TrafficLightPatterns.IsValidPattern(m_Ways, TrafficLightPatterns.Pattern.SplitPhasing))
            {
                menu.items.Add(Types.MainPanelItemPattern("SplitPhasing", (uint)TrafficLightPatterns.Pattern.SplitPhasing, (uint)m_CustomTrafficLights.GetPattern(m_Ways)));
            }
            if (TrafficLightPatterns.IsValidPattern(m_Ways, TrafficLightPatterns.Pattern.SplitPhasingAdvanced))
            {
                menu.items.Add(Types.MainPanelItemPattern("AdvancedSplitPhasing", (uint)TrafficLightPatterns.Pattern.SplitPhasingAdvanced, (uint)m_CustomTrafficLights.GetPattern(m_Ways)));
            }
            if (TrafficLightPatterns.IsValidPattern(m_Ways, TrafficLightPatterns.Pattern.ProtectedCentreTurn))
            {
                if (m_CityConfigurationSystem.leftHandTraffic)
                {
                    menu.items.Add(Types.MainPanelItemPattern("ProtectedRightTurns", (uint)TrafficLightPatterns.Pattern.ProtectedCentreTurn, (uint)m_CustomTrafficLights.GetPattern(m_Ways)));
                }
                else
                {
                    menu.items.Add(Types.MainPanelItemPattern("ProtectedLeftTurns", (uint)TrafficLightPatterns.Pattern.ProtectedCentreTurn, (uint)m_CustomTrafficLights.GetPattern(m_Ways)));
                }
            }
            menu.items.Add(Types.MainPanelItemPattern("CustomPhase", (uint)TrafficLightPatterns.Pattern.CustomPhase, (uint)m_CustomTrafficLights.GetPattern(m_Ways)));
            if (m_CustomTrafficLights.GetPatternOnly(m_Ways) == TrafficLightPatterns.Pattern.CustomPhase)
            {
                menu.items.Add(default(Types.ItemDivider));
                menu.items.Add(new Types.ItemTitle{title = "CustomPhase"});
                menu.items.Add(new Types.ItemButton{label = "Setup", key = "state", value = "3", engineEventName = "C2VM.TLE.CallSetMainPanelState"});
            }
            if (m_CustomTrafficLights.GetPatternOnly(m_Ways) < TrafficLightPatterns.Pattern.ModDefault)
            {
                menu.items.Add(default(Types.ItemDivider));
                menu.items.Add(new Types.ItemTitle{title = "Options"});
                menu.items.Add(Types.MainPanelItemOption("AllowTurningOnRed", (uint)TrafficLightPatterns.Pattern.AlwaysGreenKerbsideTurn, (uint)m_CustomTrafficLights.GetPattern(m_Ways)));
                if (m_CustomTrafficLights.GetPatternOnly(m_Ways) == TrafficLightPatterns.Pattern.Vanilla)
                {
                    menu.items.Add(Types.MainPanelItemOption("GiveWayToOncomingVehicles", (uint)TrafficLightPatterns.Pattern.CentreTurnGiveWay, (uint)m_CustomTrafficLights.GetPattern(m_Ways)));
                }
                menu.items.Add(Types.MainPanelItemOption("ExclusivePedestrianPhase", (uint)TrafficLightPatterns.Pattern.ExclusivePedestrian, (uint)m_CustomTrafficLights.GetPattern(m_Ways)));
                if (((uint)m_CustomTrafficLights.GetPattern(m_Ways) & (uint)TrafficLightPatterns.Pattern.ExclusivePedestrian) != 0)
                {
                    menu.items.Add(default(Types.ItemDivider));
                    menu.items.Add(new Types.ItemRange
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
            menu.items.Add(default(Types.ItemDivider));
            if (m_LdtRetirementSystem.m_UnmigratedNodeCount > 0)
            {
                menu.items.Add(new Types.ItemTitle{title = "LaneDirectionTool"});
                menu.items.Add(new Types.ItemButton{label = "Reset", key = "status", value = "0", engineEventName = "C2VM.TLE.CallLaneDirectionToolReset"});
                menu.items.Add(default(Types.ItemDivider));
                menu.items.Add(new Types.ItemNotification{label = "LdtMigrationNotice", notificationType = "notice", value = LDTRetirementSystem.kRetirementNoticeLink, engineEventName = "C2VM.TLE.CallOpenBrowser"});
                menu.items.Add(default(Types.ItemDivider));
            }
            else if (Mod.m_Settings != null && !Mod.m_Settings.m_HasReadLdtRetirementNotice)
            {
                menu.items.Add(new Types.ItemNotification{label = "LdtRetirementNotice", notificationType = "notice"});
                menu.items.Add(default(Types.ItemDivider));
            }
            menu.items.Add(new Types.ItemButton{label = "Save", key = "save", value = "1", engineEventName = "C2VM.TLE.CallMainPanelSave"});
            if (m_ShowNotificationUnsaved)
            {
                menu.items.Add(default(Types.ItemDivider));
                menu.items.Add(new Types.ItemNotification{label = "PleaseSave", notificationType = "warning"});
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
                menu.items.Add(new Types.ItemCustomPhase{
                    activeIndex = m_ActiveEditingCustomPhaseIndexBinding.value,
                    currentSignalGroup = trafficLights.m_CurrentSignalGroup,
                    index = i,
                    length = customPhaseDataBuffer.Length,
                    minimumDurationMultiplier = customPhaseDataBuffer[i].m_MinimumDurationMultiplier
                });
            }
            if (customPhaseDataBuffer.Length < 16)
            {
                menu.items.Add(new Types.ItemButton{label = "Add", key = "add", value = "add", engineEventName = "C2VM.TLE.CallAddCustomPhase"});
            }
            menu.items.Add(new Types.ItemButton{label = "Save", key = "state", value = "2", engineEventName = "C2VM.TLE.CallSetMainPanelState"});
        }
        else if (m_MainPanelState == MainPanelState.Empty)
        {
            menu.items.Add(new Types.ItemMessage{message = "PleaseSelectJunction"});
        }
        if (Mod.IsCanary() && Mod.m_Settings != null && Mod.m_Settings.m_SuppressCanaryWarningVersion != Mod.m_InformationalVersion)
        {
            menu.items.Add(default(Types.ItemDivider));
            menu.items.Add(new Types.ItemNotification{label = "CanaryBuildWarning", notificationType = "warning"});
        }
        string result = JsonConvert.SerializeObject(menu);
        return result;
    }

    protected string GetEdgeInfo()
    {
        if (m_SelectedEntity.Equals(Entity.Null))
        {
            return "[]";
        }
        var edgeInfoNativeList = NodeUtils.GetEdgeInfoList(Allocator.Temp, EntityManager, m_SelectedEntity);
        return JsonConvert.SerializeObject(edgeInfoNativeList.AsArray());
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
            m_EdgeInfoBinding.Update();
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
            if (!EntityManager.TryGetBuffer(m_SelectedEntity, false, out edgeGroupMaskBuffer))
            {
                edgeGroupMaskBuffer = EntityManager.AddBuffer<EdgeGroupMask>(m_SelectedEntity);
            }
            CustomPhaseUtils.SwapBit(edgeGroupMaskBuffer, value.index, 16);

            DynamicBuffer<SubLaneGroupMask> subLaneGroupMaskBuffer;
            if (!EntityManager.TryGetBuffer(m_SelectedEntity, false, out subLaneGroupMaskBuffer))
            {
                subLaneGroupMaskBuffer = EntityManager.AddBuffer<SubLaneGroupMask>(m_SelectedEntity);
            }
            CustomPhaseUtils.SwapBit(subLaneGroupMaskBuffer, value.index, 16);

            if (m_ActiveEditingCustomPhaseIndexBinding.value >= customPhaseDataBuffer.Length)
            {
                m_ActiveEditingCustomPhaseIndexBinding.Update(customPhaseDataBuffer.Length - 1);
            }

            m_MainPanelBinding.Update();
            m_EdgeInfoBinding.Update();

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
            m_EdgeInfoBinding.Update();

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
                m_EdgeInfoBinding.Update();
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

        m_EdgeInfoBinding.Update();
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

        m_EdgeInfoBinding.Update();
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
        Types.WorldPosition[] posArray = JsonConvert.DeserializeObject<Types.WorldPosition[]>(input);
        foreach (var pos in posArray)
        {
            m_WorldPositionList.Add(pos);
        }
        m_CameraPosition = float.MaxValue; // Trigger binding update
        return "";
    }

    protected string CallRemoveWorldPosition(string input)
    {
        Types.WorldPosition[] posArray = JsonConvert.DeserializeObject<Types.WorldPosition[]>(input);
        foreach (var pos in posArray)
        {
            m_WorldPositionList.Remove(pos);
        }
        m_CameraPosition = float.MaxValue; // Trigger binding update
        return "";
    }

    protected Dictionary<string, Types.ScreenPoint> GetScreenPoint()
    {
        Dictionary<string, Types.ScreenPoint> screenPointDictionary = [];
        m_Camera = Camera.main;
        m_ScreenHeight = Screen.height;
        foreach (var wp in m_WorldPositionList)
        {
            if (!screenPointDictionary.ContainsKey(wp))
            {
                screenPointDictionary[wp] = new Types.ScreenPoint(m_Camera.WorldToScreenPoint(wp), m_ScreenHeight);
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

            if (m_CustomTrafficLights.GetPatternOnly(m_Ways) == TrafficLightPatterns.Pattern.ModDefault)
            {
                EntityManager.RemoveComponent<CustomTrafficLights>(m_SelectedEntity);
            }

            EntityManager.AddComponentData(m_SelectedEntity, default(Updated));
        }
    }

    protected void ResetMainPanelState()
    {
        m_CustomTrafficLights = new CustomTrafficLights(TrafficLightPatterns.Pattern.ModDefault);

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
            m_RenderSystem.ClearMesh();

            // Clean up old entity
            if (EntityManager.HasBuffer<ConnectPositionSource>(m_SelectedEntity))
            {
                EntityManager.RemoveComponent<ConnectPositionSource>(m_SelectedEntity);
            }

            if (EntityManager.HasBuffer<ConnectPositionTarget>(m_SelectedEntity))
            {
                EntityManager.RemoveComponent<ConnectPositionTarget>(m_SelectedEntity);
            }

            if (!entity.Equals(Entity.Null))
            {
                SetMainPanelState(MainPanelState.Main);
            }
            else
            {
                SetMainPanelState(MainPanelState.Empty);
            }

            m_SelectedEntity = entity;

            m_Ways = 0;

            // Retrieve info of new entity
            if (EntityManager.HasBuffer<SubLane>(m_SelectedEntity))
            {
                Dictionary<float3, bool> lanes = new Dictionary<float3, bool>();
                DynamicBuffer<SubLane> buffer = EntityManager.GetBuffer<SubLane>(m_SelectedEntity);
                foreach (SubLane subLane in buffer)
                {
                    if (EntityManager.HasComponent<SecondaryLane>(subLane.m_SubLane))
                    {
                        continue;
                    }

                    if (EntityManager.HasComponent<PedestrianLane>(subLane.m_SubLane))
                    {
                        continue;
                    }

                    if (!EntityManager.HasComponent<CarLane>(subLane.m_SubLane) && !EntityManager.HasComponent<TrackLane>(subLane.m_SubLane))
                    {
                        continue;
                    }

                    if (EntityManager.HasComponent<MasterLane>(subLane.m_SubLane) || !EntityManager.HasComponent<SlaveLane>(subLane.m_SubLane))
                    {
                        if (EntityManager.HasComponent<Curve>(subLane.m_SubLane))
                        {
                            Curve curve = EntityManager.GetComponentData<Curve>(subLane.m_SubLane);
                            if (lanes.ContainsKey(curve.m_Bezier.a))
                            {
                                continue;
                            }
                            lanes[curve.m_Bezier.a] = true;
                        }
                    }
                }
                m_Ways = lanes.Count;
            }

            ResetMainPanelState();

            m_MainPanelBinding.Update();
            m_EdgeInfoBinding.Update();
        }
    }
}