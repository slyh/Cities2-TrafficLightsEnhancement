#region Assembly Game, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null
// location unknown
// Decompiled with ICSharpCode.Decompiler 8.1.1.7464
#endregion

using System.Runtime.CompilerServices;
using Game;
using Game.Common;
using Game.Net;
using Game.Objects;
using Game.Simulation;
using Game.Tools;
using Unity.Burst;
using Unity.Burst.Intrinsics;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine.Scripting;

namespace C2VM.TrafficLightsEnhancement.Systems.TrafficLightSystem;

[CompilerGenerated]
public class PatchedTrafficLightSystem : GameSystemBase
{
    [BurstCompile]
    private struct UpdateTrafficLightsJob : IJobChunk
    {
        [ReadOnly]
        public BufferTypeHandle<SubLane> m_SubLaneType;

        [ReadOnly]
        public BufferTypeHandle<SubObject> m_SubObjectType;

        public ComponentTypeHandle<TrafficLights> m_TrafficLightsType;

        [NativeDisableParallelForRestriction]
        public ComponentLookup<LaneSignal> m_LaneSignalData;

        [NativeDisableParallelForRestriction]
        public ComponentLookup<TrafficLight> m_TrafficLightData;

        public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
        {
            NativeArray<TrafficLights> nativeArray = chunk.GetNativeArray(ref m_TrafficLightsType);
            BufferAccessor<SubLane> bufferAccessor = chunk.GetBufferAccessor(ref m_SubLaneType);
            BufferAccessor<SubObject> bufferAccessor2 = chunk.GetBufferAccessor(ref m_SubObjectType);
            for (int i = 0; i < nativeArray.Length; i++)
            {
                TrafficLights trafficLights = nativeArray[i];
                DynamicBuffer<SubLane> subLanes = bufferAccessor[i];
                DynamicBuffer<SubObject> subObjects = bufferAccessor2[i];
                UpdateTrafficLightState(subLanes, subObjects, ref trafficLights);
                nativeArray[i] = trafficLights;
            }
        }

