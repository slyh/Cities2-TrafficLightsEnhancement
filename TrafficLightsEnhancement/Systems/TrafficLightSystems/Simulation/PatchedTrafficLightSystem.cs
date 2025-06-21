#region Assembly Game, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null
// location unknown
// Decompiled with ICSharpCode.Decompiler 9.1.0.7988
#endregion

using System.Runtime.CompilerServices;
using Colossal.Mathematics;
using Game.Common;
using Game.Net;
using Game.Objects;
using Game.Prefabs;
using Game.Tools;
using Unity.Burst;
using Unity.Burst.Intrinsics;
using Unity.Collections;
using Unity.Entities;
using Unity.Entities.Internal;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine.Scripting;

using C2VM.TrafficLightsEnhancement.Components;
using Game;
using Game.Simulation;

namespace C2VM.TrafficLightsEnhancement.Systems.TrafficLightSystems.Simulation;

[CompilerGenerated]
public partial class PatchedTrafficLightSystem : GameSystemBase
{
    [BurstCompile]
    private struct UpdateTrafficLightsJob : IJobChunk
    {
        [ReadOnly]
        public EntityTypeHandle m_EntityType;

        [ReadOnly]
        public BufferTypeHandle<Game.Net.SubLane> m_SubLaneType;

        [ReadOnly]
        public BufferTypeHandle<ConnectedEdge> m_ConnectedEdgeType;

        [ReadOnly]
        public BufferTypeHandle<Game.Objects.SubObject> m_SubObjectType;

        public ComponentTypeHandle<TrafficLights> m_TrafficLightsType;

        [ReadOnly]
        public ComponentLookup<Owner> m_OwnerData;

        [ReadOnly]
        public ComponentLookup<Node> m_NodeData;

        [ReadOnly]
        public ComponentLookup<Edge> m_EdgeData;

        [ReadOnly]
        public ComponentLookup<Curve> m_CurveData;

        [ReadOnly]
        public ComponentLookup<Lane> m_LaneData;

        [ReadOnly]
        public ComponentLookup<LaneReservation> m_LaneReservationData;

        [ReadOnly]
        public ComponentLookup<Transform> m_TransformData;

        [ReadOnly]
        public ComponentLookup<PrefabRef> m_PrefabRefData;

        [ReadOnly]
        public ComponentLookup<CarLaneData> m_PrefabCarLaneData;

        [ReadOnly]
        public ComponentLookup<MoveableBridgeData> m_PrefabMoveableBridgeData;

        [ReadOnly]
        public ComponentLookup<ObjectGeometryData> m_PrefabObjectGeometryData;

        [ReadOnly]
        public BufferLookup<LaneObject> m_LaneObjects;

        [ReadOnly]
        public BufferLookup<Game.Net.SubNet> m_SubNets;

        [ReadOnly]
        public BufferLookup<Game.Net.SubLane> m_SubLanes;

        [ReadOnly]
        public BufferLookup<ConnectedEdge> m_ConnectedEdges;

        [NativeDisableParallelForRestriction]
        public ComponentLookup<LaneSignal> m_LaneSignalData;

        [NativeDisableParallelForRestriction]
        public ComponentLookup<TrafficLight> m_TrafficLightData;

        [NativeDisableParallelForRestriction]
        public ComponentLookup<PointOfInterest> m_PointOfInterestData;

        public EntityCommandBuffer.ParallelWriter m_CommandBuffer;

        public ExtraTypeHandle m_ExtraTypeHandle;

        public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
        {
            NativeArray<Entity> nativeArray = chunk.GetNativeArray(m_EntityType);
            NativeArray<TrafficLights> nativeArray2 = chunk.GetNativeArray(ref m_TrafficLightsType);
            BufferAccessor<Game.Net.SubLane> bufferAccessor = chunk.GetBufferAccessor(ref m_SubLaneType);
            BufferAccessor<ConnectedEdge> bufferAccessor2 = chunk.GetBufferAccessor(ref m_ConnectedEdgeType);
            BufferAccessor<Game.Objects.SubObject> bufferAccessor3 = chunk.GetBufferAccessor(ref m_SubObjectType);
            NativeList<Entity> laneSignals = new NativeList<Entity>(30, Allocator.Temp);

            NativeArray<CustomTrafficLights> customTrafficLightsArray = chunk.GetNativeArray(ref m_ExtraTypeHandle.m_CustomTrafficLights);
            BufferAccessor<CustomPhaseData> customPhaseDataBufferAccessor = chunk.GetBufferAccessor(ref m_ExtraTypeHandle.m_CustomPhaseData);

            for (int i = 0; i < nativeArray2.Length; i++)
            {
                TrafficLights trafficLights = nativeArray2[i];
                DynamicBuffer<Game.Net.SubLane> subLanes = bufferAccessor[i];
                DynamicBuffer<Game.Objects.SubObject> subObjects = bufferAccessor3[i];
                if ((trafficLights.m_Flags & TrafficLightFlags.IsSubNode) != 0)
                {
                    continue;
                }

                Entity entity = default(Entity);
                MoveableBridgeData moveableBridgeData = default(MoveableBridgeData);
                FillLaneSignals(subLanes, laneSignals);
                if ((trafficLights.m_Flags & TrafficLightFlags.MoveableBridge) != 0)
                {
                    FindMoveableBridge(subObjects, out entity, out moveableBridgeData);
                    FillLaneSignals(nativeArray[i], bufferAccessor2[i], laneSignals);
                }

                CustomTrafficLights customTrafficLights = i < customTrafficLightsArray.Length ? customTrafficLightsArray[i] : new CustomTrafficLights();
                DynamicBuffer<CustomPhaseData> customPhaseDataBuffer = i < customPhaseDataBufferAccessor.Length ? customPhaseDataBufferAccessor[i] : default;

                if (UpdateTrafficLightState(laneSignals, moveableBridgeData, ref trafficLights, ref customTrafficLights, customPhaseDataBuffer))
                {
                    UpdateLaneSignals(laneSignals, trafficLights);
                    UpdateTrafficLightObjects(subObjects, trafficLights);
                    if (entity != Entity.Null)
                    {
                        ref PointOfInterest valueRW = ref m_PointOfInterestData.GetRefRW(entity).ValueRW;
                        UpdateMoveableBridge(trafficLights, m_TransformData[entity], moveableBridgeData, ref valueRW);
                        m_CommandBuffer.AddComponent<EffectsUpdated>(unfilteredChunkIndex, nativeArray[i]);
                    }
                }

                nativeArray2[i] = trafficLights;
                laneSignals.Clear();
            }

            laneSignals.Dispose();
        }

