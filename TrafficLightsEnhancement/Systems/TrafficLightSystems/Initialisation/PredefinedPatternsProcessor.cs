using C2VM.TrafficLightsEnhancement.Components;
using C2VM.TrafficLightsEnhancement.Utils;
using Game.Net;
using Unity.Collections;
using Unity.Entities;
using static C2VM.TrafficLightsEnhancement.Systems.TrafficLightSystems.Initialisation.PatchedTrafficLightInitializationSystem;
using static C2VM.TrafficLightsEnhancement.Utils.NodeUtils;

namespace C2VM.TrafficLightsEnhancement.Systems.TrafficLightSystems.Initialisation;

public class PredefinedPatternsProcessor
{
    public static bool IsValidPattern(NativeArray<EdgeInfo> edgeInfoArray, CustomTrafficLights.Patterns pattern)
    {
        if (HasTrainTrack(edgeInfoArray))
        {
            return false;
        }

        switch ((uint)pattern & 0xFFFF)
        {
            case (uint)CustomTrafficLights.Patterns.SplitPhasing:
            {
                if (edgeInfoArray.Length > 7)
                {
                    return false;
                }
                return true;
            }

            case (uint)CustomTrafficLights.Patterns.SplitPhasingAdvancedObsolete:
            {
                return false;
            }

            case (uint)CustomTrafficLights.Patterns.ProtectedCentreTurn:
            {
                int ways = 0;
                foreach (var edgeInfo in edgeInfoArray)
                {
                    if (edgeInfo.m_TrackLaneLeftCount + edgeInfo.m_TrackLaneRightCount > 0)
                    {
                        return false;
                    }
                    if (edgeInfo.m_CarLaneStraightCount + edgeInfo.m_PublicCarLaneStraightCount + edgeInfo.m_TrackLaneStraightCount > 0)
                    {
                        ways++;
                    }
                }
                if (ways == 4 && edgeInfoArray.Length == ways)
                {
                    return true;
                }
                return false;
            }

            default:
            {
                return true;
            }
        }
    }

    public static void SetupSplitPhasing(ref InitializeTrafficLightsJob job, DynamicBuffer<ConnectedEdge> connectedEdges, DynamicBuffer<SubLane> subLanes, out int groupCount, ref TrafficLights trafficLights)
    {
        NativeHashMap<Entity, NodeUtils.LaneConnection> laneConnectionMap = NodeUtils.GetLaneConnectionMap(Allocator.Temp, subLanes, connectedEdges, job.m_ExtraTypeHandle.m_SubLane, job.m_ExtraTypeHandle.m_Lane);
        groupCount = 0;

        for (int i = 0; i < subLanes.Length; i++)
        {
            Entity subLane = subLanes[i].m_SubLane;
            if (!job.m_LaneSignalData.TryGetComponent(subLane, out var laneSignal))
            {
                continue;
            }
            laneSignal.m_GroupMask = 0;
            job.m_LaneSignalData[subLane] = laneSignal;
        }

        // Set up car lanes
        for (int i = 0; i < connectedEdges.Length; i++)
        {
            Entity edge = connectedEdges[i].m_Edge;
            bool modified = false;
            for (int j = 0; j < subLanes.Length; j++)
            {
                Entity subLane = subLanes[j].m_SubLane;
                if (laneConnectionMap[subLane].m_SourceEdge != edge)
                {
                    continue;
                }
                if (!job.m_LaneSignalData.TryGetComponent(subLane, out var laneSignal))
                {
                    continue;
                }
                if (!job.m_CarLaneData.HasComponent(laneConnectionMap[subLane].m_SourceSubLane))
                {
                    continue;
                }
                laneSignal.m_GroupMask |= (ushort)(1 << groupCount);
                job.m_LaneSignalData[subLane] = laneSignal;
                modified = true;
            }
            if (modified)
            {
                groupCount++;
            }
        }

        // Set up track lanes
        for (int i = 0; i < connectedEdges.Length; i++)
        {
            Entity edge = connectedEdges[i].m_Edge;
            bool modified = false;
            for (int j = 0; j < subLanes.Length; j++)
            {
                Entity subLane = subLanes[j].m_SubLane;
                if (laneConnectionMap[subLane].m_SourceEdge != edge)
                {
                    continue;
                }
                if (!job.m_LaneSignalData.TryGetComponent(subLane, out var laneSignal) || !job.m_ExtraTypeHandle.m_TrackLane.HasComponent(subLane))
                {
                    continue;
                }
                if (job.m_CarLaneData.HasComponent(laneConnectionMap[subLane].m_SourceSubLane))
                {
                    continue;
                }
                laneSignal.m_GroupMask |= (ushort)(1 << groupCount);
                job.m_LaneSignalData[subLane] = laneSignal;
                modified = true;
            }
            if (modified)
            {
                groupCount++;
            }
        }

        SetupNonOverlapLanes(ref job, subLanes, groupCount, laneConnectionMap);
        SetupMasterLanes(ref job, subLanes);
        RemoveDuplicateGroups(ref job, subLanes, ref groupCount);
        SetupPedestrianLanes(ref job, subLanes, groupCount, laneConnectionMap);
        CheckPedestrianLanes(ref job, subLanes, ref groupCount);
        UpdateLaneSignal(ref job, subLanes, ref trafficLights);
    }

