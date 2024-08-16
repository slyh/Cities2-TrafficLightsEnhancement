using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using C2VM.CommonLibraries.LaneSystem;
using C2VM.TrafficLightsEnhancement.Components;
using C2VM.TrafficLightsEnhancement.Systems.TrafficLightInitializationSystem;
using Colossal.UI.Binding;
using Game;
using Game.Common;
using Game.Net;
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
    public bool m_IsLaneManagementToolOpen;

    public bool m_ShowNotificationUnsaved;

    private bool m_ShouldShowPanel;

    public Entity m_SelectedEntity;

    private int m_Ways;

    private CustomTrafficLights m_CustomTrafficLights;

    private Game.City.CityConfigurationSystem m_CityConfigurationSystem;

    private LDTRetirementSystem m_LdtRetirementSystem;

    private Entity m_TrafficLightsAssetEntity = Entity.Null;

    private GetterValueBinding<string> m_MainPanelBinding;

    private static GetterValueBinding<string> m_LocaleBinding;

    protected override void OnCreate()
    {
        base.OnCreate();

        m_CityConfigurationSystem = World.GetOrCreateSystemManaged<Game.City.CityConfigurationSystem>();
        m_LdtRetirementSystem = World.GetOrCreateSystemManaged<LDTRetirementSystem>();

        AddBinding(m_MainPanelBinding = new GetterValueBinding<string>("C2VM.TLE", "GetterMainPanel", GetterMainPanel));
        AddBinding(m_LocaleBinding = new GetterValueBinding<string>("C2VM.TLE", "GetterLocale", GetterLocale));
        AddBinding(new CallBinding<string, string>("C2VM.TLE", "CallMainPanelUpdatePattern", CallMainPanelUpdatePattern));
        AddBinding(new CallBinding<string, string>("C2VM.TLE", "CallMainPanelUpdateOption", CallMainPanelUpdateOption));
        AddBinding(new CallBinding<string, string>("C2VM.TLE", "CallMainPanelUpdateValue", CallMainPanelUpdateValue));
        AddBinding(new CallBinding<string, string>("C2VM.TLE", "CallMainPanelSave", CallMainPanelSave));
        // AddBinding(new CallBinding<string, string>("C2VM.TLE", "CallLaneDirectionToolOpen", CallLaneDirectionToolOpen));
        // AddBinding(new CallBinding<string, string>("C2VM.TLE", "CallLaneDirectionToolClose", CallLaneDirectionToolClose));
        AddBinding(new CallBinding<string, string>("C2VM.TLE", "CallLaneDirectionToolReset", CallLaneDirectionToolReset));
        AddBinding(new CallBinding<string, string>("C2VM.TLE", "CallLaneDirectionToolPanelSave", CallLaneDirectionToolPanelSave));

        AddBinding(new CallBinding<string, string>("C2VM.TLE", "CallKeyPress", CallKeyPress));
        AddBinding(new CallBinding<string, string>("C2VM.TLE", "CallTranslatePosition", CallTranslatePosition));

        AddBinding(new CallBinding<string, string>("C2VM.TLE", "CallOpenBrowser", CallOpenBrowser));
    }

    protected override void OnUpdate()
    {
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
    }

    public void ShouldShowPanel(bool should)
    {
        m_ShouldShowPanel = should;
        if (m_ShouldShowPanel)
        {
            m_MainPanelBinding.Update();
        }
        else
        {
            CallMainPanelSave("");
        }
    }

    public static string GetLocale()
    {
        string locale = Localisations.Helper.GetAutoLocale(GameManager.instance.localizationManager.activeLocaleId, CultureInfo.CurrentCulture.Name);
        if (Mod.m_Settings != null && Mod.m_Settings.m_Locale != "auto")
        {
            locale = Mod.m_Settings.m_Locale;
        }
        return locale;
    }

    public static string GetterLocale()
    {
        var result = new
        {
            locale = GetLocale(),
        };

        return JsonConvert.SerializeObject(result);
    }

    public static void UpdateLocale()
    {
        Localisations.Helper localisationsHelper = new Localisations.Helper(GetLocale());
        localisationsHelper.AddToDictionary(GameManager.instance.localizationManager.activeDictionary);

        if (m_LocaleBinding != null)
        {
            m_LocaleBinding.Update();
        }
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
        if (((uint)m_CustomTrafficLights.GetPattern(m_Ways) & 0xFFFF) != (uint)TrafficLightPatterns.Pattern.Vanilla)
        {
            m_CustomTrafficLights.SetPattern(m_CustomTrafficLights.GetPattern(m_Ways) & ~TrafficLightPatterns.Pattern.CentreTurnGiveWay);
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
        // CallLaneDirectionToolClose("");
        return "";
    }

    protected string GetterMainPanel()
    {
        var menu = new {
            title = "Traffic Lights Enhancement",
            image = "coui://GameUI/Media/Game/Icons/TrafficLights.svg",
            showPanel = m_ShouldShowPanel,
            showFloatingButton = Mod.m_Settings != null && Mod.m_Settings.m_ShowFloatingButton,
            trafficLightsAssetEntityIndex = m_TrafficLightsAssetEntity.Index,
            trafficLightsAssetEntityVersion = m_TrafficLightsAssetEntity.Version,
            items = new ArrayList()
        };
        if (m_SelectedEntity != Entity.Null)
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
            if (((uint)m_CustomTrafficLights.GetPattern(m_Ways) & 0xFFFF) != (uint)TrafficLightPatterns.Pattern.ModDefault)
            {
                menu.items.Add(default(Types.ItemDivider));
                menu.items.Add(new Types.ItemTitle{title = "Options"});
                menu.items.Add(Types.MainPanelItemOption("AllowTurningOnRed", (uint)TrafficLightPatterns.Pattern.AlwaysGreenKerbsideTurn, (uint)m_CustomTrafficLights.GetPattern(m_Ways)));
                if (((uint)m_CustomTrafficLights.GetPattern(m_Ways) & 0xFFFF) == (uint)TrafficLightPatterns.Pattern.Vanilla)
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
        else
        {
            menu.items.Add(new Types.ItemMessage{message = "PleaseSelectJunction"});
        }
        #if SHOW_CANARY_BUILD_WARNING
        menu.items.Add(default(Types.ItemDivider));
        menu.items.Add(new Types.ItemNotification{label = "CanaryBuildWarning", notificationType = "warning"});
        #endif
        string result = JsonConvert.SerializeObject(menu);
        return result;
    }

    // protected string CallLaneDirectionToolOpen(string input)
    // {
    //     m_IsLaneManagementToolOpen = true;
    //     UpdateLaneDirectionTool();
    //     UpdateMainPanel();
    //     UpdateEntity();
    //     return "";
    // }

    // protected string CallLaneDirectionToolClose(string input)
    // {
    //     m_IsLaneManagementToolOpen = false;
    //     UpdateLaneDirectionTool();
    //     UpdateMainPanel();
    //     UpdateEntity();
    //     return "";
    // }

    protected string CallLaneDirectionToolReset(string input)
    {
        if (m_SelectedEntity != Entity.Null)
        {
            EntityManager.RemoveComponent<CustomLaneDirection>(m_SelectedEntity);
            // CallLaneDirectionToolClose("");
        }
        return "";
    }

    protected void UpdateLaneDirectionTool()
    {
        var result = new
        {
            buttons = new List<Types.LaneToolButton>(),
            panels = new List<Types.LaneDirectionToolPanel>()
        };

        if (m_IsLaneManagementToolOpen && m_SelectedEntity != Entity.Null)
        {
            if (EntityManager.HasBuffer<ConnectPositionSource>(m_SelectedEntity))
            {
                DynamicBuffer<ConnectPositionSource> connectPositionSourceBuffer = EntityManager.GetBuffer<ConnectPositionSource>(m_SelectedEntity);
                Dictionary<float3, bool> sourceExist = new Dictionary<float3, bool>();
                for (int i = 0; i < connectPositionSourceBuffer.Length; i++)
                {
                    sourceExist.Add(connectPositionSourceBuffer[i].m_Position, true);
                }

                DynamicBuffer<CustomLaneDirection> customLaneDirectionBuffer;
                // Remove CustomLaneDirection that is no longer exists
                if (EntityManager.HasBuffer<CustomLaneDirection>(m_SelectedEntity))
                {
                    customLaneDirectionBuffer = EntityManager.GetBuffer<CustomLaneDirection>(m_SelectedEntity);
                    for (int i = 0; i < customLaneDirectionBuffer.Length; i++)
                    {
                        float3 customLaneDirectionPosition = customLaneDirectionBuffer[i].m_Position;
                        if (!sourceExist.ContainsKey(customLaneDirectionPosition))
                        {
                            customLaneDirectionBuffer.RemoveAt(i);
                            i--;
                        }
                    }
                }
                // Build default config if CustomLaneDirection doesn't exist
                else
                {
                    customLaneDirectionBuffer = EntityManager.AddBuffer<CustomLaneDirection>(m_SelectedEntity);
                    DefaultLaneDirection.Build(ref customLaneDirectionBuffer, ref connectPositionSourceBuffer);
                }

                for (int i = 0; i < connectPositionSourceBuffer.Length; i++)
                {
                    result.buttons.Add(new Types.LaneToolButton(
                        new Types.WorldPosition
                        {
                            x = connectPositionSourceBuffer[i].m_Position.x,
                            y = connectPositionSourceBuffer[i].m_Position.y,
                            z = connectPositionSourceBuffer[i].m_Position.z
                        },
                        true,
                        ""
                    ));
                    result.panels.Add(new Types.LaneDirectionToolPanel
                    {
                        title = "Lane Direction",
                        image = "Media/Game/Icons/RoadsServices.svg",
                        visible = true,
                        position = new Types.WorldPosition
                        {
                            x = connectPositionSourceBuffer[i].m_Position.x,
                            y = connectPositionSourceBuffer[i].m_Position.y,
                            z = connectPositionSourceBuffer[i].m_Position.z
                        },
                        lanes = [],
                        items =
                        [
                            new Types.ItemButton
                            {
                                label = "Save",
                                engineEventName = "C2VM.TLE.CallLaneDirectionToolPanelSave"
                            }
                        ]
                    });
                    for (int j = i; j < connectPositionSourceBuffer.Length; j++)
                    {
                        if (connectPositionSourceBuffer[j].m_Owner != connectPositionSourceBuffer[i].m_Owner || connectPositionSourceBuffer[j].m_GroupIndex != connectPositionSourceBuffer[i].m_GroupIndex)
                        {
                            i = j - 1;
                            break;
                        }
                        CustomLaneDirection.Get(customLaneDirectionBuffer, connectPositionSourceBuffer[j].m_Position, connectPositionSourceBuffer[j].m_Tangent, connectPositionSourceBuffer[j].m_Owner, connectPositionSourceBuffer[j].m_GroupIndex, connectPositionSourceBuffer[j].m_LaneIndex, out CustomLaneDirection directionFound);
                        result.panels[result.panels.Count - 1].lanes.Add(
                            new Types.LaneDirection
                            {
                                position = new Types.WorldPosition
                                {
                                    x = connectPositionSourceBuffer[j].m_Position.x,
                                    y = connectPositionSourceBuffer[j].m_Position.y,
                                    z = connectPositionSourceBuffer[j].m_Position.z
                                },
                                leftHandTraffic = m_CityConfigurationSystem.leftHandTraffic,
                                label = "Lane " + (j - i + 1).ToString(),
                                banLeft = directionFound.m_Restriction.m_BanLeft,
                                banRight = directionFound.m_Restriction.m_BanRight,
                                banStraight = directionFound.m_Restriction.m_BanStraight,
                                banUTurn = directionFound.m_Restriction.m_BanUTurn
                            }
                        );
                        if (j == connectPositionSourceBuffer.Length - 1)
                        {
                            i = j;
                        }
                    }
                }
            }
        }

        // m_View.TriggerEvent("C2VM-TLE-Event-UpdateLaneDirectionTool", JsonConvert.SerializeObject(result));
    }

    protected string CallLaneDirectionToolPanelSave(string input)
    {
        Types.LaneDirection[] panel = JsonConvert.DeserializeObject<Types.LaneDirection[]>(input);

        if (m_SelectedEntity == Entity.Null)
        {
            return "";
        }

        foreach (Types.LaneDirection lane in panel)
        {
            CustomLaneDirection direction = new CustomLaneDirection
            {
                m_Position = new float3(lane.position.x, lane.position.y, lane.position.z),
                m_Restriction = new CustomLaneDirection.Restriction
                {
                    m_BanLeft = lane.banLeft,
                    m_BanRight = lane.banRight,
                    m_BanStraight = lane.banStraight,
                    m_BanUTurn = lane.banUTurn
                }
            };

            if (EntityManager.HasBuffer<ConnectPositionSource>(m_SelectedEntity))
            {
                DynamicBuffer<ConnectPositionSource> sourcePosBuffer = EntityManager.GetBuffer<ConnectPositionSource>(m_SelectedEntity);
                for (int i = 0; i < sourcePosBuffer.Length; i++)
                {
                    if (sourcePosBuffer[i].m_Position.Equals(direction.m_Position))
                    {
                        direction.m_Tangent = sourcePosBuffer[i].m_Tangent;
                        direction.m_Owner = sourcePosBuffer[i].m_Owner;
                        direction.m_GroupIndex = sourcePosBuffer[i].m_GroupIndex;
                        direction.m_LaneIndex = sourcePosBuffer[i].m_LaneIndex;
                        direction.m_Initialised = true;
                    }
                }
            }

            if (!direction.m_Initialised)
            {
                continue;
            }

            if (EntityManager.HasBuffer<CustomLaneDirection>(m_SelectedEntity))
            {
                DynamicBuffer<CustomLaneDirection> buffer = EntityManager.GetBuffer<CustomLaneDirection>(m_SelectedEntity);
                bool foundExistingDirection = false;
                for (int i = 0; i < buffer.Length; i++)
                {
                    CustomLaneDirection existingDirection = buffer[i];
                    if (existingDirection.Equals(direction))
                    {
                        buffer[i] = direction;
                        foundExistingDirection = true;
                        break;
                    }
                }
                if (!foundExistingDirection)
                {
                    buffer.Add(direction);
                }
            }
            else
            {
                DynamicBuffer<CustomLaneDirection> buffer = EntityManager.AddBuffer<CustomLaneDirection>(m_SelectedEntity);
                buffer.Add(direction);
            }
        }

        UpdateEntity();

        return "";
    }

    protected string CallKeyPress(string value)
    {
        var definition = new { ctrlKey = false, key = "" };
        var keyPressEvent = JsonConvert.DeserializeAnonymousType(value, definition);
        if (keyPressEvent.ctrlKey && keyPressEvent.key == "S")
        {
            if (m_IsLaneManagementToolOpen)
            {
                // CallLaneDirectionToolClose("");
            }
            else if (!m_SelectedEntity.Equals(Entity.Null))
            {
                CallMainPanelSave("");
            }
        }
        return "";
    }

    protected string CallTranslatePosition(string input)
    {
        Types.WorldPosition worldPos = JsonConvert.DeserializeObject<Types.WorldPosition>(input);
        float3 screenPos = Camera.main.WorldToScreenPoint(new float3(worldPos.x, worldPos.y, worldPos.z));
        return JsonConvert.SerializeObject(new Types.ScreenPosition{left = screenPos.x, top = Screen.height - screenPos.y});
    }

    protected void UpdateEntity()
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

            if (((uint)m_CustomTrafficLights.GetPattern(m_Ways) & 0xFFFF) == (uint)TrafficLightPatterns.Pattern.ModDefault)
            {
                EntityManager.RemoveComponent<CustomTrafficLights>(m_SelectedEntity);
            }

            EntityManager.AddComponentData(m_SelectedEntity, default(Updated));
        }
    }

    protected void ResetMainPanelState()
    {
        m_IsLaneManagementToolOpen = false;

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

            // Clean up old entity
            if (EntityManager.HasBuffer<ConnectPositionSource>(m_SelectedEntity))
            {
                EntityManager.RemoveComponent<ConnectPositionSource>(m_SelectedEntity);
            }

            if (EntityManager.HasBuffer<ConnectPositionTarget>(m_SelectedEntity))
            {
                EntityManager.RemoveComponent<ConnectPositionTarget>(m_SelectedEntity);
            }

            m_SelectedEntity = entity;

            m_Ways = 0;

            m_CustomTrafficLights = new CustomTrafficLights(TrafficLightPatterns.Pattern.ModDefault);

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

            // UpdateLaneDirectionTool();
        }
    }
}