        private void FillLaneSignals(DynamicBuffer<Game.Net.SubLane> subLanes, NativeList<Entity> laneSignals)
        {
            for (int i = 0; i < subLanes.Length; i++)
            {
                Entity value = subLanes[i].m_SubLane;
                if (m_LaneSignalData.HasComponent(value))
                {
                    laneSignals.Add(in value);
                }
            }
        }

        private void FillLaneSignals(Entity node, DynamicBuffer<ConnectedEdge> connectedEdges, NativeList<Entity> laneSignals)
        {
            for (int i = 0; i < connectedEdges.Length; i++)
            {
                Entity edge = connectedEdges[i].m_Edge;
                if (m_SubNets.TryGetBuffer(edge, out var bufferData))
                {
                    FillLaneSignals(node, edge, bufferData, laneSignals);
                }
            }
        }

        private void FillLaneSignals(Entity node, Entity edge, DynamicBuffer<Game.Net.SubNet> subNets, NativeList<Entity> laneSignals)
        {
            Node componentData = m_NodeData[node];
            Curve curve = m_CurveData[edge];
            float num = math.distancesq(componentData.m_Position, curve.m_Bezier.a);
            float num2 = math.distancesq(componentData.m_Position, curve.m_Bezier.d);
            bool flag = num <= num2;
            for (int i = 0; i < subNets.Length; i++)
            {
                Entity subNet = subNets[i].m_SubNet;
                if (m_NodeData.TryGetComponent(subNet, out componentData))
                {
                    float num3 = math.distancesq(componentData.m_Position, curve.m_Bezier.a);
                    num2 = math.distancesq(componentData.m_Position, curve.m_Bezier.d);
                    bool flag2 = num3 <= num2;
                    if (flag == flag2 && m_SubLanes.TryGetBuffer(subNet, out var bufferData))
                    {
                        FillLaneSignals(bufferData, laneSignals);
                    }
                }
            }
        }

