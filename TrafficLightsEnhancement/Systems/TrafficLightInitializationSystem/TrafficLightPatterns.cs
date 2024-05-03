using C2VM.TrafficLightsEnhancement.Components;
using Game.Net;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

namespace C2VM.TrafficLightsEnhancement.Systems.TrafficLightInitializationSystem;

public class TrafficLightPatterns {
    public enum Pattern : uint
    {
        Vanilla = 0,

        SplitPhasing = 1,

        ProtectedCentreTurn = 2,

        SplitPhasingAdvanced = 3,

        ModDefault = 4,

        ExclusivePedestrian = 1 << 16,

        AlwaysGreenKerbsideTurn = 1 << 17,

        CentreTurnGiveWay = 1 << 18,
    }

    public static bool IsValidPattern(int ways, Pattern pattern)
    {
        switch ((uint)pattern & 0xFFFF)
        {
            case (uint)Pattern.Vanilla:
                return true;

            case (uint)Pattern.SplitPhasing:
                return true;

            case (uint)Pattern.SplitPhasingAdvanced:
                if (ways >= 3 && ways <= 4)
                {
                    return true;
                }
                return false;

            case (uint)Pattern.ProtectedCentreTurn:
                if (ways == 4)
                {
                    return true;
                }
                return false;

            default:
                return false;
        }
    }

