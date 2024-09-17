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
        NativeHashMap<Entity, NodeUtils.LaneConnection> laneConnectionMap = NodeUtils.GetLaneConnectionMap(Allocator.Temp, subLanes, connectedEdges, job.m_ExtraTypeHandle.m_SubLane, job.m_ExtraTypeHandle.m_Lane);
        groupCount = customPhaseDatas.Length;
        for (int i = 0; i < subLanes.Length; i++)
        {
            Entity subLane = subLanes[i].m_SubLane;
            bool isPedestrian = job.m_PedestrianLaneData.TryGetComponent(subLane, out var pedestrianLane);
            if ((pedestrianLane.m_Flags & (PedestrianLaneFlags.Crosswalk | PedestrianLaneFlags.Unsafe)) == (PedestrianLaneFlags.Crosswalk | PedestrianLaneFlags.Unsafe))
            {
                continue;
            }
            LaneSignal laneSignal = job.m_LaneSignalData[subLane];
            ExtraLaneSignal extraLaneSignal = new();
            laneSignal.m_GroupMask = ushort.MaxValue;
            laneSignal.m_Default = 0;
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
                if ((pedestrianLane.m_Flags & PedestrianLaneFlags.Crosswalk) != 0)
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

            TrafficLightSystem.PatchedTrafficLightSystem.UpdateLaneSignal(trafficLights, ref laneSignal, ref extraLaneSignal);
            if (job.m_LaneSignalData.HasComponent(subLane))
            {
                job.m_LaneSignalData[subLane] = laneSignal;
            }
            else
            {
                job.m_CommandBuffer.AddComponent(unfilteredChunkIndex, subLane, laneSignal);
            }
            if (job.m_ExtraTypeHandle.m_ExtraLaneSignal.HasComponent(subLane))
            {
                job.m_CommandBuffer.SetComponent(unfilteredChunkIndex, subLane, extraLaneSignal);
            }
            else
            {
                job.m_CommandBuffer.AddComponent(unfilteredChunkIndex, subLane, extraLaneSignal);
            }
        }

        // Set up pedestrian crossings at tracks
        for (int i = 0; i < subLanes.Length; i++)
        {
            Entity subLane = subLanes[i].m_SubLane;
            bool isPedestrian = job.m_PedestrianLaneData.TryGetComponent(subLane, out var pedestrianLane);
            if (!isPedestrian)
            {
                continue;
            }
            if ((pedestrianLane.m_Flags & (PedestrianLaneFlags.Crosswalk | PedestrianLaneFlags.Unsafe)) == (PedestrianLaneFlags.Crosswalk | PedestrianLaneFlags.Unsafe))
            {
                continue;
            }
            LaneSignal laneSignal = job.m_LaneSignalData[subLane];
            ExtraLaneSignal extraLaneSignal = new();
            laneSignal.m_GroupMask = ushort.MaxValue;
            laneSignal.m_Default = 0;
            if (job.m_Overlaps.HasBuffer(subLane))
            {
                bool hasCarLane = false;
                foreach (var overlap in job.m_Overlaps[subLane])
                {
                    if (job.m_CarLaneData.HasComponent(overlap.m_Other))
                    {
                        hasCarLane = true;
                        break;
                    }
                    if (!job.m_ExtraTypeHandle.m_TrackLane.HasComponent(overlap.m_Other))
                    {
                        continue;
                    }
                    if (job.m_LaneSignalData.TryGetComponent(overlap.m_Other, out var overlapSignal))
                    {
                        laneSignal.m_GroupMask &= (ushort)~overlapSignal.m_GroupMask;
                    }
                }
                if (hasCarLane)
                {
                    continue;
                }
            }

            TrafficLightSystem.PatchedTrafficLightSystem.UpdateLaneSignal(trafficLights, ref laneSignal, ref extraLaneSignal);
            // Do not check if the component exists because it has already been added to the ecb in the previous loop
            job.m_CommandBuffer.SetComponent(unfilteredChunkIndex, subLane, laneSignal);
            job.m_CommandBuffer.SetComponent(unfilteredChunkIndex, subLane, extraLaneSignal);
        }
    }
}