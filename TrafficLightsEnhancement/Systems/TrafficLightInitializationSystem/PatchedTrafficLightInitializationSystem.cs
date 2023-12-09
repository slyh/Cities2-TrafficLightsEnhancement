#region Assembly Game, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null
// location unknown
// Decompiled with ICSharpCode.Decompiler 8.1.1.7464
#endregion

using System.Runtime.CompilerServices;
using C2VM.TrafficLightsEnhancement.Components;
using Colossal.Mathematics;
using Game;
using Game.Common;
using Game.Net;
using Game.Simulation;
using Unity.Burst;
using Unity.Burst.Intrinsics;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine.Scripting;

namespace C2VM.TrafficLightsEnhancement.Systems.TrafficLightInitializationSystem;

[CompilerGenerated]
public class PatchedTrafficLightInitializationSystem : GameSystemBase
{
    [BurstCompile]
    private struct InitializeTrafficLightsJob : IJobChunk
    {
        [ReadOnly]
        public BufferTypeHandle<SubLane> m_SubLaneType;

        public ComponentTypeHandle<TrafficLights> m_TrafficLightsType;

        [ReadOnly]
        public ComponentLookup<MasterLane> m_MasterLaneData;

        [ReadOnly]
        public ComponentLookup<SlaveLane> m_SlaveLaneData;

        [ReadOnly]
        public ComponentLookup<CarLane> m_CarLaneData;

        [ReadOnly]
        public ComponentLookup<PedestrianLane> m_PedestrianLaneData;

        [ReadOnly]
        public ComponentLookup<SecondaryLane> m_SecondaryLaneData;

        [ReadOnly]
        public ComponentLookup<Curve> m_CurveData;

        [ReadOnly]
        public BufferLookup<LaneOverlap> m_Overlaps;

        [NativeDisableParallelForRestriction]
        public ComponentLookup<LaneSignal> m_LaneSignalData;

        public ComponentTypeHandle<CustomTrafficLights> m_CustomTrafficLightsType;

        public bool m_LeftHandTraffic;

        public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
        {
            NativeList<LaneGroup> groups = new NativeList<LaneGroup>(16, Allocator.Temp);
            NativeList<LaneGroup> vehicleLanes = new NativeList<LaneGroup>(16, Allocator.Temp);
            NativeList<LaneGroup> pedestrianLanes = new NativeList<LaneGroup>(16, Allocator.Temp);
            NativeArray<TrafficLights> nativeArray = chunk.GetNativeArray(ref m_TrafficLightsType);
            BufferAccessor<SubLane> bufferAccessor = chunk.GetBufferAccessor(ref m_SubLaneType);
            NativeArray<CustomTrafficLights> customTrafficLightsArray = chunk.GetNativeArray(ref m_CustomTrafficLightsType);
            for (int i = 0; i < nativeArray.Length; i++)
            {
                TrafficLights trafficLights = nativeArray[i];
                DynamicBuffer<SubLane> subLanes = bufferAccessor[i];
                CustomTrafficLights customTrafficLights;
                if (i < customTrafficLightsArray.Length)
                {
                    customTrafficLights = customTrafficLightsArray[i];
                }
                else
                {
                    customTrafficLights = new CustomTrafficLights((int) TrafficLightPatterns.Pattern.Vanilla);
                }
                bool isLevelCrossing = (trafficLights.m_Flags & TrafficLightFlags.LevelCrossing) != 0;
                FillLaneBuffers(subLanes, vehicleLanes, pedestrianLanes);
                ProcessVehicleLaneGroups(vehicleLanes, groups, isLevelCrossing, out var groupCount, ref customTrafficLights, out int ways, out uint pattern);
                ProcessPedestrianLaneGroups(subLanes, pedestrianLanes, groups, isLevelCrossing, ref groupCount, ways, pattern);
                InitializeTrafficLights(subLanes, groups, groupCount, isLevelCrossing, ref trafficLights);
                nativeArray[i] = trafficLights;
                groups.Clear();
                vehicleLanes.Clear();
                pedestrianLanes.Clear();
            }

            groups.Dispose();
            vehicleLanes.Dispose();
            pedestrianLanes.Dispose();
        }