        private bool UpdateTrafficLightState(NativeList<Entity> laneSignals, MoveableBridgeData moveableBridgeData, ref TrafficLights trafficLights, ref CustomTrafficLights customTrafficLights, DynamicBuffer<CustomPhaseData> customPhaseDataBuffer)
        {
            bool canExtend;
            switch (trafficLights.m_State)
            {
                case Game.Net.TrafficLightState.None:
                    if (++trafficLights.m_Timer >= 1)
                    {
                        trafficLights.m_State = Game.Net.TrafficLightState.Beginning;
                        trafficLights.m_CurrentSignalGroup = 0;
                        trafficLights.m_NextSignalGroup = (byte)GetNextSignalGroup(laneSignals, trafficLights, preferChange: true, out canExtend, ref customTrafficLights);
                        trafficLights.m_Timer = 0;
                        return true;
                    }

                    break;
                case Game.Net.TrafficLightState.Beginning:
                    if (++trafficLights.m_Timer >= 1)
                    {
                        trafficLights.m_State = Game.Net.TrafficLightState.Ongoing;
                        trafficLights.m_CurrentSignalGroup = trafficLights.m_NextSignalGroup;
                        trafficLights.m_NextSignalGroup = 0;
                        trafficLights.m_Timer = 0;
                        return true;
                    }

                    break;
                case Game.Net.TrafficLightState.Ongoing:
                    float greenDuration = 2;
                    if (customTrafficLights.GetPatternOnly() == CustomTrafficLights.Patterns.CustomPhase)
                    {
                        float multiplier = trafficLights.m_CurrentSignalGroup > 0 && trafficLights.m_CurrentSignalGroup <= customPhaseDataBuffer.Length ? customPhaseDataBuffer[trafficLights.m_CurrentSignalGroup - 1].m_MinimumDurationMultiplier : 1;
                        greenDuration *= multiplier;
                    }
                    else if ((customTrafficLights.m_PedestrianPhaseGroupMask & 1 << trafficLights.m_CurrentSignalGroup - 1) != 0)
                    {
                        greenDuration *= customTrafficLights.m_PedestrianPhaseDurationMultiplier;
                    }
                    #if VERBOSITY_DEBUG
                    System.Console.WriteLine($"UpdateTrafficLightState m_CurrentSignalGroup {trafficLights.m_CurrentSignalGroup} greenDuration {greenDuration} m_PedestrianPhaseGroupMask {customTrafficLights.m_PedestrianPhaseGroupMask}");
                    #endif
                    if (++trafficLights.m_Timer >= greenDuration)
                    {
                        int num2 = 6;
                        if (moveableBridgeData.m_MovingTime != 0f)
                        {
                            num2 = math.clamp((int)(moveableBridgeData.m_MovingTime * 1.875f + 0.5f), num2, 255);
                        }

                        bool canExtend2;
                        int nextSignalGroup2 = GetNextSignalGroup(laneSignals, trafficLights, trafficLights.m_Timer >= num2, out canExtend2, ref customTrafficLights);
                        if (nextSignalGroup2 != trafficLights.m_CurrentSignalGroup)
                        {
                            trafficLights.m_State = (canExtend2 ? Game.Net.TrafficLightState.Extending : Game.Net.TrafficLightState.Ending);
                            trafficLights.m_NextSignalGroup = (byte)nextSignalGroup2;
                            trafficLights.m_Timer = 0;
                            return true;
                        }

                        return false;
                    }

                    break;
                case Game.Net.TrafficLightState.Extending:
                    if (++trafficLights.m_Timer >= 2)
                    {
                        bool canExtend4;
                        int nextSignalGroup4 = GetNextSignalGroup(laneSignals, trafficLights, preferChange: true, out canExtend4, ref customTrafficLights);
                        if (nextSignalGroup4 == trafficLights.m_CurrentSignalGroup)
                        {
                            trafficLights.m_State = Game.Net.TrafficLightState.Beginning;
                            trafficLights.m_CurrentSignalGroup = 0;
                        }
                        else
                        {
                            trafficLights.m_State = (canExtend4 ? Game.Net.TrafficLightState.Extended : Game.Net.TrafficLightState.Ending);
                        }

                        trafficLights.m_NextSignalGroup = (byte)nextSignalGroup4;
                        trafficLights.m_Timer = 0;
                        return true;
                    }

                    break;
                case Game.Net.TrafficLightState.Extended:
                    if (++trafficLights.m_Timer >= 2)
                    {
                        bool canExtend3;
                        int nextSignalGroup3 = GetNextSignalGroup(laneSignals, trafficLights, preferChange: true, out canExtend3, ref customTrafficLights);
                        if (nextSignalGroup3 == trafficLights.m_CurrentSignalGroup)
                        {
                            trafficLights.m_State = Game.Net.TrafficLightState.Beginning;
                            trafficLights.m_CurrentSignalGroup = 0;
                            trafficLights.m_NextSignalGroup = (byte)nextSignalGroup3;
                            trafficLights.m_Timer = 0;
                            return true;
                        }

                        if (trafficLights.m_Timer >= 4 || !canExtend3)
                        {
                            trafficLights.m_State = Game.Net.TrafficLightState.Ending;
                            trafficLights.m_NextSignalGroup = (byte)nextSignalGroup3;
                            trafficLights.m_Timer = 0;
                            return true;
                        }

                        return false;
                    }

                    break;
                case Game.Net.TrafficLightState.Ending:
                    {
                        if (++trafficLights.m_Timer < 2)
                        {
                            break;
                        }

                        int nextSignalGroup5 = GetNextSignalGroup(laneSignals, trafficLights, preferChange: true, out canExtend, ref customTrafficLights);
                        if ((trafficLights.m_Flags & TrafficLightFlags.MoveableBridge) != 0 && !IsEmpty(laneSignals, nextSignalGroup5))
                        {
                            return false;
                        }

                        if (nextSignalGroup5 != trafficLights.m_NextSignalGroup)
                        {
                            if (RequireEnding(laneSignals, nextSignalGroup5))
                            {
                                trafficLights.m_CurrentSignalGroup = trafficLights.m_NextSignalGroup;
                            }
                            else
                            {
                                trafficLights.m_State = Game.Net.TrafficLightState.Changing;
                            }

                            trafficLights.m_NextSignalGroup = (byte)nextSignalGroup5;
                        }
                        else
                        {
                            trafficLights.m_State = Game.Net.TrafficLightState.Changing;
                        }

                        trafficLights.m_Timer = 0;
                        return true;
                    }
                case Game.Net.TrafficLightState.Changing:
                    {
                        int num = 1;
                        if (moveableBridgeData.m_MovingTime != 0f && trafficLights.m_CurrentSignalGroup != trafficLights.m_NextSignalGroup)
                        {
                            num = math.clamp((int)(moveableBridgeData.m_MovingTime * 0.9375f + 0.5f), num, 255);
                        }

                        if (++trafficLights.m_Timer < num)
                        {
                            break;
                        }

                        int nextSignalGroup = GetNextSignalGroup(laneSignals, trafficLights, preferChange: true, out canExtend, ref customTrafficLights);
                        if (nextSignalGroup != trafficLights.m_NextSignalGroup)
                        {
                            if (RequireEnding(laneSignals, nextSignalGroup))
                            {
                                trafficLights.m_CurrentSignalGroup = trafficLights.m_NextSignalGroup;
                                trafficLights.m_State = Game.Net.TrafficLightState.Ending;
                            }
                            else if (moveableBridgeData.m_MovingTime == 0f)
                            {
                                trafficLights.m_State = Game.Net.TrafficLightState.Beginning;
                            }
                            else
                            {
                                trafficLights.m_CurrentSignalGroup = trafficLights.m_NextSignalGroup;
                            }

                            trafficLights.m_NextSignalGroup = (byte)nextSignalGroup;
                        }
                        else
                        {
                            trafficLights.m_State = Game.Net.TrafficLightState.Beginning;
                        }

                        trafficLights.m_Timer = 0;
                        return true;
                    }
            }

            ClearPriority(laneSignals);
            return false;
        }

