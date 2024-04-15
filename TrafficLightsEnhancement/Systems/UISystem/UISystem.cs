using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using C2VM.CommonLibraries.LaneSystem;
using C2VM.TrafficLightsEnhancement.Components;
using C2VM.TrafficLightsEnhancement.Systems.TrafficLightInitializationSystem;
using cohtml.Net;
using Game;
using Game.Common;
using Game.Net;
using Game.SceneFlow;
using Newtonsoft.Json;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace C2VM.TrafficLightsEnhancement.Systems.UISystem;

public partial class UISystem : GameSystemBase
{
    public bool m_IsLaneManagementToolOpen;

    public bool m_ShowNotificationUnsaved;

    public Entity m_SelectedEntity;

    private View m_View;

    private int m_Ways;

    private CustomTrafficLights m_CustomTrafficLights;

    private Game.City.CityConfigurationSystem m_CityConfigurationSystem;

    private BufferLookup<ConnectPositionSource> m_ConnectPositionSourceLookup;

    private BufferLookup<ConnectPositionTarget> m_ConnectPositionTargetLookup;

    private BufferLookup<CustomLaneDirection> m_CustomLaneDirectionLookup;

    private BufferLookup<ConnectedEdge> m_ConnectedEdgeLookup;

    private ComponentLookup<CarLane> m_CarLaneLookup;

    private ComponentLookup<Curve> m_CurveLookup;

    private ComponentLookup<CustomTrafficLights> m_CustomTrafficLightsLookup;

    private ComponentLookup<Edge> m_EdgeLookup;

    private ComponentLookup<LaneSignal> m_LaneSignalLookup;

    private ComponentLookup<MasterLane> m_MasterLaneLookup;

    private ComponentLookup<PedestrianLane> m_PedestrianLaneLookup;

    private ComponentLookup<SlaveLane> m_SlaveLaneLookup;

    private ComponentLookup<SecondaryLane> m_SecondaryLaneLookup;

    private BufferLookup<SubLane> m_SubLaneLookup;

    private ComponentLookup<TrackLane> m_TrackLaneLookup;

    protected override void OnCreate()
    {
        base.OnCreate();

        m_CityConfigurationSystem = World.GetOrCreateSystemManaged<Game.City.CityConfigurationSystem>();
        m_ConnectPositionSourceLookup = GetBufferLookup<ConnectPositionSource>(false);
        m_ConnectPositionTargetLookup = GetBufferLookup<ConnectPositionTarget>(false);
        m_CustomLaneDirectionLookup = GetBufferLookup<CustomLaneDirection>(false);
        m_CustomTrafficLightsLookup = GetComponentLookup<CustomTrafficLights>(false);
        m_ConnectedEdgeLookup = GetBufferLookup<ConnectedEdge>(false);
        m_CarLaneLookup = GetComponentLookup<CarLane>(true);
        m_CurveLookup = GetComponentLookup<Curve>(true);
        m_EdgeLookup = GetComponentLookup<Edge>(false);
        m_LaneSignalLookup = GetComponentLookup<LaneSignal>(true);
        m_MasterLaneLookup = GetComponentLookup<MasterLane>(true);
        m_PedestrianLaneLookup = GetComponentLookup<PedestrianLane>(true);
        m_SlaveLaneLookup = GetComponentLookup<SlaveLane>(true);
        m_SecondaryLaneLookup = GetComponentLookup<SecondaryLane>(true);
        m_SubLaneLookup = GetBufferLookup<SubLane>(false);
        m_TrackLaneLookup = GetComponentLookup<TrackLane>(true);

        m_View = GameManager.instance.userInterface.view.View;

        m_View.BindCall("C2VM-TLE-Call-GetLocale", CallGetLocale);
        m_View.BindCall("C2VM-TLE-Call-MainPanel-Update", CallMainPanelUpdate);
        m_View.BindCall("C2VM-TLE-Call-MainPanel-UpdatePattern", CallMainPanelUpdatePattern);
        m_View.BindCall("C2VM-TLE-Call-MainPanel-UpdateOption", CallMainPanelUpdateOption);
        m_View.BindCall("C2VM-TLE-Call-MainPanel-UpdateValue", CallMainPanelUpdateValue);
        m_View.BindCall("C2VM-TLE-Call-MainPanel-Save", CallMainPanelSave);
        m_View.BindCall("C2VM-TLE-Call-LaneDirectionTool-Open", CallLaneDirectionToolOpen);
        m_View.BindCall("C2VM-TLE-Call-LaneDirectionTool-Close", CallLaneDirectionToolClose);
        m_View.BindCall("C2VM-TLE-Call-LaneDirectionTool-Reset", CallLaneDirectionToolReset);
        m_View.BindCall("C2VM-TLE-Call-LaneDirectionTool-Panel-Save", CallLaneDirectionToolPanelSave);

        m_View.BindCall("C2VM-TLE-Call-KeyPress", CallKeyPress);
        m_View.BindCall("C2VM-TLE-Call-TranslatePosition", CallTranslatePosition);
        m_View.ExecuteScript(Payload.payload);
    }