    public static void SetupProtectedCentreTurn(ref InitializeTrafficLightsJob job, DynamicBuffer<ConnectedEdge> connectedEdges, DynamicBuffer<SubLane> subLanes, out int groupCount, ref TrafficLights trafficLights)
    {
        NativeHashMap<Entity, NodeUtils.LaneConnection> laneConnectionMap = NodeUtils.GetLaneConnectionMap(Allocator.Temp, subLanes, connectedEdges, job.m_ExtraTypeHandle.m_SubLane, job.m_ExtraTypeHandle.m_Lane);
        groupCount = 0;

        NativeHashMap<Entity, Entity> straightEdgeMap = new(connectedEdges.Length, Allocator.Temp);
        NativeHashSet<Entity> fulfilledStraightEdgeList = new(connectedEdges.Length, Allocator.Temp);
        NativeHashSet<Entity> fulfilledTurnEdgeList = new(connectedEdges.Length, Allocator.Temp);

        for (int i = 0; i < subLanes.Length; i++)
        {
            Entity subLane = subLanes[i].m_SubLane;
            if (!job.m_LaneSignalData.TryGetComponent(subLane, out var laneSignal))
            {
                continue;
            }
            laneSignal.m_GroupMask = 0;
            job.m_LaneSignalData[subLane] = laneSignal;

            bool isCarLane = job.m_CarLaneData.TryGetComponent(subLane, out var carLane);
            bool isTrackLane = job.m_ExtraTypeHandle.m_TrackLane.TryGetComponent(subLane, out var trackLane);
            if (!isCarLane && !isTrackLane)
            {
                continue;
            }
            if (isCarLane && (carLane.m_Flags & (CarLaneFlags.TurnLeft | CarLaneFlags.TurnRight | CarLaneFlags.GentleTurnLeft | CarLaneFlags.GentleTurnRight | CarLaneFlags.UTurnLeft | CarLaneFlags.UTurnRight)) != 0)
            {
                continue;
            }
            else if (isTrackLane && (trackLane.m_Flags & (TrackLaneFlags.TurnLeft | TrackLaneFlags.TurnRight)) != 0)
            {
                continue;
            }
            var laneConnection = laneConnectionMap[subLane];
            if (straightEdgeMap.ContainsKey(laneConnection.m_SourceEdge) || laneConnection.m_DestEdge == Entity.Null)
            {
                continue;
            }
            straightEdgeMap[laneConnection.m_SourceEdge] = laneConnection.m_DestEdge;
        }

        NativeArray<Entity> edgeArray = new(2, Allocator.Temp);
        for (int i = 0; i < connectedEdges.Length; i++)
        {
            edgeArray[0] = connectedEdges[i].m_Edge;
            edgeArray[1] = straightEdgeMap[edgeArray[0]];
            bool modified = false;
            foreach (Entity edge in edgeArray)
            {
                if (edge == Entity.Null || fulfilledStraightEdgeList.Contains(edge))
                {
                    continue;
                }
                fulfilledStraightEdgeList.Add(edge);
                for (int j = 0; j < subLanes.Length; j++)
                {
                    Entity subLane = subLanes[j].m_SubLane;
                    bool isCarLane = job.m_CarLaneData.TryGetComponent(subLane, out var carLane);
                    bool isTrackLane = job.m_ExtraTypeHandle.m_TrackLane.TryGetComponent(subLane, out var trackLane);
                    if (laneConnectionMap[subLane].m_SourceEdge != edge)
                    {
                        continue;
                    }
                    if (!job.m_LaneSignalData.TryGetComponent(subLane, out var laneSignal))
                    {
                        continue;
                    }
                    if (!isCarLane && !isTrackLane)
                    {
                        continue;
                    }
                    if (job.m_LeftHandTraffic)
                    {
                        if (isCarLane && (carLane.m_Flags & (CarLaneFlags.GentleTurnRight | CarLaneFlags.TurnRight | CarLaneFlags.UTurnRight)) != 0)
                        {
                            continue;
                        }
                        else if (isTrackLane && (trackLane.m_Flags & TrackLaneFlags.TurnRight) != 0)
                        {
                            continue;
                        }
                    }
                    else
                    {
                        if (isCarLane && (carLane.m_Flags & (CarLaneFlags.GentleTurnLeft | CarLaneFlags.TurnLeft | CarLaneFlags.UTurnLeft)) != 0)
                        {
                            continue;
                        }
                        else if (isTrackLane && (trackLane.m_Flags & TrackLaneFlags.TurnLeft) != 0)
                        {
                            continue;
                        }
                    }
                    laneSignal.m_GroupMask |= (ushort)(1 << groupCount);
                    job.m_LaneSignalData[subLane] = laneSignal;
                    modified = true;
                }
            }
            if (modified)
            {
                groupCount++;
            }

            modified = false;
            foreach (Entity edge in edgeArray)
            {
                if (edge == Entity.Null || fulfilledTurnEdgeList.Contains(edge))
                {
                    continue;
                }
                fulfilledTurnEdgeList.Add(edge);
                for (int j = 0; j < subLanes.Length; j++)
                {
                    Entity subLane = subLanes[j].m_SubLane;
                    bool isCarLane = job.m_CarLaneData.TryGetComponent(subLane, out var carLane);
                    bool isTrackLane = job.m_ExtraTypeHandle.m_TrackLane.TryGetComponent(subLane, out var trackLane);
                    if (laneConnectionMap[subLane].m_SourceEdge != edge)
                    {
                        continue;
                    }
                    if (!job.m_LaneSignalData.TryGetComponent(subLane, out var laneSignal))
                    {
                        continue;
                    }
                    if (!isCarLane && !isTrackLane)
                    {
                        continue;
                    }
                    if (job.m_LeftHandTraffic)
                    {
                        if (isCarLane && (carLane.m_Flags & (CarLaneFlags.GentleTurnRight | CarLaneFlags.TurnRight | CarLaneFlags.UTurnRight)) == 0)
                        {
                            continue;
                        }
                        else if (isTrackLane && (trackLane.m_Flags & TrackLaneFlags.TurnRight) == 0)
                        {
                            continue;
                        }
                    }
                    else
                    {
                        if (isCarLane && (carLane.m_Flags & (CarLaneFlags.GentleTurnLeft | CarLaneFlags.TurnLeft | CarLaneFlags.UTurnLeft)) == 0)
                        {
                            continue;
                        }
                        else if (isTrackLane && (trackLane.m_Flags & TrackLaneFlags.TurnLeft) == 0)
                        {
                            continue;
                        }
                    }
                    laneSignal.m_GroupMask |= (ushort)(1 << groupCount);
                    job.m_LaneSignalData[subLane] = laneSignal;
                    modified = true;
                }
            }
            if (modified)
            {
                groupCount++;
            }
        }

        SetupNonOverlapLanes(ref job, subLanes, groupCount, laneConnectionMap);
        SetupMasterLanes(ref job, subLanes);
        SetupPedestrianLanes(ref job, subLanes, groupCount, laneConnectionMap);
        CheckPedestrianLanes(ref job, subLanes, ref groupCount);
        UpdateLaneSignal(ref job, subLanes, ref trafficLights);
    }