        private bool RequireEnding(NativeList<Entity> laneSignals, int nextSignalGroup)
        {
            int num = 0;
            if (nextSignalGroup > 0)
            {
                num |= 1 << nextSignalGroup - 1;
            }

            for (int i = 0; i < laneSignals.Length; i++)
            {
                LaneSignal laneSignal = m_LaneSignalData[laneSignals[i]];
                if (laneSignal.m_Signal == LaneSignalType.Go && (laneSignal.m_GroupMask & num) == 0)
                {
                    return true;
                }
            }

            return false;
        }

        private int GetNextSignalGroup(NativeList<Entity> laneSignals, TrafficLights trafficLights, bool preferChange, out bool canExtend, ref CustomTrafficLights customTrafficLights)
        {
            Entity entity = Entity.Null;
            Entity entity2 = Entity.Null;
            int num = 0;
            int num2 = 0;
            int num3 = 0;
            int num4 = 0;
            int y = math.select(127, 1, (trafficLights.m_Flags & TrafficLightFlags.MoveableBridge) != 0);
            for (int i = 0; i < laneSignals.Length; i++)
            {
                Entity entity3 = laneSignals[i];
                LaneSignal value = m_LaneSignalData[entity3];

                ExtraLaneSignal extraLaneSignal = new ExtraLaneSignal();
                if (m_ExtraTypeHandle.m_ExtraLaneSignal.HasComponent(entity3))
                {
                    extraLaneSignal = m_ExtraTypeHandle.m_ExtraLaneSignal[entity3];
                }
                if ((value.m_GroupMask & (1 << trafficLights.m_CurrentSignalGroup - 1)) != 0)
                {
                    // Reduce priority if the lane has IgnorePriority flag
                    if ((extraLaneSignal.m_IgnorePriorityGroupMask & (1 << trafficLights.m_CurrentSignalGroup - 1)) != 0)
                    {
                        value.m_Priority = value.m_Default;
                    }

                    // Stop pedestrian phase from hogging the green light
                    // if ((customTrafficLights.m_PedestrianPhaseGroupMask & value.m_GroupMask) != 0)
                    // {
                    //     value.m_Priority = value.m_Default;
                    // }
                }

                int num5 = math.min(value.m_Priority, y);
                if (num5 > num)
                {
                    entity = value.m_Petitioner;
                    num = num5;
                    num2 = value.m_GroupMask;
                    num3 = math.select(0, value.m_GroupMask, (value.m_Flags & LaneSignalFlags.CanExtend) != 0);
                }
                else if (num5 == num)
                {
                    num2 |= value.m_GroupMask;
                    num3 |= math.select(0, value.m_GroupMask, (value.m_Flags & LaneSignalFlags.CanExtend) != 0);
                }
                else if (num5 < 0)
                {
                    num4 |= value.m_GroupMask;
                }

                if (value.m_Blocker != Entity.Null)
                {
                    entity2 = value.m_Blocker;
                }

                value.m_Petitioner = Entity.Null;
                value.m_Priority = value.m_Default;
                m_LaneSignalData[entity3] = value;
            }

            if (entity != entity2)
            {
                for (int j = 0; j < laneSignals.Length; j++)
                {
                    Entity entity4 = laneSignals[j];
                    LaneSignal value2 = m_LaneSignalData[entity4];
                    if ((num2 & value2.m_GroupMask) != 0)
                    {
                        value2.m_Blocker = Entity.Null;
                    }
                    else
                    {
                        value2.m_Blocker = entity;
                    }

                    m_LaneSignalData[entity4] = value2;
                }
            }

            if (num == 0)
            {
                preferChange = false;
                num2 &= ~num4;
            }

            int trueValue = (byte)math.select(trafficLights.m_CurrentSignalGroup + 1, 1, trafficLights.m_CurrentSignalGroup >= trafficLights.m_SignalGroupCount);
            int num6 = math.select(math.max(1, trafficLights.m_CurrentSignalGroup), trueValue, preferChange);
            int num7 = math.select(trafficLights.m_CurrentSignalGroup - 1, trafficLights.m_CurrentSignalGroup, preferChange);
            canExtend = preferChange && trafficLights.m_CurrentSignalGroup >= 1 && (num3 & (1 << trafficLights.m_CurrentSignalGroup - 1)) != 0;
            for (int k = num6; k <= trafficLights.m_SignalGroupCount; k++)
            {
                if ((num2 & (1 << k - 1)) != 0)
                {
                    return k;
                }
            }

            for (int l = 1; l <= num7; l++)
            {
                if ((num2 & (1 << l - 1)) != 0)
                {
                    return l;
                }
            }

            return trafficLights.m_CurrentSignalGroup;
        }