        private void UpdateTrafficLightState(DynamicBuffer<SubLane> subLanes, DynamicBuffer<SubObject> subObjects, ref TrafficLights trafficLights)
        {
            bool canExtend;
            switch (trafficLights.m_State)
            {
                case Game.Net.TrafficLightState.None:
                    if (++trafficLights.m_Timer >= 1)
                    {
                        trafficLights.m_State = Game.Net.TrafficLightState.Beginning;
                        trafficLights.m_CurrentSignalGroup = 0;
                        trafficLights.m_NextSignalGroup = (byte)GetNextSignalGroup(subLanes, trafficLights, preferChange: true, out canExtend);
                        trafficLights.m_Timer = 0;
                        UpdateLaneSignals(subLanes, trafficLights);
                        UpdateTrafficLightObjects(subObjects, trafficLights);
                    }
                    else
                    {
                        ClearPriority(subLanes);
                    }

                    break;
                case Game.Net.TrafficLightState.Beginning:
                    if (++trafficLights.m_Timer >= 1)
                    {
                        trafficLights.m_State = Game.Net.TrafficLightState.Ongoing;
                        trafficLights.m_CurrentSignalGroup = trafficLights.m_NextSignalGroup;
                        trafficLights.m_NextSignalGroup = 0;
                        trafficLights.m_Timer = 0;
                        UpdateLaneSignals(subLanes, trafficLights);
                        UpdateTrafficLightObjects(subObjects, trafficLights);
                    }
                    else
                    {
                        ClearPriority(subLanes);
                    }

                    break;
                case Game.Net.TrafficLightState.Ongoing:
                    if (++trafficLights.m_Timer >= 2)
                    {
                        bool canExtend2;
                        int nextSignalGroup3 = GetNextSignalGroup(subLanes, trafficLights, trafficLights.m_Timer >= 6, out canExtend2);
                        if (nextSignalGroup3 != trafficLights.m_CurrentSignalGroup)
                        {
                            if (canExtend2)
                            {
                                trafficLights.m_State = Game.Net.TrafficLightState.Extending;
                                trafficLights.m_NextSignalGroup = (byte)nextSignalGroup3;
                                trafficLights.m_Timer = 0;
                                UpdateLaneSignals(subLanes, trafficLights);
                                UpdateTrafficLightObjects(subObjects, trafficLights);
                            }
                            else
                            {
                                trafficLights.m_State = Game.Net.TrafficLightState.Ending;
                                trafficLights.m_NextSignalGroup = (byte)nextSignalGroup3;
                                trafficLights.m_Timer = 0;
                                UpdateLaneSignals(subLanes, trafficLights);
                                UpdateTrafficLightObjects(subObjects, trafficLights);
                            }
                        }
                    }
                    else
                    {
                        ClearPriority(subLanes);
                    }

                    break;
                case Game.Net.TrafficLightState.Extending:
                    if (++trafficLights.m_Timer >= 2)
                    {
                        bool canExtend3;
                        int nextSignalGroup4 = GetNextSignalGroup(subLanes, trafficLights, preferChange: true, out canExtend3);
                        if (nextSignalGroup4 == trafficLights.m_CurrentSignalGroup)
                        {
                            trafficLights.m_State = Game.Net.TrafficLightState.Beginning;
                            trafficLights.m_CurrentSignalGroup = 0;
                        }
                        else if (!canExtend3)
                        {
                            trafficLights.m_State = Game.Net.TrafficLightState.Ending;
                        }
                        else
                        {
                            trafficLights.m_State = Game.Net.TrafficLightState.Extended;
                        }

                        trafficLights.m_NextSignalGroup = (byte)nextSignalGroup4;
                        trafficLights.m_Timer = 0;
                        UpdateLaneSignals(subLanes, trafficLights);
                        UpdateTrafficLightObjects(subObjects, trafficLights);
                    }
                    else
                    {
                        ClearPriority(subLanes);
                    }

                    break;
                case Game.Net.TrafficLightState.Extended:
                    if (++trafficLights.m_Timer >= 2)
                    {
                        bool canExtend4;
                        int nextSignalGroup5 = GetNextSignalGroup(subLanes, trafficLights, preferChange: true, out canExtend4);
                        if (nextSignalGroup5 == trafficLights.m_CurrentSignalGroup)
                        {
                            trafficLights.m_State = Game.Net.TrafficLightState.Beginning;
                            trafficLights.m_CurrentSignalGroup = 0;
                            trafficLights.m_NextSignalGroup = (byte)nextSignalGroup5;
                            trafficLights.m_Timer = 0;
                            UpdateLaneSignals(subLanes, trafficLights);
                            UpdateTrafficLightObjects(subObjects, trafficLights);
                        }
                        else if (trafficLights.m_Timer >= 4 || !canExtend4)
                        {
                            trafficLights.m_State = Game.Net.TrafficLightState.Ending;
                            trafficLights.m_NextSignalGroup = (byte)nextSignalGroup5;
                            trafficLights.m_Timer = 0;
                            UpdateLaneSignals(subLanes, trafficLights);
                            UpdateTrafficLightObjects(subObjects, trafficLights);
                        }
                    }
                    else
                    {
                        ClearPriority(subLanes);
                    }

                    break;
                case Game.Net.TrafficLightState.Ending:
                    if (++trafficLights.m_Timer >= 2)
                    {
                        int nextSignalGroup2 = GetNextSignalGroup(subLanes, trafficLights, preferChange: true, out canExtend);
                        if (nextSignalGroup2 != trafficLights.m_NextSignalGroup)
                        {
                            if (RequireEnding(subLanes, nextSignalGroup2))
                            {
                                trafficLights.m_CurrentSignalGroup = trafficLights.m_NextSignalGroup;
                            }
                            else
                            {
                                trafficLights.m_State = Game.Net.TrafficLightState.Changing;
                            }

                            trafficLights.m_NextSignalGroup = (byte)nextSignalGroup2;
                        }
                        else
                        {
                            trafficLights.m_State = Game.Net.TrafficLightState.Changing;
                        }

                        trafficLights.m_Timer = 0;
                        UpdateLaneSignals(subLanes, trafficLights);
                        UpdateTrafficLightObjects(subObjects, trafficLights);
                    }
                    else
                    {
                        ClearPriority(subLanes);
                    }

                    break;
                case Game.Net.TrafficLightState.Changing:
                    if (++trafficLights.m_Timer >= 1)
                    {
                        int nextSignalGroup = GetNextSignalGroup(subLanes, trafficLights, preferChange: true, out canExtend);
                        if (nextSignalGroup != trafficLights.m_NextSignalGroup)
                        {
                            if (RequireEnding(subLanes, nextSignalGroup))
                            {
                                trafficLights.m_CurrentSignalGroup = trafficLights.m_NextSignalGroup;
                                trafficLights.m_State = Game.Net.TrafficLightState.Ending;
                            }
                            else
                            {
                                trafficLights.m_State = Game.Net.TrafficLightState.Beginning;
                            }

                            trafficLights.m_NextSignalGroup = (byte)nextSignalGroup;
                        }
                        else
                        {
                            trafficLights.m_State = Game.Net.TrafficLightState.Beginning;
                        }

                        trafficLights.m_Timer = 0;
                        UpdateLaneSignals(subLanes, trafficLights);
                        UpdateTrafficLightObjects(subObjects, trafficLights);
                    }
                    else
                    {
                        ClearPriority(subLanes);
                    }

                    break;
            }
        }