    private static void SetupNonOverlapLanes(ref InitializeTrafficLightsJob job, DynamicBuffer<SubLane> subLanes, int groupCount, NativeHashMap<Entity, NodeUtils.LaneConnection> laneConnectionMap)
    {
        for (int i = 0; i < subLanes.Length; i++)
        {
            Entity subLane = subLanes[i].m_SubLane;
            if (!job.m_LaneSignalData.TryGetComponent(subLane, out var laneSignal))
            {
                continue;
            }
            if (!job.m_CarLaneData.TryGetComponent(subLane, out var carLane) && !job.m_ExtraTypeHandle.m_TrackLane.HasComponent(subLane))
            {
                continue;
            }
            if ((carLane.m_Flags & (CarLaneFlags.UTurnLeft | CarLaneFlags.UTurnRight)) != 0)
            {
                continue;
            }
            if (!job.m_Overlaps.TryGetBuffer(subLane, out var laneOverlapBuffer) || laneOverlapBuffer.Length == 0)
            {
                // Set all groups to green since there is no overlap
                laneSignal.m_GroupMask |= (ushort)((1 << groupCount) - 1);
            }
            else
            {
                ushort overlapGroupMask = 0;
                foreach (var laneOverlap in laneOverlapBuffer)
                {
                    Entity overlapSubLane = laneOverlap.m_Other;
                    if (!job.m_LaneSignalData.TryGetComponent(overlapSubLane, out var overlapLaneSignal))
                    {
                        continue;
                    }
                    if (laneConnectionMap[subLane].m_SourceSubLane == laneConnectionMap[overlapSubLane].m_SourceSubLane)
                    {
                        continue;
                    }
                    if (!job.m_CarLaneData.TryGetComponent(overlapSubLane, out var overlapCarLane) && !job.m_ExtraTypeHandle.m_TrackLane.HasComponent(overlapSubLane))
                    {
                        continue;
                    }
                    if ((overlapCarLane.m_Flags & (CarLaneFlags.UTurnLeft | CarLaneFlags.UTurnRight)) != 0)
                    {
                        continue;
                    }
                    overlapGroupMask |= overlapLaneSignal.m_GroupMask;
                }
                laneSignal.m_GroupMask |= (ushort)((~overlapGroupMask) & ((1 << groupCount) - 1));
            }
            job.m_LaneSignalData[subLane] = laneSignal;
        }
    }