        private void FillLaneBuffers(DynamicBuffer<SubLane> subLanes, NativeList<LaneGroup> vehicleLanes, NativeList<LaneGroup> pedestrianLanes)
        {
            for (int i = 0; i < subLanes.Length; i++)
            {
                Entity subLane = subLanes[i].m_SubLane;
                if (!m_LaneSignalData.HasComponent(subLane) || m_SecondaryLaneData.HasComponent(subLane))
                {
                    continue;
                }

                if (m_PedestrianLaneData.HasComponent(subLane))
                {
                    PedestrianLane pedestrianLane = m_PedestrianLaneData[subLane];
                    Curve curve = m_CurveData[subLane];
                    LaneGroup laneGroup = default(LaneGroup);
                    laneGroup.m_StartDirection = math.normalizesafe(MathUtils.StartTangent(curve.m_Bezier).xz);
                    laneGroup.m_EndDirection = math.normalizesafe(-MathUtils.EndTangent(curve.m_Bezier).xz);
                    laneGroup.m_LaneRange = new int2(i, i);
                    laneGroup.m_IsUnsafe = (pedestrianLane.m_Flags & PedestrianLaneFlags.Unsafe) != 0;
                    LaneGroup value = laneGroup;
                    pedestrianLanes.Add(in value);
                }
                else if (m_MasterLaneData.HasComponent(subLane))
                {
                    MasterLane masterLane = m_MasterLaneData[subLane];
                    Curve curve2 = m_CurveData[subLane];
                    LaneGroup laneGroup = default(LaneGroup);
                    laneGroup.m_StartDirection = math.normalizesafe(MathUtils.StartTangent(curve2.m_Bezier).xz);
                    laneGroup.m_EndDirection = math.normalizesafe(-MathUtils.EndTangent(curve2.m_Bezier).xz);
                    laneGroup.m_LaneRange = new int2(masterLane.m_MinIndex - 1, masterLane.m_MaxIndex);
                    LaneGroup value2 = laneGroup;
                    if (m_CarLaneData.HasComponent(subLane))
                    {
                        CarLane carLane = m_CarLaneData[subLane];
                        value2.m_IsStraight = (carLane.m_Flags & (CarLaneFlags.UTurnLeft | CarLaneFlags.TurnLeft | CarLaneFlags.TurnRight | CarLaneFlags.UTurnRight | CarLaneFlags.GentleTurnLeft | CarLaneFlags.GentleTurnRight)) == 0;
                        value2.m_IsUnsafe = (carLane.m_Flags & CarLaneFlags.Unsafe) != 0;
                        value2.m_IsTurnLeft = (carLane.m_Flags & (CarLaneFlags.UTurnLeft | CarLaneFlags.TurnLeft | CarLaneFlags.GentleTurnLeft)) != 0;
                        value2.m_IsTurnRight = (carLane.m_Flags & (CarLaneFlags.TurnRight | CarLaneFlags.UTurnRight | CarLaneFlags.GentleTurnRight)) != 0;
                    }
                    else
                    {
                        value2.m_IsStraight = true;
                    }

                    vehicleLanes.Add(in value2);
                }
                else if (!m_SlaveLaneData.HasComponent(subLane))
                {
                    Curve curve3 = m_CurveData[subLane];
                    LaneGroup laneGroup = default(LaneGroup);
                    laneGroup.m_StartDirection = math.normalizesafe(MathUtils.StartTangent(curve3.m_Bezier).xz);
                    laneGroup.m_EndDirection = math.normalizesafe(-MathUtils.EndTangent(curve3.m_Bezier).xz);
                    laneGroup.m_LaneRange = new int2(i, i);
                    LaneGroup value3 = laneGroup;
                    if (m_CarLaneData.HasComponent(subLane))
                    {
                        CarLane carLane2 = m_CarLaneData[subLane];
                        value3.m_IsStraight = (carLane2.m_Flags & (CarLaneFlags.UTurnLeft | CarLaneFlags.TurnLeft | CarLaneFlags.TurnRight | CarLaneFlags.UTurnRight | CarLaneFlags.GentleTurnLeft | CarLaneFlags.GentleTurnRight)) == 0;
                        value3.m_IsUnsafe = (carLane2.m_Flags & CarLaneFlags.Unsafe) != 0;
                    }
                    else
                    {
                        value3.m_IsStraight = true;
                        value3.m_IsTrack = true;
                    }

                    vehicleLanes.Add(in value3);
                }
            }
        }