        private void UpdateLaneSignals(NativeList<Entity> laneSignals, TrafficLights trafficLights)
        {
            for (int i = 0; i < laneSignals.Length; i++)
            {
                Entity entity = laneSignals[i];
                LaneSignal laneSignal = m_LaneSignalData[entity];
                ExtraLaneSignal extraLaneSignal = new ExtraLaneSignal();
                if (m_ExtraTypeHandle.m_ExtraLaneSignal.HasComponent(entity))
                {
                    extraLaneSignal = m_ExtraTypeHandle.m_ExtraLaneSignal[entity];
                }
                UpdateLaneSignal(trafficLights, ref laneSignal, ref extraLaneSignal);
                laneSignal.m_Petitioner = Entity.Null;
                laneSignal.m_Priority = laneSignal.m_Default;
                m_LaneSignalData[entity] = laneSignal;
            }
        }

        private bool FindMoveableBridge(DynamicBuffer<Game.Objects.SubObject> subObjects, out Entity entity, out MoveableBridgeData moveableBridgeData)
        {
            for (int i = 0; i < subObjects.Length; i++)
            {
                Entity subObject = subObjects[i].m_SubObject;
                if (m_PointOfInterestData.HasComponent(subObject))
                {
                    PrefabRef prefabRef = m_PrefabRefData[subObject];
                    if (m_PrefabMoveableBridgeData.TryGetComponent(prefabRef.m_Prefab, out moveableBridgeData))
                    {
                        entity = subObject;
                        return true;
                    }
                }
            }

            entity = default(Entity);
            moveableBridgeData = default(MoveableBridgeData);
            return false;
        }

        private void UpdateTrafficLightObjects(DynamicBuffer<Game.Objects.SubObject> subObjects, TrafficLights trafficLights)
        {
            for (int i = 0; i < subObjects.Length; i++)
            {
                Entity subObject = subObjects[i].m_SubObject;
                if (m_TrafficLightData.TryGetComponent(subObject, out var componentData))
                {
                    PatchedTrafficLightSystem.UpdateTrafficLightState(trafficLights, ref componentData);
                    m_TrafficLightData[subObject] = componentData;
                }
            }
        }

        private void ClearPriority(NativeList<Entity> laneSignals)
        {
            for (int i = 0; i < laneSignals.Length; i++)
            {
                Entity entity = laneSignals[i];
                LaneSignal value = m_LaneSignalData[entity];
                value.m_Petitioner = Entity.Null;
                value.m_Priority = value.m_Default;
                m_LaneSignalData[entity] = value;
            }
        }

        private bool IsEmpty(NativeList<Entity> laneSignals, int nextSignalGroup)
        {
            if (nextSignalGroup > 0)
            {
                int num = 1 << nextSignalGroup - 1;
                Entity blocker = Entity.Null;
                for (int i = 0; i < laneSignals.Length; i++)
                {
                    Entity entity = laneSignals[i];
                    if ((m_LaneSignalData[entity].m_GroupMask & num) == 0)
                    {
                        if (m_LaneObjects.TryGetBuffer(entity, out var bufferData) && bufferData.Length != 0)
                        {
                            blocker = bufferData[0].m_LaneObject;
                            break;
                        }

                        if (m_LaneReservationData.TryGetComponent(entity, out var componentData) && componentData.GetPriority() >= 100)
                        {
                            blocker = componentData.m_Blocker;
                            break;
                        }

                        if (m_PrefabRefData.TryGetComponent(entity, out var componentData2) && m_PrefabCarLaneData.TryGetComponent(componentData2.m_Prefab, out var componentData3) && (componentData3.m_RoadTypes & RoadTypes.Watercraft) != RoadTypes.None && CheckNextLane(Entity.Null, entity, 0f, 0, out blocker))
                        {
                            break;
                        }
                    }
                }

                if (blocker != Entity.Null)
                {
                    for (int j = 0; j < laneSignals.Length; j++)
                    {
                        Entity entity2 = laneSignals[j];
                        LaneSignal value = m_LaneSignalData[entity2];
                        if (value.m_Blocker == Entity.Null)
                        {
                            value.m_Blocker = blocker;
                            m_LaneSignalData[entity2] = value;
                        }
                    }

                    return false;
                }
            }

            return true;
        }

        private bool CheckNextLane(Entity prevOwner, Entity lane, float distance, int depth, out Entity blocker)
        {
            if (m_OwnerData.TryGetComponent(lane, out var componentData))
            {
                Edge componentData2;
                if (m_ConnectedEdges.TryGetBuffer(componentData.m_Owner, out var bufferData))
                {
                    for (int i = 0; i < bufferData.Length; i++)
                    {
                        ConnectedEdge connectedEdge = bufferData[i];
                        if (!(connectedEdge.m_Edge == prevOwner) && CheckNextLane(componentData.m_Owner, connectedEdge.m_Edge, lane, distance, depth, out blocker))
                        {
                            return true;
                        }
                    }
                }
                else if (m_EdgeData.TryGetComponent(componentData.m_Owner, out componentData2) && (componentData2.m_Start == prevOwner || componentData2.m_End == prevOwner))
                {
                    if (CheckNextLane(componentData.m_Owner, (componentData2.m_End == prevOwner) ? componentData2.m_Start : componentData2.m_End, lane, distance, depth, out blocker))
                    {
                        return true;
                    }

                    if (CheckNextLane(prevOwner, componentData.m_Owner, lane, distance, depth, out blocker))
                    {
                        return true;
                    }
                }
            }

            blocker = Entity.Null;
            return false;
        }