    private static void RemoveDuplicateGroups(ref InitializeTrafficLightsJob job, DynamicBuffer<SubLane> subLanes, ref int groupCount)
    {
        NativeList<NativeList<int>> greenLanes = new(groupCount, Allocator.Temp);
        for (int i = 0; i < groupCount; i++)
        {
            greenLanes[i] = new(subLanes.Length, Allocator.Temp);
            for (int j = 0; j < subLanes.Length; j++)
            {
                Entity subLane = subLanes[j].m_SubLane;
                if (!job.m_LaneSignalData.TryGetComponent(subLane, out var laneSignal))
                {
                    continue;
                }
                if ((laneSignal.m_GroupMask & (1 << i)) != 0)
                {
                    greenLanes[i].Add(j);
                }
            }
        }

        for (int i = 0; i < groupCount; i++)
        {
            for (int j = i + 1; j < groupCount; j++)
            {
                if (greenLanes[i].Length != greenLanes[j].Length)
                {
                    continue;
                }
                int k = 0;
                while (k < greenLanes[i].Length)
                {
                    if (greenLanes[i][k] != greenLanes[j][k])
                    {
                        break;
                    }
                    k++;
                }
                if (k >= greenLanes[i].Length)
                {
                    RemoveGroup(ref job, subLanes, j);
                    greenLanes.RemoveAt(j);
                    groupCount--;
                    j--;
                }
            }
        }
    }

    private static void RemoveGroup(ref InitializeTrafficLightsJob job, DynamicBuffer<SubLane> subLanes, int index)
    {
        for (int i = 0; i < subLanes.Length; i++)
        {
            Entity subLane = subLanes[i].m_SubLane;
            if (!job.m_LaneSignalData.TryGetComponent(subLane, out var laneSignal))
            {
                continue;
            }
            ushort mask = (ushort)((1 << index) - 1);
            laneSignal.m_GroupMask = (ushort)(((laneSignal.m_GroupMask >> 1) & ~mask) | (laneSignal.m_GroupMask & mask));
            job.m_LaneSignalData[subLane] = laneSignal;
        }
    }

