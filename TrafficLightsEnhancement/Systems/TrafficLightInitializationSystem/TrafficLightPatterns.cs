using System;
using Unity.Collections;
using Unity.Mathematics;

namespace C2VM.TrafficLightsEnhancement.Systems.TrafficLightInitializationSystem;

class TrafficLightPatterns {
    public enum Pattern : uint
    {
        Vanilla = 0,
        SplitPhasing = 1,
        ProtectedCentreTurn = 2,
        SplitPhasingAdvanced = 3,
        ExclusivePedestrian = 1 << 16,
        AlwaysGreenKerbsideTurn = 2 << 16,
    }

    public static bool IsValidPattern(int ways, uint pattern)
    {
        switch (pattern & 0xFFFF)
        {
            case (uint) Pattern.Vanilla:
                return true;

            case (uint) Pattern.SplitPhasing:
                return true;

            case (uint) Pattern.SplitPhasingAdvanced:
                if (ways >= 3 && ways <= 4)
                {
                    return true;
                }
                return false;

            case (uint) Pattern.ProtectedCentreTurn:
                if (ways == 4)
                {
                    return true;
                }
                return false;

            default:
                return false;
        }
    }

    public static void ProcessVehicleLaneGroups(ref NativeList<LaneGroup> vehicleLanes, ref NativeList<LaneGroup> groups, ref bool isLevelCrossing, ref int groupCount, bool leftHandTraffic, int ways, uint pattern)
    {
        int[] groupLeft = new int[groups.Length];
        int[] groupRight = new int[groups.Length];
        int[] groupStraight = new int[groups.Length];

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

                if (group.m_IsStraight && math.dot(group.m_EndDirection, group2.m_EndDirection) > 0.999f && math.dot(group.m_EndDirection, group2.m_StartDirection) > 0.999f)
                {
                    groupStraight[group.m_GroupIndex] = group2.m_GroupIndex;
                }

                if (math.dot(group.m_EndDirection, group2.m_StartDirection) > 0.999f)
                {
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

        if ((pattern & 0xFFFF) == (int) Pattern.SplitPhasing)
        {
            for (int i = 0; i < groups.Length; i++)
            {
                LaneGroup group = groups[i];
                group.m_GroupMask = (ushort)(1 << (group.m_GroupIndex & 0xF));
                groups[i] = group;
            }
        }

        if ((pattern & 0xFFFF) == (int) Pattern.SplitPhasingAdvanced)
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

        if ((pattern & 0xFFFF) == (int) Pattern.ProtectedCentreTurn)
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
        
        if ((pattern & (int) Pattern.AlwaysGreenKerbsideTurn) != 0)
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

    // public static void ProcessPedestrianLaneGroups(DynamicBuffer<Game.Net.SubLane> subLanes, NativeList<TrafficLightInitializationSystem.LaneGroup> pedestrianLanes, NativeList<TrafficLightInitializationSystem.LaneGroup> groups, bool isLevelCrossing, ref int groupCount)
    // {

    // }
}