    protected override void OnUpdate()
    {
    }

    protected string CallGetLocale()
    {
        var result = new
        {
            culture = CultureInfo.CurrentCulture.Name,
            locale = GameManager.instance.localizationManager.activeLocaleId,
        };
        return JsonConvert.SerializeObject(result);
    }

    protected void CallMainPanelUpdate()
    {
        UpdateMainPanel();
    }

    protected void CallMainPanelUpdatePattern(string input)
    {
        Types.ItemRadio pattern = JsonConvert.DeserializeObject<Types.ItemRadio>(input);
        m_CustomTrafficLights.SetPattern(((uint)m_CustomTrafficLights.GetPattern(m_Ways) & 0xFFFF0000) | uint.Parse(pattern.value));
        if (((uint)m_CustomTrafficLights.GetPattern(m_Ways) & 0xFFFF) != (uint)TrafficLightPatterns.Pattern.Vanilla)
        {
            m_CustomTrafficLights.SetPattern(m_CustomTrafficLights.GetPattern(m_Ways) & ~TrafficLightPatterns.Pattern.CentreTurnGiveWay);
        }
        UpdateEntity();
        UpdateMainPanel();
    }

    protected void CallMainPanelUpdateOption(string input)
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
        UpdateMainPanel();
    }

    protected void CallMainPanelUpdateValue(string jsonString)
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
        UpdateMainPanel();
    }

    protected void CallMainPanelSave(string value)
    {
        ChangeSelectedEntity(Entity.Null);
        UpdateMainPanel();
        CallLaneDirectionToolClose("");
    }

    protected void UpdateMainPanel()
    {
        var menu = new {
            title = "Traffic Lights Enhancement",
            image = "coui://GameUI/Media/Game/Icons/TrafficLights.svg",
            items = new ArrayList()
        };
        if (m_SelectedEntity != Entity.Null)
        {
            menu.items.Add(new Types.ItemTitle{title = "TrafficSignal"});
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
                    engineEventName = "C2VM-TLE-Call-MainPanel-UpdateValue"
                });
            }
            menu.items.Add(default(Types.ItemDivider));
            menu.items.Add(new Types.ItemTitle{title = "LaneDirectionTool"});
            if (m_IsLaneManagementToolOpen)
            {
                menu.items.Add(new Types.ItemButton{label = "Close", key = "status", value = "1", engineEventName = "C2VM-TLE-Call-LaneDirectionTool-Close"});
            }
            else
            {
                menu.items.Add(new Types.ItemButton{label = "Open", key = "status", value = "0", engineEventName = "C2VM-TLE-Call-LaneDirectionTool-Open"});
            }
            menu.items.Add(new Types.ItemButton{label = "Reset", key = "status", value = "0", engineEventName = "C2VM-TLE-Call-LaneDirectionTool-Reset"});
            menu.items.Add(default(Types.ItemDivider));
            menu.items.Add(new Types.ItemButton{label = "Save", key = "save", value = "1", engineEventName = "C2VM-TLE-Call-MainPanel-Save"});
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
        m_View.TriggerEvent("C2VM-TLE-Event-UpdateMainPanel", result);
    }

    protected void CallLaneDirectionToolOpen(string input)
    {
        m_IsLaneManagementToolOpen = true;
        UpdateLaneDirectionTool();
        UpdateMainPanel();
        UpdateEntity();
    }

    protected void CallLaneDirectionToolClose(string input)
    {
        m_IsLaneManagementToolOpen = false;
        UpdateLaneDirectionTool();
        UpdateMainPanel();
        UpdateEntity();
    }

    protected void CallLaneDirectionToolReset(string input)
    {
        if (m_SelectedEntity != Entity.Null)
        {
            EntityManager.RemoveComponent<CustomLaneDirection>(m_SelectedEntity);
            CallLaneDirectionToolClose("");
        }
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
            if (m_ConnectPositionSourceLookup.HasBuffer(m_SelectedEntity))
            {
                DynamicBuffer<ConnectPositionSource> connectPositionSourceBuffer = m_ConnectPositionSourceLookup[m_SelectedEntity];
                Dictionary<float3, bool> sourceExist = new Dictionary<float3, bool>();
                for (int i = 0; i < connectPositionSourceBuffer.Length; i++)
                {
                    sourceExist.Add(connectPositionSourceBuffer[i].m_Position, true);
                }

                DynamicBuffer<CustomLaneDirection> customLaneDirectionBuffer;
                // Remove CustomLaneDirection that is no longer exists
                if (m_CustomLaneDirectionLookup.HasBuffer(m_SelectedEntity))
                {
                    customLaneDirectionBuffer = m_CustomLaneDirectionLookup[m_SelectedEntity];
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
                                engineEventName = "C2VM-TLE-Call-LaneDirectionTool-Panel-Save"
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

        m_View.TriggerEvent("C2VM-TLE-Event-UpdateLaneDirectionTool", JsonConvert.SerializeObject(result));
    }

    protected void CallLaneDirectionToolPanelSave(string input)
    {
        Types.LaneDirection[] panel = JsonConvert.DeserializeObject<Types.LaneDirection[]>(input);

        if (m_SelectedEntity == Entity.Null)
        {
            return;
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

            if (m_ConnectPositionSourceLookup.HasBuffer(m_SelectedEntity))
            {
                DynamicBuffer<ConnectPositionSource> sourcePosBuffer = m_ConnectPositionSourceLookup[m_SelectedEntity];
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

            if (m_CustomLaneDirectionLookup.HasBuffer(m_SelectedEntity))
            {
                DynamicBuffer<CustomLaneDirection> buffer = m_CustomLaneDirectionLookup[m_SelectedEntity];
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
                    m_CustomLaneDirectionLookup[m_SelectedEntity].Add(direction);
                }
            }
            else
            {
                DynamicBuffer<CustomLaneDirection> buffer = EntityManager.AddBuffer<CustomLaneDirection>(m_SelectedEntity);
                buffer.Add(direction);
            }
        }

        UpdateEntity();
    }

    protected void CallKeyPress(string value)
    {
        var definition = new { ctrlKey = false, key = "" };
        var keyPressEvent = JsonConvert.DeserializeAnonymousType(value, definition);
        if (keyPressEvent.ctrlKey && keyPressEvent.key == "S")
        {
            if (m_IsLaneManagementToolOpen)
            {
                CallLaneDirectionToolClose("");
            }
            else if (!m_SelectedEntity.Equals(Entity.Null))
            {
                CallMainPanelSave("");
            }
        }
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
            if (!m_CustomTrafficLightsLookup.HasComponent(m_SelectedEntity))
            {
                EntityManager.AddComponentData(m_SelectedEntity, m_CustomTrafficLights);
            }
            else
            {
                m_CustomTrafficLightsLookup[m_SelectedEntity] = m_CustomTrafficLights;
            }

            if (m_SubLaneLookup.HasBuffer(m_SelectedEntity))
            {
                DynamicBuffer<SubLane> buffer = m_SubLaneLookup[m_SelectedEntity];
                foreach (SubLane subLane in buffer)
                {
                    EntityManager.AddComponentData(subLane.m_SubLane, default(Updated));
                }
            }

            if (m_ConnectedEdgeLookup.HasBuffer(m_SelectedEntity))
            {
                DynamicBuffer<ConnectedEdge> buffer = m_ConnectedEdgeLookup[m_SelectedEntity];
                foreach (ConnectedEdge connectedEdge in buffer)
                {
                    EntityManager.AddComponentData(connectedEdge.m_Edge, default(Updated));
                    if (m_EdgeLookup.HasComponent(connectedEdge.m_Edge))
                    {
                        Edge edge = m_EdgeLookup[connectedEdge.m_Edge];
                        EntityManager.AddComponentData(edge.m_Start, default(Updated));
                        EntityManager.AddComponentData(edge.m_End, default(Updated));
                    }
                }
            }

            EntityManager.AddComponentData(m_SelectedEntity, default(Updated));
        }
    }

    protected void ResetMainPanelState()
    {
        m_IsLaneManagementToolOpen = false;

        m_CustomTrafficLights = new CustomTrafficLights();

        if (m_CustomTrafficLightsLookup.HasComponent(m_SelectedEntity))
        {
            m_CustomTrafficLights = m_CustomTrafficLightsLookup[m_SelectedEntity];
        }
    }

    public void ChangeSelectedEntity(Entity entity)
    {
        if (entity != m_SelectedEntity && entity != Entity.Null && m_SelectedEntity != Entity.Null)
        {
            m_ShowNotificationUnsaved = true;
            UpdateMainPanel();
            return;
        }

        if (entity != m_SelectedEntity)
        {
            m_ShowNotificationUnsaved = false;

            // Clean up old entity
            if (m_ConnectPositionSourceLookup.HasBuffer(m_SelectedEntity))
            {
                EntityManager.RemoveComponent<ConnectPositionSource>(m_SelectedEntity);
            }

            if (m_ConnectPositionTargetLookup.HasBuffer(m_SelectedEntity))
            {
                EntityManager.RemoveComponent<ConnectPositionTarget>(m_SelectedEntity);
            }

            m_SelectedEntity = entity;

            m_Ways = 0;

            m_CustomTrafficLights = new CustomTrafficLights();

            // Retrieve info of new entity
            if (m_SubLaneLookup.HasBuffer(m_SelectedEntity))
            {
                Dictionary<float3, bool> lanes = new Dictionary<float3, bool>();
                DynamicBuffer<SubLane> buffer = m_SubLaneLookup[m_SelectedEntity];
                foreach (SubLane subLane in buffer)
                {
                    if (m_SecondaryLaneLookup.HasComponent(subLane.m_SubLane))
                    {
                        continue;
                    }

                    if (m_PedestrianLaneLookup.HasComponent(subLane.m_SubLane))
                    {
                        continue;
                    }

                    if (!m_CarLaneLookup.HasComponent(subLane.m_SubLane) && !m_TrackLaneLookup.HasComponent(subLane.m_SubLane))
                    {
                        continue;
                    }

                    if (m_MasterLaneLookup.HasComponent(subLane.m_SubLane) || !m_SlaveLaneLookup.HasComponent(subLane.m_SubLane))
                    {
                        if (m_CurveLookup.HasComponent(subLane.m_SubLane))
                        {
                            Curve curve = m_CurveLookup[subLane.m_SubLane];
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

            UpdateMainPanel();

            UpdateLaneDirectionTool();
        }
    }
}