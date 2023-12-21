using System;
using System.Collections;
using System.Collections.Generic;
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

public class UISystem : GameSystemBase
{
    public bool m_IsLaneManagementToolOpen;

    public bool m_ShowNotificationUnsaved;

    public uint m_SelectedPattern;

    public Entity m_SelectedEntity;

    private View m_View;

    private int m_Ways;

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
        m_View.BindCall("C2VM-TLE-Call-MainPanel-Save", CallMainPanelSave);
        m_View.BindCall("C2VM-TLE-Call-LaneDirectionTool-Open", CallLaneDirectionToolOpen);
        m_View.BindCall("C2VM-TLE-Call-LaneDirectionTool-Close", CallLaneDirectionToolClose);
        m_View.BindCall("C2VM-TLE-Call-LaneDirectionTool-Reset", CallLaneDirectionToolReset);
        m_View.BindCall("C2VM-TLE-Call-LaneDirectionTool-Panel-Save", CallLaneDirectionToolPanelSave);

        m_View.BindCall("C2VM-TLE-Call-TranslatePosition", CallTranslatePosition);
        m_View.ExecuteScript(Payload.payload);
    }

    protected override void OnUpdate()
    {
    }

    protected static string GetLocale()
    {
        string locale = GameManager.instance.localizationManager.activeLocaleId;
        return locale;
    }

    protected string CallGetLocale()
    {
        var result = new {
            locale = GetLocale()
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
        m_SelectedPattern = (m_SelectedPattern & 0xFFFF0000) | uint.Parse(pattern.value);
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
                if (uint.Parse(option.key) == (uint) pattern)
                {
                    // Toggle the option
                    m_SelectedPattern ^= (uint) pattern;
                }
            }
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
            menu.items.Add(Types.MainPanelItemPattern("Vanilla", (int) TrafficLightPatterns.Pattern.Vanilla, m_SelectedPattern));
            if (TrafficLightPatterns.IsValidPattern(m_Ways, (int) TrafficLightPatterns.Pattern.SplitPhasing))
            {
                menu.items.Add(Types.MainPanelItemPattern("SplitPhasing", (int) TrafficLightPatterns.Pattern.SplitPhasing, m_SelectedPattern));
            }
            if (TrafficLightPatterns.IsValidPattern(m_Ways, (int) TrafficLightPatterns.Pattern.SplitPhasingAdvanced))
            {
                menu.items.Add(Types.MainPanelItemPattern("AdvancedSplitPhasing", (int) TrafficLightPatterns.Pattern.SplitPhasingAdvanced, m_SelectedPattern));
            }
            if (TrafficLightPatterns.IsValidPattern(m_Ways, (int) TrafficLightPatterns.Pattern.ProtectedCentreTurn))
            {
                if (m_CityConfigurationSystem.leftHandTraffic)
                {
                    menu.items.Add(Types.MainPanelItemPattern("ProtectedRightTurns", (int) TrafficLightPatterns.Pattern.ProtectedCentreTurn, m_SelectedPattern));
                }
                else
                {
                    menu.items.Add(Types.MainPanelItemPattern("ProtectedLeftTurns", (int) TrafficLightPatterns.Pattern.ProtectedCentreTurn, m_SelectedPattern));
                }
            }
            menu.items.Add(default(Types.ItemDivider));
            menu.items.Add(new Types.ItemTitle{title = "Options"});
            menu.items.Add(Types.MainPanelItemOption("ExclusivePedestrianPhase", (int) TrafficLightPatterns.Pattern.ExclusivePedestrian, m_SelectedPattern));
            if (m_CityConfigurationSystem.leftHandTraffic)
            {
                menu.items.Add(Types.MainPanelItemOption("AlwaysGreenLeftTurns", (int) TrafficLightPatterns.Pattern.AlwaysGreenKerbsideTurn, m_SelectedPattern));
            }
            else
            {
                menu.items.Add(Types.MainPanelItemOption("AlwaysGreenRightTurns", (int) TrafficLightPatterns.Pattern.AlwaysGreenKerbsideTurn, m_SelectedPattern));
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
                        if (connectPositionSourceBuffer[j].m_Owner != connectPositionSourceBuffer[i].m_Owner)
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
                EntityManager.AddComponentData(m_SelectedEntity, new CustomTrafficLights(m_SelectedPattern));
            }
            else
            {
                CustomTrafficLights customTrafficLights = m_CustomTrafficLightsLookup[m_SelectedEntity];
                customTrafficLights.SetPattern(m_SelectedPattern);
                m_CustomTrafficLightsLookup[m_SelectedEntity] = customTrafficLights;
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

        m_SelectedPattern = 0;

        if (m_CustomTrafficLightsLookup.HasComponent(m_SelectedEntity))
        {
            m_SelectedPattern = m_CustomTrafficLightsLookup[m_SelectedEntity].GetPattern(m_Ways);
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

            m_SelectedPattern = 0;

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