        private bool CheckNextLane(Entity prevOwner, Entity nextOwner, Entity lane, float distance, int depth, out Entity blocker)
        {
            if (m_SubLanes.TryGetBuffer(nextOwner, out var bufferData) && m_LaneData.TryGetComponent(lane, out var componentData))
            {
                for (int i = 0; i < bufferData.Length; i++)
                {
                    Entity subLane = bufferData[i].m_SubLane;
                    if (!m_LaneData.TryGetComponent(subLane, out var componentData2) || !componentData.m_EndNode.Equals(componentData2.m_StartNode) || !m_CurveData.TryGetComponent(subLane, out var componentData3) || !m_LaneObjects.TryGetBuffer(subLane, out var bufferData2))
                    {
                        continue;
                    }

                    for (int j = 0; j < bufferData2.Length; j++)
                    {
                        LaneObject laneObject = bufferData2[j];
                        if (m_PrefabRefData.TryGetComponent(laneObject.m_LaneObject, out var componentData4) && m_PrefabObjectGeometryData.TryGetComponent(componentData4.m_Prefab, out var componentData5))
                        {
                            float3 x = MathUtils.Position(componentData3.m_Bezier, laneObject.m_CurvePosition.x);
                            float3 @float = MathUtils.Size(componentData5.m_Bounds);
                            if (math.distance(x, componentData3.m_Bezier.a) + distance < @float.z - @float.x * 0.25f)
                            {
                                blocker = laneObject.m_LaneObject;
                                return true;
                            }
                        }
                    }

                    float num = distance + componentData3.m_Length;
                    if (num < 150f && depth < 3 && CheckNextLane(prevOwner, subLane, num, depth + 1, out blocker))
                    {
                        return true;
                    }
                }
            }

            blocker = Entity.Null;
            return false;
        }

        void IJobChunk.Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
        {
            Execute(in chunk, unfilteredChunkIndex, useEnabledMask, in chunkEnabledMask);
        }
    }

    private struct TypeHandle
    {
        [ReadOnly]
        public EntityTypeHandle __Unity_Entities_Entity_TypeHandle;

        [ReadOnly]
        public BufferTypeHandle<Game.Net.SubLane> __Game_Net_SubLane_RO_BufferTypeHandle;

        [ReadOnly]
        public BufferTypeHandle<ConnectedEdge> __Game_Net_ConnectedEdge_RO_BufferTypeHandle;

        [ReadOnly]
        public BufferTypeHandle<Game.Objects.SubObject> __Game_Objects_SubObject_RO_BufferTypeHandle;

        public ComponentTypeHandle<TrafficLights> __Game_Net_TrafficLights_RW_ComponentTypeHandle;

        [ReadOnly]
        public ComponentLookup<Owner> __Game_Common_Owner_RO_ComponentLookup;

        [ReadOnly]
        public ComponentLookup<Node> __Game_Net_Node_RO_ComponentLookup;

        [ReadOnly]
        public ComponentLookup<Edge> __Game_Net_Edge_RO_ComponentLookup;

        [ReadOnly]
        public ComponentLookup<Curve> __Game_Net_Curve_RO_ComponentLookup;

        [ReadOnly]
        public ComponentLookup<Lane> __Game_Net_Lane_RO_ComponentLookup;

        [ReadOnly]
        public ComponentLookup<LaneReservation> __Game_Net_LaneReservation_RO_ComponentLookup;

        [ReadOnly]
        public ComponentLookup<Transform> __Game_Objects_Transform_RO_ComponentLookup;

        [ReadOnly]
        public ComponentLookup<PrefabRef> __Game_Prefabs_PrefabRef_RO_ComponentLookup;

        [ReadOnly]
        public ComponentLookup<CarLaneData> __Game_Prefabs_CarLaneData_RO_ComponentLookup;

        [ReadOnly]
        public ComponentLookup<MoveableBridgeData> __Game_Prefabs_MoveableBridgeData_RO_ComponentLookup;

        [ReadOnly]
        public ComponentLookup<ObjectGeometryData> __Game_Prefabs_ObjectGeometryData_RO_ComponentLookup;

        [ReadOnly]
        public BufferLookup<LaneObject> __Game_Net_LaneObject_RO_BufferLookup;

        [ReadOnly]
        public BufferLookup<Game.Net.SubNet> __Game_Net_SubNet_RO_BufferLookup;

        [ReadOnly]
        public BufferLookup<Game.Net.SubLane> __Game_Net_SubLane_RO_BufferLookup;

        [ReadOnly]
        public BufferLookup<ConnectedEdge> __Game_Net_ConnectedEdge_RO_BufferLookup;

        public ComponentLookup<LaneSignal> __Game_Net_LaneSignal_RW_ComponentLookup;

        public ComponentLookup<TrafficLight> __Game_Objects_TrafficLight_RW_ComponentLookup;