        private bool RequireEnding(DynamicBuffer<SubLane> subLanes, int nextSignalGroup)
        {
            int num = 0;
            if (nextSignalGroup > 0)
            {
                num |= 1 << nextSignalGroup - 1;
            }

            for (int i = 0; i < subLanes.Length; i++)
            {
                Entity subLane = subLanes[i].m_SubLane;
                if (m_LaneSignalData.HasComponent(subLane))
                {
                    LaneSignal laneSignal = m_LaneSignalData[subLane];
                    if (laneSignal.m_Signal == LaneSignalType.Go && (laneSignal.m_GroupMask & num) == 0)
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        private int GetNextSignalGroup(DynamicBuffer<SubLane> subLanes, TrafficLights trafficLights, bool preferChange, out bool canExtend)
        {
            Entity entity = Entity.Null;
            Entity entity2 = Entity.Null;
            int num = 0;
            int num2 = 0;
            int num3 = 0;
            int num4 = 0;
            for (int i = 0; i < subLanes.Length; i++)
            {
                Entity subLane = subLanes[i].m_SubLane;
                if (m_LaneSignalData.HasComponent(subLane))
                {
                    LaneSignal value = m_LaneSignalData[subLane];
                    if (value.m_Priority > num)
                    {
                        entity = value.m_Petitioner;
                        num = value.m_Priority;
                        num2 = value.m_GroupMask;
                        num3 = math.select(0, value.m_GroupMask, (value.m_Flags & LaneSignalFlags.CanExtend) != 0);
                    }
                    else if (value.m_Priority == num)
                    {
                        num2 |= value.m_GroupMask;
                        num3 |= math.select(0, value.m_GroupMask, (value.m_Flags & LaneSignalFlags.CanExtend) != 0);
                    }
                    else if (value.m_Priority < 0)
                    {
                        num4 |= value.m_GroupMask;
                    }

                    if (value.m_Blocker != Entity.Null)
                    {
                        entity2 = value.m_Blocker;
                    }

                    value.m_Petitioner = Entity.Null;
                    value.m_Priority = value.m_Default;
                    m_LaneSignalData[subLane] = value;
                }
            }

            if (entity != entity2)
            {
                for (int j = 0; j < subLanes.Length; j++)
                {
                    Entity subLane2 = subLanes[j].m_SubLane;
                    if (m_LaneSignalData.HasComponent(subLane2))
                    {
                        LaneSignal value2 = m_LaneSignalData[subLane2];
                        if ((num2 & value2.m_GroupMask) != 0)
                        {
                            value2.m_Blocker = Entity.Null;
                        }
                        else
                        {
                            value2.m_Blocker = entity;
                        }

                        m_LaneSignalData[subLane2] = value2;
                    }
                }
            }

            if (num == 0)
            {
                preferChange = false;
                num2 &= ~num4;
            }

            int b = (byte)math.select(trafficLights.m_CurrentSignalGroup + 1, 1, trafficLights.m_CurrentSignalGroup >= trafficLights.m_SignalGroupCount);
            int num5 = math.select(math.max(1, trafficLights.m_CurrentSignalGroup), b, preferChange);
            int num6 = math.select(trafficLights.m_CurrentSignalGroup - 1, trafficLights.m_CurrentSignalGroup, preferChange);
            canExtend = preferChange && trafficLights.m_CurrentSignalGroup >= 1 && (num3 & (1 << trafficLights.m_CurrentSignalGroup - 1)) != 0;
            for (int k = num5; k <= trafficLights.m_SignalGroupCount; k++)
            {
                if ((num2 & (1 << k - 1)) != 0)
                {
                    return k;
                }
            }

            for (int l = 1; l <= num6; l++)
            {
                if ((num2 & (1 << l - 1)) != 0)
                {
                    return l;
                }
            }

            return trafficLights.m_CurrentSignalGroup;
        }

        private void UpdateLaneSignals(DynamicBuffer<SubLane> subLanes, TrafficLights trafficLights)
        {
            for (int i = 0; i < subLanes.Length; i++)
            {
                Entity subLane = subLanes[i].m_SubLane;
                if (m_LaneSignalData.HasComponent(subLane))
                {
                    LaneSignal laneSignal = m_LaneSignalData[subLane];
                    UpdateLaneSignal(trafficLights, ref laneSignal);
                    laneSignal.m_Petitioner = Entity.Null;
                    laneSignal.m_Priority = laneSignal.m_Default;
                    m_LaneSignalData[subLane] = laneSignal;
                }
            }
        }

        private void UpdateTrafficLightObjects(DynamicBuffer<SubObject> subObjects, TrafficLights trafficLights)
        {
            for (int i = 0; i < subObjects.Length; i++)
            {
                Entity subObject = subObjects[i].m_SubObject;
                if (m_TrafficLightData.HasComponent(subObject))
                {
                    TrafficLight trafficLight = m_TrafficLightData[subObject];
                    PatchedTrafficLightSystem.UpdateTrafficLightState(trafficLights, ref trafficLight);
                    m_TrafficLightData[subObject] = trafficLight;
                }
            }
        }

        private void ClearPriority(DynamicBuffer<SubLane> subLanes)
        {
            for (int i = 0; i < subLanes.Length; i++)
            {
                Entity subLane = subLanes[i].m_SubLane;
                if (m_LaneSignalData.HasComponent(subLane))
                {
                    LaneSignal value = m_LaneSignalData[subLane];
                    value.m_Petitioner = Entity.Null;
                    value.m_Priority = value.m_Default;
                    m_LaneSignalData[subLane] = value;
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

        [ReadOnly]
        public BufferTypeHandle<SubObject> __Game_Objects_SubObject_RO_BufferTypeHandle;

        public ComponentTypeHandle<TrafficLights> __Game_Net_TrafficLights_RW_ComponentTypeHandle;

        public ComponentLookup<LaneSignal> __Game_Net_LaneSignal_RW_ComponentLookup;

        public ComponentLookup<TrafficLight> __Game_Objects_TrafficLight_RW_ComponentLookup;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void __AssignHandles(ref SystemState state)
        {
            __Game_Net_SubLane_RO_BufferTypeHandle = state.GetBufferTypeHandle<SubLane>(isReadOnly: true);
            __Game_Objects_SubObject_RO_BufferTypeHandle = state.GetBufferTypeHandle<SubObject>(isReadOnly: true);
            __Game_Net_TrafficLights_RW_ComponentTypeHandle = state.GetComponentTypeHandle<TrafficLights>();
            __Game_Net_LaneSignal_RW_ComponentLookup = state.GetComponentLookup<LaneSignal>();
            __Game_Objects_TrafficLight_RW_ComponentLookup = state.GetComponentLookup<TrafficLight>();
        }
    }

    private const uint UPDATE_INTERVAL = 64u;

    private SimulationSystem m_SimulationSystem;

    private EntityQuery m_TrafficLightQuery;

    private TypeHandle __TypeHandle;

    public override int GetUpdateInterval(SystemUpdatePhase phase)
    {
        return 4;
    }

    [Preserve]
    protected override void OnCreate()
    {
        base.OnCreate();
        m_SimulationSystem = base.World.GetOrCreateSystemManaged<SimulationSystem>();
        m_TrafficLightQuery = GetEntityQuery(ComponentType.ReadWrite<TrafficLights>(), ComponentType.ReadOnly<UpdateFrame>(), ComponentType.Exclude<Deleted>(), ComponentType.Exclude<Destroyed>(), ComponentType.Exclude<Temp>());
        RequireForUpdate(m_TrafficLightQuery);
    }

    [Preserve]
    protected override void OnUpdate()
    {
        m_TrafficLightQuery.ResetFilter();
        m_TrafficLightQuery.SetSharedComponentFilter(new UpdateFrame(SimulationUtils.GetUpdateFrameWithInterval(m_SimulationSystem.frameIndex, (uint)GetUpdateInterval(SystemUpdatePhase.GameSimulation), 16)));
        __TypeHandle.__Game_Objects_TrafficLight_RW_ComponentLookup.Update(ref base.CheckedStateRef);
        __TypeHandle.__Game_Net_LaneSignal_RW_ComponentLookup.Update(ref base.CheckedStateRef);
        __TypeHandle.__Game_Net_TrafficLights_RW_ComponentTypeHandle.Update(ref base.CheckedStateRef);
        __TypeHandle.__Game_Objects_SubObject_RO_BufferTypeHandle.Update(ref base.CheckedStateRef);
        __TypeHandle.__Game_Net_SubLane_RO_BufferTypeHandle.Update(ref base.CheckedStateRef);
        UpdateTrafficLightsJob jobData = default(UpdateTrafficLightsJob);
        jobData.m_SubLaneType = __TypeHandle.__Game_Net_SubLane_RO_BufferTypeHandle;
        jobData.m_SubObjectType = __TypeHandle.__Game_Objects_SubObject_RO_BufferTypeHandle;
        jobData.m_TrafficLightsType = __TypeHandle.__Game_Net_TrafficLights_RW_ComponentTypeHandle;
        jobData.m_LaneSignalData = __TypeHandle.__Game_Net_LaneSignal_RW_ComponentLookup;
        jobData.m_TrafficLightData = __TypeHandle.__Game_Objects_TrafficLight_RW_ComponentLookup;
        JobHandle dependency = JobChunkExtensions.ScheduleParallel(jobData, m_TrafficLightQuery, base.Dependency);
        base.Dependency = dependency;
    }

    public static void UpdateLaneSignal(TrafficLights trafficLights, ref LaneSignal laneSignal)
    {
        int num = 0;
        int num2 = 0;
        if (trafficLights.m_CurrentSignalGroup > 0)
        {
            num |= 1 << trafficLights.m_CurrentSignalGroup - 1;
        }

        if (trafficLights.m_NextSignalGroup > 0)
        {
            num2 |= 1 << trafficLights.m_NextSignalGroup - 1;
        }

        switch (trafficLights.m_State)
        {
            case Game.Net.TrafficLightState.Beginning:
                if ((laneSignal.m_GroupMask & num2) != 0)
                {
                    if (laneSignal.m_Signal != LaneSignalType.Go)
                    {
                        laneSignal.m_Signal = LaneSignalType.Yield;
                    }
                }
                else
                {
                    laneSignal.m_Signal = LaneSignalType.Stop;
                }

                break;
            case Game.Net.TrafficLightState.Ongoing:
                if ((laneSignal.m_GroupMask & num) != 0)
                {
                    laneSignal.m_Signal = LaneSignalType.Go;
                }
                else
                {
                    laneSignal.m_Signal = LaneSignalType.Stop;
                }

                break;
            case Game.Net.TrafficLightState.Extending:
                if ((laneSignal.m_Flags & LaneSignalFlags.CanExtend) != 0)
                {
                    if ((laneSignal.m_GroupMask & num) != 0)
                    {
                        laneSignal.m_Signal = LaneSignalType.Go;
                    }
                    else
                    {
                        laneSignal.m_Signal = LaneSignalType.Stop;
                    }
                }
                else if (laneSignal.m_Signal == LaneSignalType.Go)
                {
                    if ((laneSignal.m_GroupMask & num2) == 0)
                    {
                        laneSignal.m_Signal = LaneSignalType.SafeStop;
                    }
                }
                else
                {
                    laneSignal.m_Signal = LaneSignalType.Stop;
                }

                break;
            case Game.Net.TrafficLightState.Extended:
                if ((laneSignal.m_Flags & LaneSignalFlags.CanExtend) != 0 && (laneSignal.m_GroupMask & num) != 0)
                {
                    laneSignal.m_Signal = LaneSignalType.Go;
                }
                else
                {
                    laneSignal.m_Signal = LaneSignalType.Stop;
                }

                break;
            case Game.Net.TrafficLightState.Ending:
                if (laneSignal.m_Signal == LaneSignalType.Go)
                {
                    if ((laneSignal.m_GroupMask & num2) == 0)
                    {
                        laneSignal.m_Signal = LaneSignalType.SafeStop;
                    }
                }
                else
                {
                    laneSignal.m_Signal = LaneSignalType.Stop;
                }

                break;
            case Game.Net.TrafficLightState.Changing:
                if (laneSignal.m_Signal != LaneSignalType.Go || (laneSignal.m_GroupMask & num2) == 0)
                {
                    laneSignal.m_Signal = LaneSignalType.Stop;
                }

                break;
            default:
                laneSignal.m_Signal = LaneSignalType.None;
                break;
        }
    }

    public static void UpdateTrafficLightState(TrafficLights trafficLights, ref TrafficLight trafficLight)
    {
        int num = 0;
        int num2 = 0;
        if (trafficLights.m_CurrentSignalGroup > 0)
        {
            num |= 1 << trafficLights.m_CurrentSignalGroup - 1;
        }

        if (trafficLights.m_NextSignalGroup > 0)
        {
            num2 |= 1 << trafficLights.m_NextSignalGroup - 1;
        }

        Game.Objects.TrafficLightState trafficLightState = trafficLight.m_State & (Game.Objects.TrafficLightState.Red | Game.Objects.TrafficLightState.Yellow | Game.Objects.TrafficLightState.Green | Game.Objects.TrafficLightState.Flashing);
        Game.Objects.TrafficLightState trafficLightState2 = (Game.Objects.TrafficLightState)((uint)((int)trafficLight.m_State >> 4) & 0xFu);
        Game.Objects.TrafficLightState trafficLightState3 = (((trafficLights.m_Flags & TrafficLightFlags.LevelCrossing) != 0) ? (Game.Objects.TrafficLightState.Yellow | Game.Objects.TrafficLightState.Flashing) : Game.Objects.TrafficLightState.Yellow);
        Game.Objects.TrafficLightState trafficLightState4 = (((trafficLights.m_Flags & TrafficLightFlags.LevelCrossing) == 0) ? Game.Objects.TrafficLightState.Red : (Game.Objects.TrafficLightState.Red | Game.Objects.TrafficLightState.Flashing));
        switch (trafficLights.m_State)
        {
            case Game.Net.TrafficLightState.Beginning:
                if ((trafficLight.m_GroupMask0 & num2) != 0)
                {
                    if (trafficLightState != Game.Objects.TrafficLightState.Green)
                    {
                        trafficLightState = trafficLightState4 | trafficLightState3;
                    }
                }
                else
                {
                    trafficLightState = trafficLightState4;
                }

                trafficLightState2 = (((trafficLight.m_GroupMask1 & num2) == 0) ? Game.Objects.TrafficLightState.Red : Game.Objects.TrafficLightState.Green);
                break;
            case Game.Net.TrafficLightState.Ongoing:
                trafficLightState = (((trafficLight.m_GroupMask0 & num) == 0) ? trafficLightState4 : Game.Objects.TrafficLightState.Green);
                trafficLightState2 = (((trafficLight.m_GroupMask1 & num) == 0) ? Game.Objects.TrafficLightState.Red : Game.Objects.TrafficLightState.Green);
                break;
            case Game.Net.TrafficLightState.Extending:
                trafficLightState = (((trafficLight.m_GroupMask0 & num) == 0) ? trafficLightState4 : Game.Objects.TrafficLightState.Green);
                if (trafficLightState2 == Game.Objects.TrafficLightState.Green)
                {
                    if ((trafficLight.m_GroupMask1 & num2) == 0)
                    {
                        trafficLightState2 = Game.Objects.TrafficLightState.Green | Game.Objects.TrafficLightState.Flashing;
                    }
                }
                else
                {
                    trafficLightState2 = Game.Objects.TrafficLightState.Red;
                }

                break;
            case Game.Net.TrafficLightState.Extended:
                trafficLightState = (((trafficLight.m_GroupMask0 & num) == 0) ? trafficLightState4 : Game.Objects.TrafficLightState.Green);
                if (trafficLightState2 != Game.Objects.TrafficLightState.Green || (trafficLight.m_GroupMask1 & num2) == 0)
                {
                    trafficLightState2 = Game.Objects.TrafficLightState.Red;
                }

                break;
            case Game.Net.TrafficLightState.Ending:
                if (trafficLightState == Game.Objects.TrafficLightState.Green)
                {
                    if ((trafficLight.m_GroupMask0 & num2) == 0)
                    {
                        trafficLightState = trafficLightState3;
                    }
                }
                else
                {
                    trafficLightState = trafficLightState4;
                }

                if (trafficLightState2 == Game.Objects.TrafficLightState.Green)
                {
                    if ((trafficLight.m_GroupMask1 & num2) == 0)
                    {
                        trafficLightState2 = Game.Objects.TrafficLightState.Green | Game.Objects.TrafficLightState.Flashing;
                    }
                }
                else
                {
                    trafficLightState2 = Game.Objects.TrafficLightState.Red;
                }

                break;
            case Game.Net.TrafficLightState.Changing:
                if (trafficLightState != Game.Objects.TrafficLightState.Green || (trafficLight.m_GroupMask0 & num2) == 0)
                {
                    trafficLightState = trafficLightState4;
                }

                if (trafficLightState2 != Game.Objects.TrafficLightState.Green || (trafficLight.m_GroupMask1 & num2) == 0)
                {
                    trafficLightState2 = Game.Objects.TrafficLightState.Red;
                }

                break;
            default:
                trafficLightState = Game.Objects.TrafficLightState.None;
                trafficLightState2 = Game.Objects.TrafficLightState.None;
                break;
        }

        trafficLight.m_State = (Game.Objects.TrafficLightState)((uint)trafficLightState | ((uint)trafficLightState2 << 4));
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
    public PatchedTrafficLightSystem()
    {
    }
}