        private void ProcessVehicleLaneGroups(NativeList<LaneGroup> vehicleLanes, NativeList<LaneGroup> groups, bool isLevelCrossing, out int groupCount, ref CustomTrafficLights customTrafficLights, out int ways, out uint pattern)
        {
            groupCount = 0;
            while (vehicleLanes.Length > 0)
            {
                LaneGroup value = vehicleLanes[0];
                value.m_GroupIndex = groupCount++;
                groups.Add(in value);
                vehicleLanes.RemoveAtSwapBack(0);
                int num = 0;
                while (num < vehicleLanes.Length)
                {
                    LaneGroup value2 = vehicleLanes[num];
                    if ((!isLevelCrossing | (value.m_IsTrack == value2.m_IsTrack)) && math.dot(value.m_StartDirection, value2.m_StartDirection) > 0.999f)
                    {
                        value2.m_GroupIndex = value.m_GroupIndex;
                        groups.Add(in value2);
                        vehicleLanes.RemoveAtSwapBack(num);
                    }
                    else
                    {
                        num++;
                    }
                }
            }

            ways = 0;
            for (int j = 0; j < groups.Length; j++)
            {
                LaneGroup group = groups[j];
                ways = math.max(group.m_GroupIndex, ways);
            }
            ways++;
            pattern = customTrafficLights.GetPattern(ways);
            if (!TrafficLightPatterns.IsValidPattern(ways, pattern))
            {
                pattern = (int) TrafficLightPatterns.Pattern.Vanilla;
            }

            int i = 0;
            groupCount = 0;
            while (i < groups.Length)
            {
                LaneGroup laneGroup = groups[i++];
                groupCount = math.select(laneGroup.m_GroupIndex + 1, groupCount, laneGroup.m_IsCombined);
                float2 x = default(float2);
                float2 x2 = default(float2);
                float num2 = 1f;
                if (laneGroup.m_IsStraight && !laneGroup.m_IsCombined)
                {
                    x = laneGroup.m_StartDirection;
                    x2 = laneGroup.m_EndDirection;
                    num2 = math.dot(laneGroup.m_StartDirection, laneGroup.m_EndDirection);
                }

                for (; i < groups.Length; i++)
                {
                    LaneGroup laneGroup2 = groups[i];
                    if (laneGroup2.m_GroupIndex != laneGroup.m_GroupIndex)
                    {
                        break;
                    }

                    if (laneGroup2.m_IsStraight && !laneGroup2.m_IsCombined)
                    {
                        float num3 = math.dot(laneGroup2.m_StartDirection, laneGroup2.m_EndDirection);
                        if (num3 < num2)
                        {
                            x = laneGroup2.m_StartDirection;
                            x2 = laneGroup2.m_EndDirection;
                            num2 = num3;
                        }
                    }
                }

                if (num2 >= 0f)
                {
                    continue;
                }

                int num4 = i;
                while (num4 < groups.Length)
                {
                    int num5 = num4;
                    int num6 = num4;
                    LaneGroup laneGroup3 = groups[num4++];
                    bool flag = false;
                    if (!laneGroup3.m_IsCombined)
                    {
                        if (isLevelCrossing)
                        {
                            if (laneGroup.m_IsTrack == laneGroup3.m_IsTrack)
                            {
                                flag = true;
                            }
                        }
                        else if (laneGroup3.m_IsStraight && math.dot(x, laneGroup3.m_EndDirection) > 0.999f && math.dot(x2, laneGroup3.m_StartDirection) > 0.999f)
                        {
                            flag = true;
                        }
                    }

                    while (num4 < groups.Length)
                    {
                        LaneGroup laneGroup4 = groups[num4];
                        if (laneGroup4.m_GroupIndex != laneGroup3.m_GroupIndex)
                        {
                            break;
                        }

                        if (!laneGroup4.m_IsCombined)
                        {
                            if (isLevelCrossing)
                            {
                                if (laneGroup.m_IsTrack == laneGroup4.m_IsTrack)
                                {
                                    flag = true;
                                }
                            }
                            else if (laneGroup4.m_IsStraight && math.dot(x, laneGroup4.m_EndDirection) > 0.999f && math.dot(x2, laneGroup4.m_StartDirection) > 0.999f)
                            {
                                flag = true;
                            }
                        }

                        num6 = num4++;
                    }

                    if ((pattern & 0xFFFF) != (int) TrafficLightPatterns.Pattern.Vanilla)
                    {
                        flag = false;
                    }

                    if (!flag)
                    {
                        continue;
                    }

                    for (int j = num5; j <= num6; j++)
                    {
                        laneGroup3 = groups[j];
                        laneGroup3.m_GroupIndex = laneGroup.m_GroupIndex;
                        laneGroup3.m_IsCombined = true;
                        groups[j] = laneGroup3;
                    }

                    for (int k = num6 + 1; k < groups.Length; k++)
                    {
                        laneGroup3 = groups[k];
                        if (!laneGroup3.m_IsCombined)
                        {
                            laneGroup3.m_GroupIndex--;
                            groups[k] = laneGroup3;
                        }
                    }
                }
            }

            if ((pattern & 0xFFFF) == (int) TrafficLightPatterns.Pattern.Vanilla)
            {
                for (int l = 0; l < groups.Length; l++)
                {
                    LaneGroup value3 = groups[l];
                    value3.m_GroupMask = (ushort)(1 << (value3.m_GroupIndex & 0xF));
                    groups[l] = value3;
                }
            }

            TrafficLightPatterns.ProcessVehicleLaneGroups(ref vehicleLanes, ref groups, ref isLevelCrossing, ref groupCount, m_LeftHandTraffic, ways, pattern);
            return;
        }