    private static void SetupMasterLanes(ref InitializeTrafficLightsJob job, DynamicBuffer<SubLane> subLanes)
    {
        for (int i = 0; i < subLanes.Length; i++)
        {
            Entity subLane = subLanes[i].m_SubLane;
            if (!job.m_MasterLaneData.TryGetComponent(subLane, out MasterLane masterLane))
            {
                continue;
            }
            if (!job.m_LaneSignalData.TryGetComponent(subLane, out LaneSignal laneSignal))
            {
                continue;
            }

            laneSignal.m_GroupMask = 0;
            for (int j = masterLane.m_MinIndex; j <= masterLane.m_MaxIndex; j++)
            {
                Entity slaveSubLane = subLanes[j].m_SubLane;
                if (!job.m_LaneSignalData.TryGetComponent(slaveSubLane, out LaneSignal slaveLaneSignal))
                {
                    continue;
                }
                laneSignal.m_GroupMask |= slaveLaneSignal.m_GroupMask;
            }

            job.m_LaneSignalData[subLane] = laneSignal;
        }
    }

    private static void SetupPedestrianLanes(ref InitializeTrafficLightsJob job, DynamicBuffer<SubLane> subLanes, int groupCount, NativeHashMap<Entity, NodeUtils.LaneConnection> laneConnectionMap)
    {
        for (int i = 0; i < subLanes.Length; i++)
        {
            Entity subLane = subLanes[i].m_SubLane;
            if (!job.m_LaneSignalData.TryGetComponent(subLane, out var laneSignal) || !job.m_PedestrianLaneData.TryGetComponent(subLane, out var pedestrianLane))
            {
                continue;
            }
            if (!job.m_Overlaps.TryGetBuffer(subLane, out var laneOverlapBuffer) || laneOverlapBuffer.Length == 0)
            {
                // Set all groups to green since there is no overlap
                laneSignal.m_GroupMask |= (ushort)((1 << groupCount) - 1);
            }
            else
            {
                ushort overlapGroupMask = 0;
                var subLaneConnection = GetLaneConnectionFromNodeSubLane(subLane, laneConnectionMap, true);
                foreach (var laneOverlap in laneOverlapBuffer)
                {
                    Entity overlapSubLane = laneOverlap.m_Other;
                    bool isCarLane = !job.m_CarLaneData.TryGetComponent(overlapSubLane, out var overlapCarLane);
                    bool isTrackLane = !job.m_ExtraTypeHandle.m_TrackLane.TryGetComponent(overlapSubLane, out var overlapTrackLane);
                    if (!job.m_LaneSignalData.TryGetComponent(overlapSubLane, out var overlapLaneSignal))
                    {
                        continue;
                    }
                    if (!isCarLane && !isTrackLane)
                    {
                        continue;
                    }
                    if ((overlapCarLane.m_Flags & (CarLaneFlags.UTurnLeft | CarLaneFlags.UTurnRight)) != 0)
                    {
                        continue;
                    }
                    if (subLaneConnection.m_SourceEdge == laneConnectionMap[overlapSubLane].m_DestEdge || subLaneConnection.m_DestEdge == laneConnectionMap[overlapSubLane].m_DestEdge)
                    {
                        if (job.m_LeftHandTraffic)
                        {
                            if ((overlapCarLane.m_Flags & (CarLaneFlags.GentleTurnLeft | CarLaneFlags.TurnLeft)) != 0 || (overlapTrackLane.m_Flags & TrackLaneFlags.TurnLeft) != 0)
                            {
                                continue;
                            }
                        }
                        if (!job.m_LeftHandTraffic)
                        {
                            if ((overlapCarLane.m_Flags & (CarLaneFlags.GentleTurnRight | CarLaneFlags.TurnRight)) != 0 || (overlapTrackLane.m_Flags & TrackLaneFlags.TurnRight) != 0)
                            {
                                continue;
                            }
                        }
                    }
                    overlapGroupMask |= overlapLaneSignal.m_GroupMask;
                }
                laneSignal.m_GroupMask |= (ushort)((~overlapGroupMask) & ((1 << groupCount) - 1));
            }
            job.m_LaneSignalData[subLane] = laneSignal;
        }
    }

