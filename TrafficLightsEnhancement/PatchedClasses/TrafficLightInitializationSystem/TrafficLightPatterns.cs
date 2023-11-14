using System;
using Unity.Collections;
using Unity.Mathematics;

namespace TrafficLightsEnhancement.PatchedClasses;

class TrafficLightPatterns {
    public enum Pattern : int
    {
        Vanilla = 0,
        SplitPhasing = 1,
        DoesThisExistInRealWorld = 2,
        ExclusivePedestrian = 1 << 16
    }

    public static bool IsValidPattern(int ways, int pattern)
    {
        foreach(int p in Enum.GetValues(typeof(Pattern)))
        {
            if ((p & 0xFFFF) == (pattern & 0xFFFF))
            {
                return true;
            }
        }
        return false;
    }

    public static void ProcessVehicleLaneGroups(ref NativeList<TrafficLightInitializationSystem.LaneGroup> vehicleLanes, ref NativeList<TrafficLightInitializationSystem.LaneGroup> groups, ref bool isLevelCrossing, ref int groupCount, bool leftHandTraffic, int pattern)
    {
        if (pattern == (int) Pattern.SplitPhasing)
        {
            for (int i = 0; i < groups.Length; i++)
            {
                TrafficLightInitializationSystem.LaneGroup group = groups[i];
                group.m_GroupMask = (ushort)(1 << (group.m_GroupIndex & 0xF));
                groups[i] = group;
            }
            return;
        }

        for (int i = 0; i < groups.Length; i++)
        {
            TrafficLightInitializationSystem.LaneGroup group = groups[i];
            group.m_GroupMask = ushort.MaxValue;
            groups[i] = group;
            groupCount = math.max(groupCount, group.m_GroupIndex);
        }

        int[] straightWith = new int[groups.Length];
        for (int i = 0; i < groups.Length; i++)
        {
            TrafficLightInitializationSystem.LaneGroup group = groups[i];

            for (int j = 0; j < groups.Length; j++)
            {
                TrafficLightInitializationSystem.LaneGroup group2 = groups[j];
                if (group.m_IsStraight && math.dot(group.m_EndDirection, group2.m_EndDirection) > 0.999f && math.dot(group.m_EndDirection, group2.m_StartDirection) > 0.999f)
                {
                    straightWith[group.m_GroupIndex] = group2.m_GroupIndex;
                }
            }
        }

        System.Console.WriteLine("straightWith");
        for (int i = 0; i < straightWith.Length; i++)
        {
            System.Console.WriteLine($"{i} {straightWith[i]}");
        }

        if (pattern == (int) Pattern.DoesThisExistInRealWorld)
        {
            for (int currentGroupIndex = 0; currentGroupIndex < groupCount; currentGroupIndex++) {
                bool modfied = false;
                for (int i = 0; i < groups.Length; i++)
                {
                    TrafficLightInitializationSystem.LaneGroup group = groups[i];

                    if (group.m_GroupMask != ushort.MaxValue || group.m_GroupIndex != currentGroupIndex) {
                        continue;
                    }

                    if (
                        (leftHandTraffic && group.m_IsTurnRight) ||
                        (!leftHandTraffic && group.m_IsTurnLeft)
                    ) {
                        for (int j = 0; j < groups.Length; j++)
                        {
                            TrafficLightInitializationSystem.LaneGroup group2 = groups[j];
                            if (
                                (group2.m_GroupIndex ==  straightWith[group.m_GroupIndex]) &&
                                ((leftHandTraffic && group2.m_IsTurnRight) || (!leftHandTraffic && group2.m_IsTurnLeft))
                            )
                            {
                                group.m_IsCombined = true;
                                group2.m_IsCombined = true;
                                group2.m_GroupMask = (ushort)(1 << (groupCount & 0xF));
                                groups[j] = group2;
                            }
                        }
                        group.m_GroupMask = (ushort)(1 << (groupCount & 0xF));
                        groups[i] = group;
                        modfied = true;
                    }
                }

                if (modfied)
                {
                    groupCount++;
                    modfied = false;
                }
                
                for (int i = 0; i < groups.Length; i++)
                {
                    TrafficLightInitializationSystem.LaneGroup group = groups[i];

                    if (group.m_GroupMask != ushort.MaxValue || group.m_GroupIndex != currentGroupIndex) {
                        continue;
                    }

                    if (
                        (leftHandTraffic && !group.m_IsTurnRight) ||
                        (!leftHandTraffic && !group.m_IsTurnLeft)
                    ) {
                        for (int j = 0; j < groups.Length; j++)
                        {
                            TrafficLightInitializationSystem.LaneGroup group2 = groups[j];
                            if (
                                (group2.m_GroupIndex == straightWith[group.m_GroupIndex]) && 
                                ((leftHandTraffic && !group2.m_IsTurnRight) || (!leftHandTraffic && !group2.m_IsTurnLeft))
                            )
                            {
                                group.m_IsCombined = true;
                                group2.m_IsCombined = true;
                                group2.m_GroupMask = (ushort)(1 << (groupCount & 0xF));
                                groups[j] = group2;
                            }
                        }
                        group.m_GroupMask = (ushort)(1 << (groupCount & 0xF));
                        groups[i] = group;
                        modfied = true;
                    }
                }

                if (modfied)
                {
                    groupCount++;
                }
            }
        }

        System.Console.WriteLine("RESULT");
        for (int l = 0; l < groups.Length; l++)
        {
            TrafficLightInitializationSystem.LaneGroup group = groups[l];
                            
            System.Console.WriteLine($"groups[l] l {l} m_StartDirection {group.m_StartDirection} m_EndDirection {group.m_EndDirection} m_LaneRange {group.m_LaneRange} m_GroupIndex {group.m_GroupIndex} m_GroupMask {group.m_GroupMask} m_IsStraight {group.m_IsStraight} m_IsCombined {group.m_IsCombined} m_IsUnsafe {group.m_IsUnsafe} m_IsTrack {group.m_IsTrack} m_IsTurnLeft {group.m_IsTurnLeft} m_IsTurnRight {group.m_IsTurnRight}");
        }
    }

    // public static void ProcessPedestrianLaneGroups(DynamicBuffer<Game.Net.SubLane> subLanes, NativeList<TrafficLightInitializationSystem.LaneGroup> pedestrianLanes, NativeList<TrafficLightInitializationSystem.LaneGroup> groups, bool isLevelCrossing, ref int groupCount)
    // {

    // }
}


