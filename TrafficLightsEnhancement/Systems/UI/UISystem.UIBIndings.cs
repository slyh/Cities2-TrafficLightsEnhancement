using System.Collections;
using System.Collections.Generic;
using C2VM.TrafficLightsEnhancement.Components;
using C2VM.TrafficLightsEnhancement.Systems.TrafficLightSystems.Initialisation;
using C2VM.TrafficLightsEnhancement.Utils;
using Colossal.Entities;
using Colossal.UI.Binding;
using Game.Net;
using Game.UI;
using Newtonsoft.Json;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;

namespace C2VM.TrafficLightsEnhancement.Systems.UI;

public partial class UISystem : UISystemBase
{
    public static GetterValueBinding<string> m_MainPanelBinding { get; private set; }

    private static GetterValueBinding<string> m_LocaleBinding;

    private GetterValueBinding<string> m_CityConfigurationBinding;

    private GetterValueBinding<Dictionary<string, UITypes.ScreenPoint>> m_ScreenPointBinding;

    private GetterValueBinding<Dictionary<Entity, NativeArray<NodeUtils.EdgeInfo>>> m_EdgeInfoBinding;

    private ValueBinding<int> m_ActiveEditingCustomPhaseIndexBinding;

    private void AddUIBindings()
    {
        AddBinding(m_MainPanelBinding = new GetterValueBinding<string>("C2VM.TLE", "GetMainPanel", GetMainPanel));
        AddBinding(m_LocaleBinding = new GetterValueBinding<string>("C2VM.TLE", "GetLocale", GetLocale));
        AddBinding(m_CityConfigurationBinding = new GetterValueBinding<string>("C2VM.TLE", "GetCityConfiguration", GetCityConfiguration));
        AddBinding(m_ScreenPointBinding = new GetterValueBinding<Dictionary<string, UITypes.ScreenPoint>>("C2VM.TLE", "GetScreenPoint", GetScreenPoint, new DictionaryWriter<string, UITypes.ScreenPoint>(null, new ValueWriter<UITypes.ScreenPoint>()), new JsonWriter.FalseEqualityComparer<Dictionary<string, UITypes.ScreenPoint>>()));
        AddBinding(m_EdgeInfoBinding = new GetterValueBinding<Dictionary<Entity, NativeArray<NodeUtils.EdgeInfo>>>("C2VM.TLE", "GetEdgeInfo", GetEdgeInfo, new JsonWriter.EdgeInfoWriter(), new JsonWriter.FalseEqualityComparer<Dictionary<Entity, NativeArray<NodeUtils.EdgeInfo>>>()));
        AddBinding(m_ActiveEditingCustomPhaseIndexBinding = new ValueBinding<int>("C2VM.TLE", "GetActiveEditingCustomPhaseIndex", -1));

        AddBinding(new CallBinding<string, string>("C2VM.TLE", "CallMainPanelUpdatePattern", CallMainPanelUpdatePattern));
        AddBinding(new CallBinding<string, string>("C2VM.TLE", "CallMainPanelUpdateOption", CallMainPanelUpdateOption));
        AddBinding(new CallBinding<string, string>("C2VM.TLE", "CallMainPanelUpdateValue", CallMainPanelUpdateValue));
        AddBinding(new CallBinding<string, string>("C2VM.TLE", "CallMainPanelUpdatePosition", CallMainPanelUpdatePosition));
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

    protected string GetMainPanel()
    {
        var menu = new
        {
            title = Mod.IsCanary() ? "TLE Canary" : "Traffic Lights Enhancement",
            image = "coui://GameUI/Media/Game/Icons/TrafficLights.svg",
            position = m_MainPanelPosition,
            showPanel = m_MainPanelState != MainPanelState.Hidden,
            showFloatingButton = true,
            state = m_MainPanelState,
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

    public static string GetLocale()
    {
        var result = new
        {
            locale = GetLocaleCode(),
        };

        return JsonConvert.SerializeObject(result);
    }

    public string GetCityConfiguration()
    {
        var result = new
        {
            leftHandTraffic = m_CityConfigurationSystem.leftHandTraffic,
        };

        return JsonConvert.SerializeObject(result);
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

    protected Dictionary<Entity, NativeArray<NodeUtils.EdgeInfo>> GetEdgeInfo()
    {
        return m_EdgeInfoDictionary;
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
        foreach (CustomTrafficLights.Patterns pattern in System.Enum.GetValues(typeof(CustomTrafficLights.Patterns)))
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

    protected string CallMainPanelUpdatePosition(string jsonString)
    {
        m_MainPanelPosition = JsonConvert.DeserializeObject<UITypes.ScreenPoint>(jsonString);
        m_MainPanelBinding.Update();
        return "";
    }

    protected string CallMainPanelSave(string value)
    {
        SaveSelectedEntity();
        return "";
    }

    protected string CallLaneDirectionToolReset(string input)
    {
        if (m_SelectedEntity != Entity.Null)
        {
            EntityManager.RemoveComponent<CommonLibraries.LaneSystem.CustomLaneDirection>(m_SelectedEntity);
        }
        return "";
    }

    protected string CallSetMainPanelState(string input)
    {
        var definition = new { key = "", value = "" };
        var value = JsonConvert.DeserializeAnonymousType(input, definition);
        MainPanelState state = (MainPanelState)System.Int32.Parse(value.value);
        SetMainPanelState(state);
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

    protected string CallSetActiveEditingCustomPhaseIndex(string input)
    {
        var definition = new { index = 0 };
        var value = JsonConvert.DeserializeAnonymousType(input, definition);
        m_ActiveEditingCustomPhaseIndexBinding.Update(value.index);
        m_MainPanelBinding.Update();
        UpdateEntity();
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

    protected string CallKeyPress(string value)
    {
        var definition = new { ctrlKey = false, key = "" };
        var keyPressEvent = JsonConvert.DeserializeAnonymousType(value, definition);
        if (keyPressEvent.ctrlKey && keyPressEvent.key == "S")
        {
            if (!m_SelectedEntity.Equals(Entity.Null))
            {
                SaveSelectedEntity();
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

    protected string CallOpenBrowser(string jsonString)
    {
        var keyDefinition = new { key = "", value = "" };
        var parsedKey = JsonConvert.DeserializeAnonymousType(jsonString, keyDefinition);
        System.Diagnostics.Process.Start(parsedKey.value);
        return "";
    }
}