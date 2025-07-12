using C2VM.TrafficLightsEnhancement.Components;
using C2VM.TrafficLightsEnhancement.Systems.Overlay;
using Colossal.Entities;
using Game.Net;
using Game.UI;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;

namespace C2VM.TrafficLightsEnhancement.Systems.UI;

public partial class UISystem : UISystemBase
{
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
                            color = Color.cyan;
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
                RenderSystem.Icon icon = RenderSystem.Icon.TrafficLight;
                if (customTrafficLights.GetPatternOnly() == CustomTrafficLights.Patterns.CustomPhase)
                {
                    icon = RenderSystem.Icon.TrafficLightWrench;
                }
                m_RenderSystem.AddIcon(node.m_Position, node.m_Rotation, icon);
            }
        }
        m_RenderSystem.BuildIconMesh();
    }
}