        private void ProcessPedestrianLaneGroups(DynamicBuffer<SubLane> subLanes, NativeList<LaneGroup> pedestrianLanes, NativeList<LaneGroup> groups, bool isLevelCrossing, ref int groupCount, int ways, uint pattern)
        {
            if (groupCount <= 1)
            {
                int num = groupCount++;
                for (int i = 0; i < pedestrianLanes.Length; i++)
                {
                    LaneGroup value = pedestrianLanes[i];
                    value.m_GroupMask = (ushort)(1 << (num & 0xF));
                    groups.Add(in value);
                }

                return;
            }

            int length = groups.Length;
            int num2 = -1;
            float4 x = default(float4);
            for (int j = 0; j < pedestrianLanes.Length; j++)
            {
                LaneGroup value2 = pedestrianLanes[j];
                value2.m_GroupMask = (ushort)((1 << math.min(16, groupCount)) - 1);
                Entity subLane = subLanes[value2.m_LaneRange.x].m_SubLane;
                if (!value2.m_IsUnsafe && m_Overlaps.HasBuffer(subLane))
                {
                    DynamicBuffer<LaneOverlap> dynamicBuffer = m_Overlaps[subLane];
                    for (int k = 0; k < length; k++)
                    {
                        LaneGroup laneGroup = groups[k];
                        bool flag;
                        if (isLevelCrossing)
                        {
                            flag = !laneGroup.m_IsTrack;
                        }
                        else
                        {
                            flag = !laneGroup.m_IsStraight;
                            if (flag)
                            {
                                x.x = math.dot(value2.m_StartDirection, laneGroup.m_StartDirection);
                                x.y = math.dot(value2.m_StartDirection, laneGroup.m_EndDirection);
                                x.z = math.dot(value2.m_EndDirection, laneGroup.m_StartDirection);
                                x.w = math.dot(value2.m_EndDirection, laneGroup.m_EndDirection);
                                x = math.abs(x);
                                flag = x.x + x.z > x.y + x.w;
                            }
                        }

                        bool flag2 = false;
                        if (!flag)
                        {
                            for (int l = laneGroup.m_LaneRange.x; l <= laneGroup.m_LaneRange.y; l++)
                            {
                                for (int m = 0; m < dynamicBuffer.Length; m++)
                                {
                                    flag2 |= dynamicBuffer[m].m_Other == subLanes[l].m_SubLane;
                                }
                            }
                        }

                        if (!flag && flag2)
                        {
                            value2.m_GroupMask &= (ushort)(~laneGroup.m_GroupMask);
                        }
                    }
                }

                if ((pattern & (int) TrafficLightPatterns.Pattern.ExclusivePedestrian) != 0)
                {
                    value2.m_GroupMask = 0;
                }

                if (value2.m_GroupMask == 0)
                {
                    if (num2 == -1)
                    {
                        num2 = groupCount++;
                    }

                    value2.m_GroupMask = (ushort)(1 << (num2 & 0xF));
                }

                groups.Add(in value2);
            }
        }

