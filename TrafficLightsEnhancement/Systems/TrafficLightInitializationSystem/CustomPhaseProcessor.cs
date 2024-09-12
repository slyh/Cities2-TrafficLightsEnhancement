using C2VM.TrafficLightsEnhancement.Components;
using C2VM.TrafficLightsEnhancement.Utils;
using Game.Net;
using Unity.Collections;
using Unity.Entities;
using static C2VM.TrafficLightsEnhancement.Systems.TrafficLightInitializationSystem.PatchedTrafficLightInitializationSystem;

namespace C2VM.TrafficLightsEnhancement.Systems.TrafficLightInitializationSystem;

public struct CustomPhaseProcessor
{
    public static void ProcessLanes(ref InitializeTrafficLightsJob job, int unfilteredChunkIndex, Entity nodeEntity, DynamicBuffer<ConnectedEdge> connectedEdges, DynamicBuffer<SubLane> subLanes, NativeList<LaneGroup> vehicleLanes, NativeList<LaneGroup> pedestrianLanes, NativeList<LaneGroup> groups, out int groupCount, ref TrafficLights trafficLights, ref CustomTrafficLights customTrafficLights, DynamicBuffer<CustomPhaseGroupMask> customPhaseGroupMasks, DynamicBuffer<CustomPhaseData> customPhaseDatas)
    {
        groupCount = customPhaseDatas.Length;
        var laneConnectionMap = NodeUtils.GetLaneConnectionMap(Allocator.Temp, subLanes, connectedEdges, job.m_ExtraTypeHandle.m_SubLane, job.m_ExtraTypeHandle.m_Lane);
        for (int i = 0; i < subLanes.Length; i++)
        {
            Entity subLane = subLanes[i].m_SubLane;
            if (!job.m_LaneSignalData.HasComponent(subLane))
            {
                continue;
            }
            LaneSignal laneSignal = job.m_LaneSignalData[subLane];
            ExtraLaneSignal extraLaneSignal = new ExtraLaneSignal();
            laneSignal.m_GroupMask = 0;
            laneSignal.m_Default = 0;
            bool isPedestrian = job.m_PedestrianLaneData.TryGetComponent(subLane, out var pedestrianLane);
            var laneConnection = NodeUtils.GetLaneConnectionFromNodeSubLane(subLane, laneConnectionMap, (pedestrianLane.m_Flags & PedestrianLaneFlags.Crosswalk) != 0);
            var sourceEdge = laneConnection.m_SourceEdge == Entity.Null && isPedestrian ? laneConnection.m_DestEdge : laneConnection.m_SourceEdge;
            var edgePosition = NodeUtils.GetEdgePosition(ref job, nodeEntity, sourceEdge);
            if (CustomPhaseUtils.TryGet(customPhaseGroupMasks, sourceEdge, edgePosition, 0, out CustomPhaseGroupMask groupMask) >= 0)
            {
                if (job.m_CarLaneData.TryGetComponent(subLane, out var nodeCarLane))
                {
                    job.m_CarLaneData.TryGetComponent(laneConnection.m_SourceSubLane, out var edgeCarLane);
                    var turn = (edgeCarLane.m_Flags & CarLaneFlags.PublicOnly) != 0 ? groupMask.m_PublicCar : groupMask.m_Car;
                    if ((nodeCarLane.m_Flags & (CarLaneFlags.TurnLeft | CarLaneFlags.GentleTurnLeft)) != 0)
                    {
                        laneSignal.m_GroupMask = turn.m_Left.m_GoGroupMask;
                        extraLaneSignal.m_YieldGroupMask = turn.m_Left.m_YieldGroupMask;
                        extraLaneSignal.m_IgnorePriorityGroupMask = turn.m_Left.m_YieldGroupMask;
                    }
                    else if ((nodeCarLane.m_Flags & (CarLaneFlags.TurnRight | CarLaneFlags.GentleTurnRight)) != 0)
                    {
                        laneSignal.m_GroupMask = turn.m_Right.m_GoGroupMask;
                        extraLaneSignal.m_YieldGroupMask = turn.m_Right.m_YieldGroupMask;
                        extraLaneSignal.m_IgnorePriorityGroupMask = turn.m_Right.m_YieldGroupMask;
                    }
                    else
                    {
                        laneSignal.m_GroupMask = turn.m_Straight.m_GoGroupMask;
                        extraLaneSignal.m_YieldGroupMask = turn.m_Straight.m_YieldGroupMask;
                        extraLaneSignal.m_IgnorePriorityGroupMask = turn.m_Straight.m_YieldGroupMask;
                    }
                    if ((nodeCarLane.m_Flags & (CarLaneFlags.UTurnLeft | CarLaneFlags.UTurnRight)) != 0)
                    {
                        laneSignal.m_GroupMask = turn.m_UTurn.m_GoGroupMask;
                        extraLaneSignal.m_YieldGroupMask = turn.m_UTurn.m_YieldGroupMask;
                        extraLaneSignal.m_IgnorePriorityGroupMask = turn.m_UTurn.m_YieldGroupMask;
                    }
                    laneSignal.m_Flags |= LaneSignalFlags.CanExtend;
                }
                if (job.m_ExtraTypeHandle.m_TrackLane.TryGetComponent(subLane, out var trackLane))
                {
                    if ((trackLane.m_Flags & TrackLaneFlags.TurnLeft) != 0)
                    {
                        laneSignal.m_GroupMask = groupMask.m_Track.m_Left.m_GoGroupMask;
                    }
                    else if ((trackLane.m_Flags & TrackLaneFlags.TurnRight) != 0)
                    {
                        laneSignal.m_GroupMask = groupMask.m_Track.m_Right.m_GoGroupMask;
                    }
                    else
                    {
                        laneSignal.m_GroupMask = groupMask.m_Track.m_Straight.m_GoGroupMask;
                    }
                }
                if (isPedestrian)
                {
                    if (NodeUtils.IsCrossingStopLine(ref job, subLane, sourceEdge))
                    {
                        laneSignal.m_GroupMask = groupMask.m_PedestrianStopLine.m_GoGroupMask;
                    }
                    else
                    {
                        laneSignal.m_GroupMask = groupMask.m_PedestrianNonStopLine.m_GoGroupMask;
                    }
                }
            }

            if (job.m_ExtraTypeHandle.m_ExtraLaneSignal.HasComponent(subLane))
            {
                job.m_CommandBuffer.SetComponent(unfilteredChunkIndex, subLane, extraLaneSignal);
            }
            else
            {
                job.m_CommandBuffer.AddComponent(unfilteredChunkIndex, subLane, extraLaneSignal);
            }

            TrafficLightSystem.PatchedTrafficLightSystem.UpdateLaneSignal(trafficLights, ref laneSignal, ref extraLaneSignal);
            job.m_LaneSignalData[subLane] = laneSignal;
        }

        if ((trafficLights.m_Flags & TrafficLightFlags.LevelCrossing) != 0)
        {
            for (int i = 0; i < subLanes.Length; i++)
            {
                Entity subLane = subLanes[i].m_SubLane;
                if (!job.m_LaneSignalData.HasComponent(subLane) || !job.m_PedestrianLaneData.HasComponent(subLane))
                {
                    continue;
                }
                LaneSignal laneSignal = job.m_LaneSignalData[subLane];
                ExtraLaneSignal extraLaneSignal = new ExtraLaneSignal();
                laneSignal.m_GroupMask = ushort.MaxValue;
                laneSignal.m_Default = 0;
                if (job.m_Overlaps.HasBuffer(subLane))
                {
                    foreach (var overlap in job.m_Overlaps[subLane])
                    {
                        if (job.m_LaneSignalData.TryGetComponent(overlap.m_Other, out var overlapSignal))
                        {
                            laneSignal.m_GroupMask &= (ushort)~overlapSignal.m_GroupMask;
                        }
                    }
                }

                if (job.m_ExtraTypeHandle.m_ExtraLaneSignal.HasComponent(subLane))
                {
                    job.m_CommandBuffer.SetComponent(unfilteredChunkIndex, subLane, extraLaneSignal);
                }
                else
                {
                    job.m_CommandBuffer.AddComponent(unfilteredChunkIndex, subLane, extraLaneSignal);
                }

                TrafficLightSystem.PatchedTrafficLightSystem.UpdateLaneSignal(trafficLights, ref laneSignal, ref extraLaneSignal);
                job.m_LaneSignalData[subLane] = laneSignal;
            }
        }
    }
}