    public static void ProcessVehicleLaneGroups(ref NativeList<LaneGroup> vehicleLanes, ref NativeList<LaneGroup> groups, ref bool isLevelCrossing, ref int groupCount, bool leftHandTraffic, int ways, Pattern pattern)
    {
        NativeArray<int> groupLeft = new NativeArray<int>(groups.Length, Allocator.Temp);
        NativeArray<int> groupRight = new NativeArray<int>(groups.Length, Allocator.Temp);
        NativeArray<int> groupStraight = new NativeArray<int>(groups.Length, Allocator.Temp);

        for (int i = 0; i < groups.Length; i++)
        {
            groupLeft[i] = -1; // Which group is on the left hand side of group i
            groupRight[i] = -1; // Which group is on the right hand side of group i
            groupStraight[i] = -1;
        }

        for (int i = 0; i < groups.Length; i++)
        {
            LaneGroup group = groups[i];

            for (int j = 0; j < groups.Length; j++)
            {
                LaneGroup group2 = groups[j];

                if (math.dot(group.m_EndDirection, group2.m_StartDirection) > 0.999f && math.dot(group.m_StartDirection, group2.m_EndDirection) > 0.999f)
                {
                    if (group.m_IsStraight)
                    {
                        groupStraight[group.m_GroupIndex] = group2.m_GroupIndex;
                    }

                    if (group.m_IsTurnLeft)
                    {
                        groupLeft[group.m_GroupIndex] = group2.m_GroupIndex;
                    }
                    
                    if (group.m_IsTurnRight)
                    {
                        groupRight[group.m_GroupIndex] = group2.m_GroupIndex;
                    }
                }
            }
        }

        // for (int i = 0; i < groups.Length; i++)
        // {
        //     System.Console.WriteLine($"groupLeft {i} {groupLeft[i]}");
        //     System.Console.WriteLine($"groupStraight {i} {groupStraight[i]}");
        //     System.Console.WriteLine($"groupRight {i} {groupRight[i]}");
        // }

        if (((uint)pattern & 0xFFFF) == (uint)Pattern.SplitPhasing)
        {
            for (int i = 0; i < groups.Length; i++)
            {
                LaneGroup group = groups[i];
                group.m_GroupMask = (ushort)(1 << (group.m_GroupIndex & 0xF));
                groups[i] = group;
            }
        }

        if (((uint)pattern & 0xFFFF) == (uint)Pattern.SplitPhasingAdvanced)
        {
            if (ways == 3)
            {
                for (int i = 0; i < groups.Length; i++)
                {
                    LaneGroup group = groups[i];

                    group.m_GroupMask |= (ushort)(1 << (group.m_GroupIndex & 0xF));

                    if (leftHandTraffic && !group.m_IsTurnRight && groupStraight[group.m_GroupIndex] >= 0 && groupLeft[group.m_GroupIndex] < 0)
                    {
                        group.m_GroupMask |= (ushort)(1 << (groupStraight[group.m_GroupIndex] & 0xF));
                    }

                    if (!leftHandTraffic && !group.m_IsTurnLeft && groupStraight[group.m_GroupIndex] >= 0 && groupRight[group.m_GroupIndex] < 0)
                    {
                        group.m_GroupMask |= (ushort)(1 << (groupStraight[group.m_GroupIndex] & 0xF));
                    }

                    if (leftHandTraffic && group.m_IsTurnLeft && groupLeft[group.m_GroupIndex] >= 0)
                    {
                        group.m_GroupMask |= (ushort)(1 << (groupLeft[group.m_GroupIndex] & 0xF));
                    }
                    
                    if (!leftHandTraffic && group.m_IsTurnRight && groupRight[group.m_GroupIndex] >= 0)
                    {
                        group.m_GroupMask |= (ushort)(1 << (groupRight[group.m_GroupIndex] & 0xF));
                    }

                    groups[i] = group;
                }
            }
            if (ways == 4)
            {
                for (int i = 0; i < groups.Length; i++)
                {
                    LaneGroup group = groups[i];

                    group.m_GroupMask |= (ushort)(1 << (group.m_GroupIndex & 0xF));

                    if (leftHandTraffic && group.m_IsTurnLeft && groupLeft[group.m_GroupIndex] >= 0)
                    {
                        group.m_GroupMask |= (ushort)(1 << (groupLeft[group.m_GroupIndex] & 0xF));
                    }
                    
                    if (!leftHandTraffic && group.m_IsTurnRight && groupRight[group.m_GroupIndex] >= 0)
                    {
                        group.m_GroupMask |= (ushort)(1 << (groupRight[group.m_GroupIndex] & 0xF));
                    }

                    groups[i] = group;
                }
            }
        }

        if (((uint)pattern & 0xFFFF) == (uint)Pattern.ProtectedCentreTurn)
        {
            int signalGroupIndex = 0;
            for (int currentGroupIndex = 0; currentGroupIndex < groupCount; currentGroupIndex++) {
                bool modified = false;
                for (int i = 0; i < groups.Length; i++)
                {
                    LaneGroup group = groups[i];

                    if (group.m_GroupMask != 0 || group.m_GroupIndex != currentGroupIndex) {
                        continue;
                    }

                    if (
                        (leftHandTraffic && group.m_IsTurnRight) ||
                        (!leftHandTraffic && group.m_IsTurnLeft)
                    ) {
                        for (int j = 0; j < groups.Length; j++)
                        {
                            LaneGroup group2 = groups[j];
                            if (
                                (group2.m_GroupIndex == groupStraight[group.m_GroupIndex]) &&
                                ((leftHandTraffic && group2.m_IsTurnRight) || (!leftHandTraffic && group2.m_IsTurnLeft))
                            )
                            {
                                group2.m_GroupMask |= (ushort)(1 << (signalGroupIndex & 0xF));
                                groups[j] = group2;
                            }
                        }
                        group.m_GroupMask |= (ushort)(1 << (signalGroupIndex & 0xF));
                        groups[i] = group;
                        modified = true;
                    }
                }

                if (modified)
                {
                    signalGroupIndex++;
                    modified = false;
                }
                
                for (int i = 0; i < groups.Length; i++)
                {
                    LaneGroup group = groups[i];

                    if (group.m_GroupMask != 0 || group.m_GroupIndex != currentGroupIndex) {
                        continue;
                    }

                    if (
                        (leftHandTraffic && !group.m_IsTurnRight) ||
                        (!leftHandTraffic && !group.m_IsTurnLeft)
                    ) {
                        for (int j = 0; j < groups.Length; j++)
                        {
                            LaneGroup group2 = groups[j];
                            if (
                                (group2.m_GroupIndex == groupStraight[group.m_GroupIndex]) && 
                                ((leftHandTraffic && !group2.m_IsTurnRight) || (!leftHandTraffic && !group2.m_IsTurnLeft))
                            )
                            {
                                group2.m_GroupMask |= (ushort)(1 << (signalGroupIndex & 0xF));
                                groups[j] = group2;
                            }
                        }
                        group.m_GroupMask |= (ushort)(1 << (signalGroupIndex & 0xF));
                        groups[i] = group;
                        modified = true;
                    }
                }

                if (modified)
                {
                    signalGroupIndex++;
                }
            }
        }
        
        if (((uint)pattern & (uint)Pattern.AlwaysGreenKerbsideTurn) != 0)
        {
            ushort allGroupMask = 0;
            for (int i = 0; i < groups.Length; i++)
            {
                allGroupMask |= groups[i].m_GroupMask;
            }

            for (int i = 0; i < groups.Length; i++)
            {
                LaneGroup group = groups[i];
                if (
                    (leftHandTraffic && group.m_IsTurnLeft) ||
                    (!leftHandTraffic && group.m_IsTurnRight)
                )
                {
                    group.m_GroupMask |= allGroupMask;
                    group.m_IsYield = true;
                    group.m_IgnorePriority = true;
                }
                groups[i] = group;
            }
        }

        if (((uint)pattern & (uint)Pattern.CentreTurnGiveWay) != 0)
        {
            for (int i = 0; i < groups.Length; i++)
            {
                LaneGroup group = groups[i];
                if (
                    (leftHandTraffic && group.m_IsTurnRight) ||
                    (!leftHandTraffic && group.m_IsTurnLeft)
                )
                {
                    group.m_IsYield = true;
                }
                groups[i] = group;
            }
        }

        // System.Console.WriteLine("RESULT");
        // for (int l = 0; l < groups.Length; l++)
        // {
        //     TrafficLightInitializationSystem.LaneGroup group = groups[l];
                            
        //     System.Console.WriteLine($"groups[l] l {l} m_StartDirection {group.m_StartDirection} m_EndDirection {group.m_EndDirection} m_LaneRange {group.m_LaneRange} m_GroupIndex {group.m_GroupIndex} m_GroupMask {group.m_GroupMask} m_IsStraight {group.m_IsStraight} m_IsCombined {group.m_IsCombined} m_IsUnsafe {group.m_IsUnsafe} m_IsTrack {group.m_IsTrack} m_IsTurnLeft {group.m_IsTurnLeft} m_IsTurnRight {group.m_IsTurnRight}");
        // }
    }