        public ComponentLookup<PointOfInterest> __Game_Common_PointOfInterest_RW_ComponentLookup;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void __AssignHandles(ref SystemState state)
        {
            __Unity_Entities_Entity_TypeHandle = state.GetEntityTypeHandle();
            __Game_Net_SubLane_RO_BufferTypeHandle = state.GetBufferTypeHandle<Game.Net.SubLane>(isReadOnly: true);
            __Game_Net_ConnectedEdge_RO_BufferTypeHandle = state.GetBufferTypeHandle<ConnectedEdge>(isReadOnly: true);
            __Game_Objects_SubObject_RO_BufferTypeHandle = state.GetBufferTypeHandle<Game.Objects.SubObject>(isReadOnly: true);
            __Game_Net_TrafficLights_RW_ComponentTypeHandle = state.GetComponentTypeHandle<TrafficLights>();
            __Game_Common_Owner_RO_ComponentLookup = state.GetComponentLookup<Owner>(isReadOnly: true);
            __Game_Net_Node_RO_ComponentLookup = state.GetComponentLookup<Node>(isReadOnly: true);
            __Game_Net_Edge_RO_ComponentLookup = state.GetComponentLookup<Edge>(isReadOnly: true);
            __Game_Net_Curve_RO_ComponentLookup = state.GetComponentLookup<Curve>(isReadOnly: true);
            __Game_Net_Lane_RO_ComponentLookup = state.GetComponentLookup<Lane>(isReadOnly: true);
            __Game_Net_LaneReservation_RO_ComponentLookup = state.GetComponentLookup<LaneReservation>(isReadOnly: true);
            __Game_Objects_Transform_RO_ComponentLookup = state.GetComponentLookup<Transform>(isReadOnly: true);
            __Game_Prefabs_PrefabRef_RO_ComponentLookup = state.GetComponentLookup<PrefabRef>(isReadOnly: true);
            __Game_Prefabs_CarLaneData_RO_ComponentLookup = state.GetComponentLookup<CarLaneData>(isReadOnly: true);
            __Game_Prefabs_MoveableBridgeData_RO_ComponentLookup = state.GetComponentLookup<MoveableBridgeData>(isReadOnly: true);
            __Game_Prefabs_ObjectGeometryData_RO_ComponentLookup = state.GetComponentLookup<ObjectGeometryData>(isReadOnly: true);
            __Game_Net_LaneObject_RO_BufferLookup = state.GetBufferLookup<LaneObject>(isReadOnly: true);
            __Game_Net_SubNet_RO_BufferLookup = state.GetBufferLookup<Game.Net.SubNet>(isReadOnly: true);
            __Game_Net_SubLane_RO_BufferLookup = state.GetBufferLookup<Game.Net.SubLane>(isReadOnly: true);
            __Game_Net_ConnectedEdge_RO_BufferLookup = state.GetBufferLookup<ConnectedEdge>(isReadOnly: true);
            __Game_Net_LaneSignal_RW_ComponentLookup = state.GetComponentLookup<LaneSignal>();
            __Game_Objects_TrafficLight_RW_ComponentLookup = state.GetComponentLookup<TrafficLight>();
            __Game_Common_PointOfInterest_RW_ComponentLookup = state.GetComponentLookup<PointOfInterest>();
        }
    }

    private const uint UPDATE_INTERVAL = 64u;

    private SimulationSystem m_SimulationSystem;

    private EndFrameBarrier m_EndFrameBarrier;

    private EntityQuery m_TrafficLightQuery;

    private TypeHandle __TypeHandle;

    private ExtraTypeHandle m_ExtraTypeHandle;

    public override int GetUpdateInterval(SystemUpdatePhase phase)
    {
        return 4;
    }

    [Preserve]
    protected override void OnCreate()
    {
        base.OnCreate();
        m_SimulationSystem = base.World.GetOrCreateSystemManaged<SimulationSystem>();
        m_EndFrameBarrier = base.World.GetOrCreateSystemManaged<EndFrameBarrier>();
        m_TrafficLightQuery = GetEntityQuery(ComponentType.ReadWrite<TrafficLights>(), ComponentType.ReadOnly<UpdateFrame>(), ComponentType.Exclude<Deleted>(), ComponentType.Exclude<Destroyed>(), ComponentType.Exclude<Temp>());
        RequireForUpdate(m_TrafficLightQuery);
    }