        private void InitializeTrafficLights(DynamicBuffer<SubLane> subLanes, NativeList<LaneGroup> groups, int groupCount, bool isLevelCrossing, ref TrafficLights trafficLights)
        {
            trafficLights.m_SignalGroupCount = (byte)math.min(16, groupCount);
            if (trafficLights.m_CurrentSignalGroup > trafficLights.m_SignalGroupCount || trafficLights.m_NextSignalGroup > trafficLights.m_SignalGroupCount)
            {
                trafficLights.m_CurrentSignalGroup = 0;
                trafficLights.m_NextSignalGroup = 0;
                trafficLights.m_Timer = 0;
                trafficLights.m_State = TrafficLightState.None;
            }

            for (int i = 0; i < groups.Length; i++)
            {
                LaneGroup laneGroup = groups[i];
                sbyte @default = (sbyte)math.select(0, -1, isLevelCrossing & laneGroup.m_IsTrack);
                for (int j = laneGroup.m_LaneRange.x; j <= laneGroup.m_LaneRange.y; j++)
                {
                    Entity subLane = subLanes[j].m_SubLane;
                    LaneSignal laneSignal = m_LaneSignalData[subLane];
                    laneSignal.m_GroupMask = laneGroup.m_GroupMask;
                    laneSignal.m_Default = @default;
                    if (m_CarLaneData.HasComponent(subLane))
                    {
                        laneSignal.m_Flags |= LaneSignalFlags.CanExtend;
                    }

                    TrafficLightSystem.UpdateLaneSignal(trafficLights, ref laneSignal);
                    m_LaneSignalData[subLane] = laneSignal;
                }
            }
        }

        void IJobChunk.Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
        {
            Execute(in chunk, unfilteredChunkIndex, useEnabledMask, in chunkEnabledMask);
        }
    }

    private struct TypeHandle
    {
        [ReadOnly]
        public BufferTypeHandle<SubLane> __Game_Net_SubLane_RO_BufferTypeHandle;

        public ComponentTypeHandle<TrafficLights> __Game_Net_TrafficLights_RW_ComponentTypeHandle;

        [ReadOnly]
        public ComponentLookup<MasterLane> __Game_Net_MasterLane_RO_ComponentLookup;

        [ReadOnly]
        public ComponentLookup<SlaveLane> __Game_Net_SlaveLane_RO_ComponentLookup;

        [ReadOnly]
        public ComponentLookup<CarLane> __Game_Net_CarLane_RO_ComponentLookup;

        [ReadOnly]
        public ComponentLookup<PedestrianLane> __Game_Net_PedestrianLane_RO_ComponentLookup;

        [ReadOnly]
        public ComponentLookup<SecondaryLane> __Game_Net_SecondaryLane_RO_ComponentLookup;

        [ReadOnly]
        public ComponentLookup<Curve> __Game_Net_Curve_RO_ComponentLookup;

        [ReadOnly]
        public BufferLookup<LaneOverlap> __Game_Net_LaneOverlap_RO_BufferLookup;

        public ComponentLookup<LaneSignal> __Game_Net_LaneSignal_RW_ComponentLookup;

        public ComponentTypeHandle<CustomTrafficLights> __CustomTrafficLights_RW_ComponentTypeHandle;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void __AssignHandles(ref SystemState state)
        {
            __Game_Net_SubLane_RO_BufferTypeHandle = state.GetBufferTypeHandle<SubLane>(isReadOnly: true);
            __Game_Net_TrafficLights_RW_ComponentTypeHandle = state.GetComponentTypeHandle<TrafficLights>();
            __Game_Net_MasterLane_RO_ComponentLookup = state.GetComponentLookup<MasterLane>(isReadOnly: true);
            __Game_Net_SlaveLane_RO_ComponentLookup = state.GetComponentLookup<SlaveLane>(isReadOnly: true);
            __Game_Net_CarLane_RO_ComponentLookup = state.GetComponentLookup<CarLane>(isReadOnly: true);
            __Game_Net_PedestrianLane_RO_ComponentLookup = state.GetComponentLookup<PedestrianLane>(isReadOnly: true);
            __Game_Net_SecondaryLane_RO_ComponentLookup = state.GetComponentLookup<SecondaryLane>(isReadOnly: true);
            __Game_Net_Curve_RO_ComponentLookup = state.GetComponentLookup<Curve>(isReadOnly: true);
            __Game_Net_LaneOverlap_RO_BufferLookup = state.GetBufferLookup<LaneOverlap>(isReadOnly: true);
            __Game_Net_LaneSignal_RW_ComponentLookup = state.GetComponentLookup<LaneSignal>();
            __CustomTrafficLights_RW_ComponentTypeHandle = state.GetComponentTypeHandle<CustomTrafficLights>();
        }
    }