    public static void ProcessPedestrianLaneGroups(DynamicBuffer<SubLane> subLanes, NativeList<LaneGroup> pedestrianLanes, NativeList<LaneGroup> groups, bool isLevelCrossing, ref int groupCount, bool leftHandTraffic, ref BufferLookup<LaneOverlap> overlaps, ref CustomTrafficLights customTrafficLights, int ways, Pattern pattern)
    {
        int newGroup = -1;
        for (int i = 0; i < pedestrianLanes.Length; i++)
        {
            LaneGroup pedLane = pedestrianLanes[i];
            pedLane.m_GroupMask = (ushort)((1 << math.min(16, groupCount)) - 1);
            Entity subLane = subLanes[pedLane.m_LaneRange.x].m_SubLane;
            if (!pedLane.m_IsUnsafe && overlaps.HasBuffer(subLane))
            {
                #if VERBOSITY_DEBUG
                System.Console.WriteLine($"ProcessPedestrianLaneGroups groupCount {groupCount} pedLane.m_GroupMask {pedLane.m_GroupMask} groups.Length {groups.Length}");
                #endif
                DynamicBuffer<LaneOverlap> dynamicBuffer = overlaps[subLane];
                for (int j = 0; j < groups.Length; j++)
                {
                    LaneGroup laneGroup = groups[j];
                    bool shouldRed;
                    if (isLevelCrossing)
                    {
                        shouldRed = laneGroup.m_IsTrack;
                    }
                    else
                    {
                        shouldRed = laneGroup.m_IsStraight;
                    }
                    if (((uint)pattern & 0xFFFF) != (uint)Pattern.Vanilla)
                    {
                        shouldRed |= (leftHandTraffic && laneGroup.m_IsTurnRight && !laneGroup.m_IsUTurn) || (!leftHandTraffic && laneGroup.m_IsTurnLeft && !laneGroup.m_IsUTurn);
                    }

                    bool isOverlap = false;
                    for (int k = laneGroup.m_LaneRange.x; k <= laneGroup.m_LaneRange.y; k++)
                    {
                        for (int m = 0; m < dynamicBuffer.Length; m++)
                        {
                            isOverlap |= dynamicBuffer[m].m_Other == subLanes[k].m_SubLane;
                        }
                    }

                    if (shouldRed && isOverlap)
                    {
                        pedLane.m_GroupMask &= (ushort)(~laneGroup.m_GroupMask);
                    }
                }
            }

            if ((pattern & Pattern.ExclusivePedestrian) != 0)
            {
                pedLane.m_GroupMask = 0;
            }

            if (pedLane.m_GroupMask == 0)
            {
                if (newGroup == -1)
                {
                    newGroup = groupCount++;
                }

                pedLane.m_GroupMask = (ushort)(1 << (newGroup & 0xF));
            }

            pedestrianLanes[i] = pedLane;
        }

        for (int i = 0; i < pedestrianLanes.Length; i++)
        {
            LaneGroup pedLane = pedestrianLanes[i];

            if (newGroup != -1)
            {
                pedLane.m_GroupMask |= (ushort)(1 << (newGroup & 0xF));
            }

            if ((pattern & Pattern.ExclusivePedestrian) != 0)
            {
                customTrafficLights.SetPedestrianPhaseGroupMask(customTrafficLights.m_PedestrianPhaseGroupMask | pedLane.m_GroupMask);
                #if VERBOSITY_DEBUG
                System.Console.WriteLine($"TrafficLightPatterns.Pattern.ExclusivePedestrian m_GroupIndex {pedLane.m_GroupIndex} m_GroupMask {pedLane.m_GroupMask} groups.Length {groups.Length}");
                #endif
            }

            groups.Add(in pedLane);
        }
    }
}