    public static void CheckPedestrianLanes(ref InitializeTrafficLightsJob job, DynamicBuffer<SubLane> subLanes, ref int groupCount)
    {
        bool needPedestrianPhase = false;
        for (int i = 0; i < subLanes.Length; i++)
        {
            Entity subLane = subLanes[i].m_SubLane;
            if (!job.m_LaneSignalData.TryGetComponent(subLane, out var laneSignal) || !job.m_PedestrianLaneData.HasComponent(subLane))
            {
                continue;
            }
            if (laneSignal.m_GroupMask == 0)
            {
                needPedestrianPhase = true;
                break;
            }
        }
        if (needPedestrianPhase)
        {
            for (int i = 0; i < subLanes.Length; i++)
            {
                Entity subLane = subLanes[i].m_SubLane;
                if (!job.m_LaneSignalData.TryGetComponent(subLane, out var laneSignal) || !job.m_PedestrianLaneData.HasComponent(subLane))
                {
                    continue;
                }
                laneSignal.m_GroupMask |= (ushort)(1 << groupCount);
                job.m_LaneSignalData[subLane] = laneSignal;
            }
            groupCount++;
        }
    }

    private static void UpdateLaneSignal(ref InitializeTrafficLightsJob job, DynamicBuffer<SubLane> subLanes, ref TrafficLights trafficLights)
    {
        for (int i = 0; i < subLanes.Length; i++)
        {
            Entity subLane = subLanes[i].m_SubLane;
            if (!job.m_LaneSignalData.TryGetComponent(subLane, out var laneSignal))
            {
                continue;
            }
            if (job.m_CarLaneData.HasComponent(subLane))
            {
                laneSignal.m_Flags |= LaneSignalFlags.CanExtend;
            }
            laneSignal.m_Default = 0;
            Simulation.PatchedTrafficLightSystem.UpdateLaneSignal(trafficLights, ref laneSignal);
            job.m_LaneSignalData[subLane] = laneSignal;
        }
    }

    public static void AddExclusivePedestrianPhase(ref InitializeTrafficLightsJob job, DynamicBuffer<SubLane> subLanes, ref int groupCount, ref TrafficLights trafficLights, ref CustomTrafficLights customTrafficLights)
    {
        ushort pedestrianGroupMask = ushort.MaxValue;
        for (int i = 0; i < subLanes.Length; i++)
        {
            Entity subLane = subLanes[i].m_SubLane;
            if (!job.m_LaneSignalData.TryGetComponent(subLane, out var laneSignal))
            {
                continue;
            }
            if (job.m_PedestrianLaneData.HasComponent(subLane))
            {
                pedestrianGroupMask &= laneSignal.m_GroupMask;
            }
            else
            {
                pedestrianGroupMask &= (ushort)~laneSignal.m_GroupMask;
            }
        }

        if (pedestrianGroupMask == 0)
        {
            pedestrianGroupMask = (ushort)(1 << groupCount);
            groupCount++;
        }

        customTrafficLights.SetPedestrianPhaseGroupMask(pedestrianGroupMask);

        for (int i = 0; i < subLanes.Length; i++)
        {
            Entity subLane = subLanes[i].m_SubLane;
            if (!job.m_LaneSignalData.TryGetComponent(subLane, out var laneSignal))
            {
                continue;
            }
            if (!job.m_PedestrianLaneData.HasComponent(subLane))
            {
                continue;
            }
            laneSignal.m_GroupMask = pedestrianGroupMask;
            Simulation.PatchedTrafficLightSystem.UpdateLaneSignal(trafficLights, ref laneSignal);
            job.m_LaneSignalData[subLane] = laneSignal;
        }
    }