    [Preserve]
    protected override void OnUpdate()
    {
        m_TrafficLightQuery.ResetFilter();
        m_TrafficLightQuery.SetSharedComponentFilter(new UpdateFrame(SimulationUtils.GetUpdateFrameWithInterval(m_SimulationSystem.frameIndex, (uint)GetUpdateInterval(SystemUpdatePhase.GameSimulation), 16)));
        m_ExtraTypeHandle.Update(ref base.CheckedStateRef);
        JobHandle dependency = JobChunkExtensions.ScheduleParallel(new UpdateTrafficLightsJob
        {
            m_EntityType = InternalCompilerInterface.GetEntityTypeHandle(ref __TypeHandle.__Unity_Entities_Entity_TypeHandle, ref base.CheckedStateRef),
            m_SubLaneType = InternalCompilerInterface.GetBufferTypeHandle(ref __TypeHandle.__Game_Net_SubLane_RO_BufferTypeHandle, ref base.CheckedStateRef),
            m_ConnectedEdgeType = InternalCompilerInterface.GetBufferTypeHandle(ref __TypeHandle.__Game_Net_ConnectedEdge_RO_BufferTypeHandle, ref base.CheckedStateRef),
            m_SubObjectType = InternalCompilerInterface.GetBufferTypeHandle(ref __TypeHandle.__Game_Objects_SubObject_RO_BufferTypeHandle, ref base.CheckedStateRef),
            m_TrafficLightsType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Net_TrafficLights_RW_ComponentTypeHandle, ref base.CheckedStateRef),
            m_OwnerData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Common_Owner_RO_ComponentLookup, ref base.CheckedStateRef),
            m_NodeData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Net_Node_RO_ComponentLookup, ref base.CheckedStateRef),
            m_EdgeData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Net_Edge_RO_ComponentLookup, ref base.CheckedStateRef),
            m_CurveData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Net_Curve_RO_ComponentLookup, ref base.CheckedStateRef),
            m_LaneData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Net_Lane_RO_ComponentLookup, ref base.CheckedStateRef),
            m_LaneReservationData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Net_LaneReservation_RO_ComponentLookup, ref base.CheckedStateRef),
            m_TransformData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Objects_Transform_RO_ComponentLookup, ref base.CheckedStateRef),
            m_PrefabRefData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_PrefabRef_RO_ComponentLookup, ref base.CheckedStateRef),
            m_PrefabCarLaneData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_CarLaneData_RO_ComponentLookup, ref base.CheckedStateRef),
            m_PrefabMoveableBridgeData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_MoveableBridgeData_RO_ComponentLookup, ref base.CheckedStateRef),
            m_PrefabObjectGeometryData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_ObjectGeometryData_RO_ComponentLookup, ref base.CheckedStateRef),
            m_LaneObjects = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Net_LaneObject_RO_BufferLookup, ref base.CheckedStateRef),
            m_SubNets = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Net_SubNet_RO_BufferLookup, ref base.CheckedStateRef),
            m_SubLanes = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Net_SubLane_RO_BufferLookup, ref base.CheckedStateRef),
            m_ConnectedEdges = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Net_ConnectedEdge_RO_BufferLookup, ref base.CheckedStateRef),
            m_LaneSignalData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Net_LaneSignal_RW_ComponentLookup, ref base.CheckedStateRef),
            m_TrafficLightData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Objects_TrafficLight_RW_ComponentLookup, ref base.CheckedStateRef),
            m_PointOfInterestData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Common_PointOfInterest_RW_ComponentLookup, ref base.CheckedStateRef),
            m_CommandBuffer = m_EndFrameBarrier.CreateCommandBuffer().AsParallelWriter(),
            m_ExtraTypeHandle = m_ExtraTypeHandle
        }, m_TrafficLightQuery, base.Dependency);
        base.Dependency = dependency;
        m_EndFrameBarrier.AddJobHandleForProducer(base.Dependency);
    }

    public static void UpdateLaneSignal(TrafficLights trafficLights, ref LaneSignal laneSignal)
    {
        ExtraLaneSignal extraLaneSignal = new();
        UpdateLaneSignal(trafficLights, ref laneSignal, ref extraLaneSignal);
    }

    public static void UpdateLaneSignal(TrafficLights trafficLights, ref LaneSignal laneSignal, ref ExtraLaneSignal extraLaneSignal)
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

        LaneSignalType goSignalType = LaneSignalType.Go;

        if ((extraLaneSignal.m_YieldGroupMask & (1 << trafficLights.m_CurrentSignalGroup - 1)) != 0)
        {
            goSignalType = LaneSignalType.Yield;
        }

        switch (trafficLights.m_State)
        {
            case Game.Net.TrafficLightState.Beginning:
                if ((laneSignal.m_GroupMask & num2) != 0)
                {
                    if (laneSignal.m_Signal != goSignalType)
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
                    laneSignal.m_Signal = goSignalType;
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
                        laneSignal.m_Signal = goSignalType;
                    }
                    else
                    {
                        laneSignal.m_Signal = LaneSignalType.Stop;
                    }
                }
                else if (laneSignal.m_Signal == goSignalType)
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
                    laneSignal.m_Signal = goSignalType;
                }
                else
                {
                    laneSignal.m_Signal = LaneSignalType.Stop;
                }

                break;
            case Game.Net.TrafficLightState.Ending:
                if (laneSignal.m_Signal == goSignalType)
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
                if (laneSignal.m_Signal != goSignalType || (laneSignal.m_GroupMask & num2) == 0)
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
        Game.Objects.TrafficLightState trafficLightState2 = (Game.Objects.TrafficLightState)(((int)trafficLight.m_State >> 4) & 0xF);
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

    public static void UpdateMoveableBridge(TrafficLights trafficLights, Transform transform, MoveableBridgeData moveableBridgeData, ref PointOfInterest pointOfInterest)
    {
        int num = -1;
        if (trafficLights.m_State == Game.Net.TrafficLightState.Beginning || trafficLights.m_State == Game.Net.TrafficLightState.Changing)
        {
            if (trafficLights.m_NextSignalGroup > 0)
            {
                num = trafficLights.m_NextSignalGroup - 1;
            }
        }
        else if (trafficLights.m_State != Game.Net.TrafficLightState.Ending && trafficLights.m_CurrentSignalGroup > 0)
        {
            num = trafficLights.m_CurrentSignalGroup - 1;
        }

        pointOfInterest.m_IsValid = false;
        if (num >= 0 && num <= 2)
        {
            pointOfInterest.m_Position = transform.m_Position;
            pointOfInterest.m_Position.y += moveableBridgeData.m_LiftOffsets[num];
            pointOfInterest.m_IsValid = true;
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void __AssignQueries(ref SystemState state)
    {
        new EntityQueryBuilder(Allocator.Temp).Dispose();
    }

    protected override void OnCreateForCompiler()
    {
        base.OnCreateForCompiler();
        __AssignQueries(ref base.CheckedStateRef);
        __TypeHandle.__AssignHandles(ref base.CheckedStateRef);
        m_ExtraTypeHandle.AssignHandles(ref base.CheckedStateRef);
    }

    [Preserve]
    public PatchedTrafficLightSystem()
    {
    }
}