    private EntityQuery m_TrafficLightsQuery;

    private TypeHandle __TypeHandle;

    private Game.City.CityConfigurationSystem m_CityConfigurationSystem;

    [Preserve]
    protected override void OnCreate()
    {
        base.OnCreate();
        m_CityConfigurationSystem = World.GetOrCreateSystemManaged<Game.City.CityConfigurationSystem>();
        m_TrafficLightsQuery = GetEntityQuery(new EntityQueryDesc
        {
            All = new ComponentType[1] { ComponentType.ReadOnly<TrafficLights>() },
            Any = new ComponentType[1] { ComponentType.ReadOnly<Updated>() }
        });
        RequireForUpdate(m_TrafficLightsQuery);
    }

    [Preserve]
    protected override void OnUpdate()
    {
        __TypeHandle.__Game_Net_LaneSignal_RW_ComponentLookup.Update(ref base.CheckedStateRef);
        __TypeHandle.__Game_Net_LaneOverlap_RO_BufferLookup.Update(ref base.CheckedStateRef);
        __TypeHandle.__Game_Net_Curve_RO_ComponentLookup.Update(ref base.CheckedStateRef);
        __TypeHandle.__Game_Net_SecondaryLane_RO_ComponentLookup.Update(ref base.CheckedStateRef);
        __TypeHandle.__Game_Net_PedestrianLane_RO_ComponentLookup.Update(ref base.CheckedStateRef);
        __TypeHandle.__Game_Net_CarLane_RO_ComponentLookup.Update(ref base.CheckedStateRef);
        __TypeHandle.__Game_Net_SlaveLane_RO_ComponentLookup.Update(ref base.CheckedStateRef);
        __TypeHandle.__Game_Net_MasterLane_RO_ComponentLookup.Update(ref base.CheckedStateRef);
        __TypeHandle.__Game_Net_TrafficLights_RW_ComponentTypeHandle.Update(ref base.CheckedStateRef);
        __TypeHandle.__Game_Net_SubLane_RO_BufferTypeHandle.Update(ref base.CheckedStateRef);
        __TypeHandle.__CustomTrafficLights_RW_ComponentTypeHandle.Update(ref base.CheckedStateRef);
        InitializeTrafficLightsJob jobData = new InitializeTrafficLightsJob{
            m_LeftHandTraffic = m_CityConfigurationSystem.leftHandTraffic,
            m_CustomTrafficLightsType = __TypeHandle.__CustomTrafficLights_RW_ComponentTypeHandle
        };
        jobData.m_SubLaneType = __TypeHandle.__Game_Net_SubLane_RO_BufferTypeHandle;
        jobData.m_TrafficLightsType = __TypeHandle.__Game_Net_TrafficLights_RW_ComponentTypeHandle;
        jobData.m_MasterLaneData = __TypeHandle.__Game_Net_MasterLane_RO_ComponentLookup;
        jobData.m_SlaveLaneData = __TypeHandle.__Game_Net_SlaveLane_RO_ComponentLookup;
        jobData.m_CarLaneData = __TypeHandle.__Game_Net_CarLane_RO_ComponentLookup;
        jobData.m_PedestrianLaneData = __TypeHandle.__Game_Net_PedestrianLane_RO_ComponentLookup;
        jobData.m_SecondaryLaneData = __TypeHandle.__Game_Net_SecondaryLane_RO_ComponentLookup;
        jobData.m_CurveData = __TypeHandle.__Game_Net_Curve_RO_ComponentLookup;
        jobData.m_Overlaps = __TypeHandle.__Game_Net_LaneOverlap_RO_BufferLookup;
        jobData.m_LaneSignalData = __TypeHandle.__Game_Net_LaneSignal_RW_ComponentLookup;
        JobHandle dependency = JobChunkExtensions.ScheduleParallel(jobData, m_TrafficLightsQuery, base.Dependency);
        base.Dependency = dependency;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void __AssignQueries(ref SystemState state)
    {
    }

    protected override void OnCreateForCompiler()
    {
        base.OnCreateForCompiler();
        __AssignQueries(ref base.CheckedStateRef);
        __TypeHandle.__AssignHandles(ref base.CheckedStateRef);
    }

    [Preserve]
    public PatchedTrafficLightInitializationSystem()
    {
    }
}