    public static void AddAlwaysGreenKerbsideTurn(ref InitializeTrafficLightsJob job, int unfilteredChunkIndex, DynamicBuffer<SubLane> subLanes, ref int groupCount, ref TrafficLights trafficLights)
    {
        ushort pedestrianGroupMask = ushort.MaxValue;
        for (int i = 0; i < subLanes.Length; i++)
        {
            Entity subLane = subLanes[i].m_SubLane;
            if (!job.m_LaneSignalData.TryGetComponent(subLane, out var laneSignal))
            {
                continue;
            }
            if (job.m_PedestrianLaneData.HasComponent(subLane))
            {
                pedestrianGroupMask &= laneSignal.m_GroupMask;
            }
            else
            {
                pedestrianGroupMask &= (ushort)~laneSignal.m_GroupMask;
            }
        }

        for (int i = 0; i < subLanes.Length; i++)
        {
            Entity subLane = subLanes[i].m_SubLane;
            if (!job.m_LaneSignalData.TryGetComponent(subLane, out var laneSignal))
            {
                continue;
            }
            if (!job.m_CarLaneData.TryGetComponent(subLane, out var carLane))
            {
                continue;
            }
            if (job.m_LeftHandTraffic && (carLane.m_Flags & (CarLaneFlags.TurnLeft | CarLaneFlags.GentleTurnLeft)) == 0)
            {
                continue;
            }
            if (!job.m_LeftHandTraffic && (carLane.m_Flags & (CarLaneFlags.TurnRight | CarLaneFlags.GentleTurnRight)) == 0)
            {
                continue;
            }

            ushort groupMask = (ushort)(~pedestrianGroupMask & ~laneSignal.m_GroupMask & ((1 << groupCount) - 1));
            laneSignal.m_GroupMask |= groupMask;

            ExtraLaneSignal extraLaneSignal = new();
            extraLaneSignal.m_YieldGroupMask = groupMask;
            extraLaneSignal.m_IgnorePriorityGroupMask = groupMask;

            Simulation.PatchedTrafficLightSystem.UpdateLaneSignal(trafficLights, ref laneSignal, ref extraLaneSignal);
            job.m_LaneSignalData[subLane] = laneSignal;
            job.m_CommandBuffer.AddComponent(unfilteredChunkIndex, subLane, extraLaneSignal);
            job.m_CommandBuffer.SetComponent(unfilteredChunkIndex, subLane, extraLaneSignal);
        }
    }

    public static void AddCentreTurnGiveWay(ref InitializeTrafficLightsJob job, int unfilteredChunkIndex, DynamicBuffer<SubLane> subLanes, ref TrafficLights trafficLights)
    {
        for (int i = 0; i < subLanes.Length; i++)
        {
            Entity subLane = subLanes[i].m_SubLane;
            if (!job.m_LaneSignalData.TryGetComponent(subLane, out var laneSignal))
            {
                continue;
            }
            if (!job.m_CarLaneData.TryGetComponent(subLane, out var carLane))
            {
                continue;
            }
            if (job.m_LeftHandTraffic && (carLane.m_Flags & (CarLaneFlags.TurnRight | CarLaneFlags.GentleTurnRight)) == 0)
            {
                continue;
            }
            if (!job.m_LeftHandTraffic && (carLane.m_Flags & (CarLaneFlags.TurnLeft | CarLaneFlags.GentleTurnLeft)) == 0)
            {
                continue;
            }

            ExtraLaneSignal extraLaneSignal = new();
            extraLaneSignal.m_YieldGroupMask = laneSignal.m_GroupMask;
            extraLaneSignal.m_IgnorePriorityGroupMask = 0;

            Simulation.PatchedTrafficLightSystem.UpdateLaneSignal(trafficLights, ref laneSignal, ref extraLaneSignal);
            job.m_LaneSignalData[subLane] = laneSignal;
            job.m_CommandBuffer.AddComponent(unfilteredChunkIndex, subLane, extraLaneSignal);
            job.m_CommandBuffer.SetComponent(unfilteredChunkIndex, subLane, extraLaneSignal);
        }
    }

    public static void ResetExtraLaneSignal(ref InitializeTrafficLightsJob job, DynamicBuffer<SubLane> subLanes, ref TrafficLights trafficLights)
    {
        for (int i = 0; i < subLanes.Length; i++)
        {
            Entity subLane = subLanes[i].m_SubLane;
            if (!job.m_LaneSignalData.TryGetComponent(subLane, out var laneSignal))
            {
                continue;
            }
            if (!job.m_ExtraTypeHandle.m_ExtraLaneSignal.TryGetComponent(subLane, out var extraLaneSignal))
            {
                continue;
            }
            extraLaneSignal.m_YieldGroupMask = 0;
            extraLaneSignal.m_IgnorePriorityGroupMask = 0;
            Simulation.PatchedTrafficLightSystem.UpdateLaneSignal(trafficLights, ref laneSignal, ref extraLaneSignal);
            job.m_LaneSignalData[subLane] = laneSignal;
            job.m_ExtraTypeHandle.m_ExtraLaneSignal[subLane] = extraLaneSignal;
        }
    }
}