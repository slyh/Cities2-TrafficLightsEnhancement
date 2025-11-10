#region Assembly Game, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null
// location unknown
// Decompiled with ICSharpCode.Decompiler 9.1.0.7988
#endregion

using System.Runtime.CompilerServices;
using Colossal.Collections;
using Colossal.Mathematics;
using Game.Common;
using Game.Objects;
using Game.Pathfind;
using Game.Prefabs;
using Game.Simulation;
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
using C2VM.TrafficLightsEnhancement.Utils;

using TrafficLights = Game.Net.TrafficLights;
using MasterLane = Game.Net.MasterLane;
using SlaveLane = Game.Net.SlaveLane;
using Curve = Game.Net.Curve;
using Node = Game.Net.Node;
using Lane = Game.Net.Lane;
using LaneOverlap = Game.Net.LaneOverlap;
using ConnectedEdge = Game.Net.ConnectedEdge;
using LaneSignal = Game.Net.LaneSignal;
using CarLane = Game.Net.CarLane;
using PedestrianLane = Game.Net.PedestrianLane;
using SecondaryLane = Game.Net.SecondaryLane;
using Edge = Game.Net.Edge;
using TrafficLightFlags = Game.Net.TrafficLightFlags;
using EdgeIterator = Game.Net.EdgeIterator;
using EdgeIteratorValue = Game.Net.EdgeIteratorValue;
using SubLane = Game.Net.SubLane;
using PedestrianLaneFlags = Game.Net.PedestrianLaneFlags;
using CarLaneFlags = Game.Net.CarLaneFlags;
using RoadTypes = Game.Net.RoadTypes;
using TrafficLightState = Game.Net.TrafficLightState;
using LaneSignalFlags = Game.Net.LaneSignalFlags;

namespace C2VM.TrafficLightsEnhancement.Systems.TrafficLightSystems.Initialisation;

[CompilerGenerated]
public partial class PatchedTrafficLightInitializationSystem : Game.GameSystemBase
{
    private struct LaneGroup
    {
        public float2 m_StartDirection;

        public float2 m_EndDirection;

        public int2 m_LaneRange;

        public int m_GroupIndex;

        public ushort m_GroupMask;

        public bool m_IsStraight;

        public bool m_IsCombined;

        public bool m_IsUnsafe;

        public bool m_IsTrack;

        public bool m_IsWaterway;

        public bool m_IsPedestrian;
    }

    [BurstCompile]
    public struct InitializeTrafficLightsJob : IJobChunk
    {
        [ReadOnly]
        public EntityTypeHandle m_EntityType;

        [ReadOnly]
        public ComponentTypeHandle<Owner> m_OwnerType;

        [ReadOnly]
        public BufferTypeHandle<SubLane> m_SubLaneType;

        [ReadOnly]
        public BufferTypeHandle<Game.Objects.SubObject> m_SubObjectType;

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
        public ComponentLookup<Edge> m_EdgeData;

        [ReadOnly]
        public ComponentLookup<Node> m_NodeData;

        [ReadOnly]
        public ComponentLookup<Lane> m_LaneData;

        [ReadOnly]
        public ComponentLookup<PointOfInterest> m_PointOfInterestData;

        [ReadOnly]
        public ComponentLookup<Temp> m_TempData;

        [ReadOnly]
        public ComponentLookup<Hidden> m_HiddenData;

        [ReadOnly]
        public ComponentLookup<PrefabRef> m_PrefabRefData;

        [ReadOnly]
        public ComponentLookup<CarLaneData> m_PrefabCarLaneData;

        [ReadOnly]
        public ComponentLookup<MoveableBridgeData> m_PrefabMoveableBridgeData;

        [ReadOnly]
        public BufferLookup<SubLane> m_SubLanes;

        [ReadOnly]
        public BufferLookup<LaneOverlap> m_Overlaps;

        [ReadOnly]
        public BufferLookup<ConnectedEdge> m_ConnectedEdges;

        [ReadOnly]
        public BufferLookup<Game.Objects.SubObject> m_SubObjects;

        [NativeDisableParallelForRestriction]
        public ComponentLookup<LaneSignal> m_LaneSignalData;

        public bool m_LeftHandTraffic;

        public Settings.Values m_Settings;

        public ExtraTypeHandle m_ExtraTypeHandle;

        public EntityCommandBuffer.ParallelWriter m_CommandBuffer;

        public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
        {
            NativeList<LaneGroup> groups = new NativeList<LaneGroup>(16, Allocator.Temp);
            NativeList<LaneGroup> vehicleLanes = new NativeList<LaneGroup>(16, Allocator.Temp);
            NativeList<LaneGroup> pedestrianLanes = new NativeList<LaneGroup>(16, Allocator.Temp);
            NativeHashMap<PathNode, int> groupIndexMap = default(NativeHashMap<PathNode, int>);
            NativeArray<Entity> nativeArray = chunk.GetNativeArray(m_EntityType);
            NativeArray<Owner> nativeArray2 = chunk.GetNativeArray(ref m_OwnerType);
            NativeArray<TrafficLights> nativeArray3 = chunk.GetNativeArray(ref m_TrafficLightsType);
            BufferAccessor<SubLane> bufferAccessor = chunk.GetBufferAccessor(ref m_SubLaneType);
            BufferAccessor<Game.Objects.SubObject> bufferAccessor2 = chunk.GetBufferAccessor(ref m_SubObjectType);

            NativeArray<CustomTrafficLights> customTrafficLightsArray = chunk.GetNativeArray(ref m_ExtraTypeHandle.m_CustomTrafficLights);
            NativeArray<Entity> entityArray = chunk.GetNativeArray(m_ExtraTypeHandle.m_Entity);
            BufferAccessor<ConnectedEdge> connectedEdgeAccessor = chunk.GetBufferAccessor(ref m_ExtraTypeHandle.m_ConnectedEdge);
            BufferAccessor<EdgeGroupMask> edgeGroupMaskAccessor = chunk.GetBufferAccessor(ref m_ExtraTypeHandle.m_EdgeGroupMask);
            BufferAccessor<SubLaneGroupMask> subLaneGroupMaskAccessor = chunk.GetBufferAccessor(ref m_ExtraTypeHandle.m_SubLaneGroupMask);
            BufferAccessor<CustomPhaseData> customPhaseDataAccessor = chunk.GetBufferAccessor(ref m_ExtraTypeHandle.m_CustomPhaseData);

            for (int i = 0; i < nativeArray3.Length; i++)
            {
                Entity entity = nativeArray[i];
                TrafficLights trafficLights = nativeArray3[i];
                DynamicBuffer<SubLane> subLanes = bufferAccessor[i];
                bool flag = (trafficLights.m_Flags & TrafficLightFlags.LevelCrossing) != 0;
                bool flag2 = false;
                bool isSubNode = false;
                int groupCount = 0;
                MoveableBridgeData moveableBridgeData = default(MoveableBridgeData);
                DynamicBuffer<Game.Objects.SubObject> value2;
                if (flag && CollectionUtils.TryGet(nativeArray2, i, out var value) && FindMoveableBridgeData(entity, value.m_Owner, out moveableBridgeData))
                {
                    flag2 = true;
                    isSubNode = true;
                    trafficLights.m_Flags |= TrafficLightFlags.MoveableBridge | TrafficLightFlags.IsSubNode;
                }
                else if (flag && CollectionUtils.TryGet(bufferAccessor2, i, out value2) && FindMoveableBridgeData(value2, out moveableBridgeData))
                {
                    flag2 = true;
                    trafficLights.m_Flags &= ~TrafficLightFlags.IsSubNode;
                    trafficLights.m_Flags |= TrafficLightFlags.MoveableBridge;
                }
                else
                {
                    trafficLights.m_Flags &= ~(TrafficLightFlags.MoveableBridge | TrafficLightFlags.IsSubNode);
                }

                CustomTrafficLights customTrafficLights = i < customTrafficLightsArray.Length ? customTrafficLights = customTrafficLightsArray[i] : new CustomTrafficLights(CustomTrafficLights.Patterns.ModDefault);
                if (customTrafficLights.GetPatternOnly() == CustomTrafficLights.Patterns.ModDefault)
                {
                    uint defaultPattern = (uint) CustomTrafficLights.Patterns.Vanilla;
                    if (m_Settings.m_DefaultSplitPhasing)
                    {
                        defaultPattern = (uint) CustomTrafficLights.Patterns.SplitPhasing;
                    }
                    if (m_Settings.m_DefaultAlwaysGreenKerbsideTurn)
                    {
                        defaultPattern |= (uint) CustomTrafficLights.Patterns.AlwaysGreenKerbsideTurn;
                    }
                    if (m_Settings.m_DefaultExclusivePedestrian)
                    {
                        defaultPattern |= (uint) CustomTrafficLights.Patterns.ExclusivePedestrian;
                    }
                    customTrafficLights.SetPattern(defaultPattern);
                }
                customTrafficLights.SetPedestrianPhaseGroupMask(0);

                if ((trafficLights.m_Flags & TrafficLightFlags.MoveableBridge) != 0)
                {
                    customTrafficLights.SetPattern(CustomTrafficLights.Patterns.Vanilla);
                }

                PredefinedPatternsProcessor.ResetExtraLaneSignal(ref this, subLanes, ref trafficLights);
                if (customTrafficLights.GetPatternOnly() == CustomTrafficLights.Patterns.CustomPhase && i < edgeGroupMaskAccessor.Length && i < subLaneGroupMaskAccessor.Length && i < customPhaseDataAccessor.Length)
                {
                    CustomPhaseUtils.ValidateBuffer(ref this, entityArray[i], subLanes, connectedEdgeAccessor[i], edgeGroupMaskAccessor[i], subLaneGroupMaskAccessor[i], m_ExtraTypeHandle.m_SubLane);
                    CustomPhaseProcessor.ProcessLanes(ref this, unfilteredChunkIndex, entityArray[i], connectedEdgeAccessor[i], subLanes, out groupCount, ref trafficLights, ref customTrafficLights, edgeGroupMaskAccessor[i], subLaneGroupMaskAccessor[i], customPhaseDataAccessor[i]);
                }
                else
                {
                    var edgeInfoArray = NodeUtils.GetEdgeInfoList(Allocator.Temp, entityArray[i], ref this, subLanes, connectedEdgeAccessor[i], edgeGroupMaskAccessor[i], subLaneGroupMaskAccessor[i]).AsArray();
                    var pattern = customTrafficLights.GetPattern();
                    if (NodeUtils.HasTrainTrack(edgeInfoArray) || !PredefinedPatternsProcessor.IsValidPattern(edgeInfoArray, pattern))
                    {
                        pattern = CustomTrafficLights.Patterns.Vanilla;
                        customTrafficLights.SetPattern(pattern);
                    }
                    if (customTrafficLights.GetPatternOnly() == CustomTrafficLights.Patterns.SplitPhasing)
                    {
                        PredefinedPatternsProcessor.SetupSplitPhasing(ref this, connectedEdgeAccessor[i], subLanes, out groupCount, ref trafficLights);
                    }
                    else if (customTrafficLights.GetPatternOnly() == CustomTrafficLights.Patterns.ProtectedCentreTurn)
                    {
                        PredefinedPatternsProcessor.SetupProtectedCentreTurn(ref this, connectedEdgeAccessor[i], subLanes, out groupCount, ref trafficLights);
                    }
                    else
                    {
                        FillLaneBuffers(subLanes, vehicleLanes, pedestrianLanes);
                        if (flag2)
                        {
                            ProcessMoveableBridgeLanes(entity, vehicleLanes, pedestrianLanes, groups, subLanes, moveableBridgeData, isSubNode, ref groupIndexMap, out groupCount);
                        }
                        else
                        {
                            ProcessVehicleLaneGroups(vehicleLanes, groups, flag, out groupCount);
                            ProcessPedestrianLaneGroups(subLanes, pedestrianLanes, groups, flag, ref groupCount);
                        }
                        InitializeTrafficLights(subLanes, groups, groupCount, flag, flag2, ref trafficLights);
                        groups.Clear();
                    }
                    if ((customTrafficLights.GetPattern() & CustomTrafficLights.Patterns.AlwaysGreenKerbsideTurn) != 0)
                    {
                        PredefinedPatternsProcessor.AddAlwaysGreenKerbsideTurn(ref this, unfilteredChunkIndex, subLanes, ref groupCount, ref trafficLights);
                    }
                    if ((customTrafficLights.GetPattern() & CustomTrafficLights.Patterns.CentreTurnGiveWay) != 0)
                    {
                        PredefinedPatternsProcessor.AddCentreTurnGiveWay(ref this, unfilteredChunkIndex, subLanes, ref trafficLights);
                    }
                    if ((customTrafficLights.GetPattern() & CustomTrafficLights.Patterns.ExclusivePedestrian) != 0)
                    {
                        PredefinedPatternsProcessor.AddExclusivePedestrianPhase(ref this, subLanes, ref groupCount, ref trafficLights, ref customTrafficLights);
                    }
                }
                if (i < customTrafficLightsArray.Length)
                {
                    customTrafficLightsArray[i] = customTrafficLights;
                }

                InitializeTrafficLights(subLanes, groups, groupCount, flag, flag2, ref trafficLights);
                nativeArray3[i] = trafficLights;
                groups.Clear();
                vehicleLanes.Clear();
                pedestrianLanes.Clear();
            }

            groups.Dispose();
            vehicleLanes.Dispose();
            pedestrianLanes.Dispose();
            if (groupIndexMap.IsCreated)
            {
                groupIndexMap.Dispose();
            }
        }

        private bool FindMoveableBridgeData(Entity node, Entity owner, out MoveableBridgeData moveableBridgeData)
        {
            moveableBridgeData = default(MoveableBridgeData);
            if (!m_EdgeData.TryGetComponent(owner, out var componentData))
            {
                return false;
            }

            Node node2 = m_NodeData[node];
            Curve curve = m_CurveData[owner];
            float num = math.distancesq(node2.m_Position, curve.m_Bezier.a);
            float num2 = math.distancesq(node2.m_Position, curve.m_Bezier.d);
            Entity entity = ((num <= num2) ? componentData.m_Start : componentData.m_End);
            if (!m_SubObjects.TryGetBuffer(entity, out var bufferData))
            {
                return false;
            }

            return FindMoveableBridgeData(bufferData, out moveableBridgeData);
        }

        private bool FindMoveableBridgeData(DynamicBuffer<Game.Objects.SubObject> subObjects, out MoveableBridgeData moveableBridgeData)
        {
            for (int i = 0; i < subObjects.Length; i++)
            {
                Entity subObject = subObjects[i].m_SubObject;
                if (m_PointOfInterestData.HasComponent(subObject))
                {
                    PrefabRef prefabRef = m_PrefabRefData[subObject];
                    if (m_PrefabMoveableBridgeData.TryGetComponent(prefabRef.m_Prefab, out moveableBridgeData))
                    {
                        return true;
                    }
                }
            }

            moveableBridgeData = default(MoveableBridgeData);
            return false;
        }

        private void ProcessMoveableBridgeLanes(Entity entity, NativeList<LaneGroup> vehicleLanes, NativeList<LaneGroup> pedestrianLanes, NativeList<LaneGroup> groups, DynamicBuffer<SubLane> subLanes, MoveableBridgeData moveableBridgeData, bool isSubNode, ref NativeHashMap<PathNode, int> groupIndexMap, out int groupCount)
        {
            if (groupIndexMap.IsCreated)
            {
                groupIndexMap.Clear();
            }
            else
            {
                groupIndexMap = new NativeHashMap<PathNode, int>(32, Allocator.Temp);
            }

            PrefabRef prefabRef = m_PrefabRefData[entity];
            bool2 x = moveableBridgeData.m_LiftOffsets.z != moveableBridgeData.m_LiftOffsets.xy;
            groupCount = math.select(2, 3, math.all(x));
            int trueValue = math.select(math.select(0, 1, x.x), 2, math.all(x));
            EdgeIterator edgeIterator = new EdgeIterator(Entity.Null, entity, m_ConnectedEdges, m_EdgeData, m_TempData, m_HiddenData);
            EdgeIteratorValue value;
            while (edgeIterator.GetNext(out value))
            {
                if (m_SubLanes.TryGetBuffer(value.m_Edge, out var bufferData))
                {
                    int falseValue = math.select(1, 0, m_PrefabRefData[value.m_Edge].m_Prefab == prefabRef.m_Prefab);
                    falseValue = math.select(falseValue, trueValue, isSubNode);
                    for (int i = 0; i < bufferData.Length; i++)
                    {
                        Lane lane = m_LaneData[bufferData[i].m_SubLane];
                        groupIndexMap.TryAdd(lane.m_StartNode, falseValue);
                        groupIndexMap.TryAdd(lane.m_EndNode, falseValue);
                    }
                }
            }

            for (int j = 0; j < vehicleLanes.Length; j++)
            {
                LaneGroup value2 = vehicleLanes[j];
                if (!groupIndexMap.TryGetValue(m_LaneData[subLanes[value2.m_LaneRange.x].m_SubLane].m_StartNode, out value2.m_GroupIndex))
                {
                    value2.m_GroupIndex = groupCount;
                }

                value2.m_GroupMask = (ushort)(1 << (value2.m_GroupIndex & 0xF));
                groups.Add(in value2);
            }

            for (int k = 0; k < pedestrianLanes.Length; k++)
            {
                LaneGroup value3 = pedestrianLanes[k];
                if (value3.m_IsWaterway)
                {
                    value3.m_GroupMask = (ushort)(~(-1 << groupCount));
                }
                else
                {
                    if (!groupIndexMap.TryGetValue(m_LaneData[subLanes[value3.m_LaneRange.x].m_SubLane].m_StartNode, out value3.m_GroupIndex))
                    {
                        value3.m_GroupIndex = groupCount;
                    }

                    value3.m_GroupMask = (ushort)(1 << (value3.m_GroupIndex & 0xF));
                }

                groups.Add(in value3);
            }
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

                MasterLane componentData2;
                if (m_PedestrianLaneData.TryGetComponent(subLane, out var componentData))
                {
                    Curve curve = m_CurveData[subLane];
                    LaneGroup value = new LaneGroup
                    {
                        m_StartDirection = math.normalizesafe(MathUtils.StartTangent(curve.m_Bezier).xz),
                        m_EndDirection = math.normalizesafe(-MathUtils.EndTangent(curve.m_Bezier).xz),
                        m_LaneRange = new int2(i, i),
                        m_IsUnsafe = ((componentData.m_Flags & PedestrianLaneFlags.Unsafe) != 0),
                        m_IsWaterway = ((componentData.m_Flags & PedestrianLaneFlags.OnWater) != 0),
                        m_IsPedestrian = true
                    };
                    pedestrianLanes.Add(in value);
                }
                else if (m_MasterLaneData.TryGetComponent(subLane, out componentData2))
                {
                    Curve curve2 = m_CurveData[subLane];
                    PrefabRef prefabRef = m_PrefabRefData[subLane];
                    LaneGroup value2 = new LaneGroup
                    {
                        m_StartDirection = math.normalizesafe(MathUtils.StartTangent(curve2.m_Bezier).xz),
                        m_EndDirection = math.normalizesafe(-MathUtils.EndTangent(curve2.m_Bezier).xz),
                        m_LaneRange = new int2(componentData2.m_MinIndex - 1, componentData2.m_MaxIndex)
                    };
                    if (m_CarLaneData.TryGetComponent(subLane, out var componentData3))
                    {
                        value2.m_IsStraight = (componentData3.m_Flags & (CarLaneFlags.UTurnLeft | CarLaneFlags.TurnLeft | CarLaneFlags.TurnRight | CarLaneFlags.UTurnRight | CarLaneFlags.GentleTurnLeft | CarLaneFlags.GentleTurnRight)) == 0;
                        value2.m_IsUnsafe = (componentData3.m_Flags & CarLaneFlags.Unsafe) != 0;
                        if (m_PrefabCarLaneData.TryGetComponent(prefabRef.m_Prefab, out var componentData4))
                        {
                            value2.m_IsWaterway = (componentData4.m_RoadTypes & RoadTypes.Watercraft) != 0;
                        }
                    }
                    else
                    {
                        value2.m_IsStraight = true;
                    }

                    vehicleLanes.Add(in value2);
                }
                else
                {
                    if (m_SlaveLaneData.HasComponent(subLane))
                    {
                        continue;
                    }

                    Curve curve3 = m_CurveData[subLane];
                    PrefabRef prefabRef2 = m_PrefabRefData[subLane];
                    LaneGroup value3 = new LaneGroup
                    {
                        m_StartDirection = math.normalizesafe(MathUtils.StartTangent(curve3.m_Bezier).xz),
                        m_EndDirection = math.normalizesafe(-MathUtils.EndTangent(curve3.m_Bezier).xz),
                        m_LaneRange = new int2(i, i)
                    };
                    if (m_CarLaneData.TryGetComponent(subLane, out var componentData5))
                    {
                        value3.m_IsStraight = (componentData5.m_Flags & (CarLaneFlags.UTurnLeft | CarLaneFlags.TurnLeft | CarLaneFlags.TurnRight | CarLaneFlags.UTurnRight | CarLaneFlags.GentleTurnLeft | CarLaneFlags.GentleTurnRight)) == 0;
                        value3.m_IsUnsafe = (componentData5.m_Flags & CarLaneFlags.Unsafe) != 0;
                        if (m_PrefabCarLaneData.TryGetComponent(prefabRef2.m_Prefab, out var componentData6))
                        {
                            value3.m_IsWaterway = (componentData6.m_RoadTypes & RoadTypes.Watercraft) != 0;
                        }
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

        private void ProcessVehicleLaneGroups(NativeList<LaneGroup> vehicleLanes, NativeList<LaneGroup> groups, bool isLevelCrossing, out int groupCount)
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

            for (int l = 0; l < groups.Length; l++)
            {
                LaneGroup value3 = groups[l];
                value3.m_GroupMask = (ushort)(1 << (value3.m_GroupIndex & 0xF));
                groups[l] = value3;
            }
        }

        private void ProcessPedestrianLaneGroups(DynamicBuffer<SubLane> subLanes, NativeList<LaneGroup> pedestrianLanes, NativeList<LaneGroup> groups, bool isLevelCrossing, ref int groupCount)
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

        private void InitializeTrafficLights(DynamicBuffer<SubLane> subLanes, NativeList<LaneGroup> groups, int groupCount, bool isLevelCrossing, bool isMoveableBridge, ref TrafficLights trafficLights)
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
                sbyte b = (sbyte)math.select(0, -1, isLevelCrossing & ((laneGroup.m_IsTrack && !isMoveableBridge) | (laneGroup.m_IsWaterway & !laneGroup.m_IsPedestrian)));
                for (int j = laneGroup.m_LaneRange.x; j <= laneGroup.m_LaneRange.y; j++)
                {
                    Entity subLane = subLanes[j].m_SubLane;
                    LaneSignal laneSignal = m_LaneSignalData[subLane];
                    laneSignal.m_GroupMask = laneGroup.m_GroupMask;
                    laneSignal.m_Default = b;
                    if (!isLevelCrossing && m_CarLaneData.HasComponent(subLane))
                    {
                        laneSignal.m_Flags |= LaneSignalFlags.CanExtend;
                    }

                    if (isMoveableBridge)
                    {
                        laneSignal.m_Flags |= LaneSignalFlags.Physical;
                    }

                    Simulation.PatchedTrafficLightSystem.UpdateLaneSignal(trafficLights, ref laneSignal);
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
        public EntityTypeHandle __Unity_Entities_Entity_TypeHandle;

        [ReadOnly]
        public ComponentTypeHandle<Owner> __Game_Common_Owner_RO_ComponentTypeHandle;

        [ReadOnly]
        public BufferTypeHandle<SubLane> __Game_Net_SubLane_RO_BufferTypeHandle;

        [ReadOnly]
        public BufferTypeHandle<Game.Objects.SubObject> __Game_Objects_SubObject_RO_BufferTypeHandle;

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
        public ComponentLookup<Edge> __Game_Net_Edge_RO_ComponentLookup;

        [ReadOnly]
        public ComponentLookup<Node> __Game_Net_Node_RO_ComponentLookup;

        [ReadOnly]
        public ComponentLookup<Lane> __Game_Net_Lane_RO_ComponentLookup;

        [ReadOnly]
        public ComponentLookup<PointOfInterest> __Game_Common_PointOfInterest_RO_ComponentLookup;

        [ReadOnly]
        public ComponentLookup<Temp> __Game_Tools_Temp_RO_ComponentLookup;

        [ReadOnly]
        public ComponentLookup<Hidden> __Game_Tools_Hidden_RO_ComponentLookup;

        [ReadOnly]
        public ComponentLookup<PrefabRef> __Game_Prefabs_PrefabRef_RO_ComponentLookup;

        [ReadOnly]
        public ComponentLookup<CarLaneData> __Game_Prefabs_CarLaneData_RO_ComponentLookup;

        [ReadOnly]
        public ComponentLookup<MoveableBridgeData> __Game_Prefabs_MoveableBridgeData_RO_ComponentLookup;

        [ReadOnly]
        public BufferLookup<SubLane> __Game_Net_SubLane_RO_BufferLookup;

        [ReadOnly]
        public BufferLookup<LaneOverlap> __Game_Net_LaneOverlap_RO_BufferLookup;

        [ReadOnly]
        public BufferLookup<ConnectedEdge> __Game_Net_ConnectedEdge_RO_BufferLookup;

        [ReadOnly]
        public BufferLookup<Game.Objects.SubObject> __Game_Objects_SubObject_RO_BufferLookup;

        public ComponentLookup<LaneSignal> __Game_Net_LaneSignal_RW_ComponentLookup;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void __AssignHandles(ref SystemState state)
        {
            __Unity_Entities_Entity_TypeHandle = state.GetEntityTypeHandle();
            __Game_Common_Owner_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Owner>(isReadOnly: true);
            __Game_Net_SubLane_RO_BufferTypeHandle = state.GetBufferTypeHandle<SubLane>(isReadOnly: true);
            __Game_Objects_SubObject_RO_BufferTypeHandle = state.GetBufferTypeHandle<Game.Objects.SubObject>(isReadOnly: true);
            __Game_Net_TrafficLights_RW_ComponentTypeHandle = state.GetComponentTypeHandle<TrafficLights>();
            __Game_Net_MasterLane_RO_ComponentLookup = state.GetComponentLookup<MasterLane>(isReadOnly: true);
            __Game_Net_SlaveLane_RO_ComponentLookup = state.GetComponentLookup<SlaveLane>(isReadOnly: true);
            __Game_Net_CarLane_RO_ComponentLookup = state.GetComponentLookup<CarLane>(isReadOnly: true);
            __Game_Net_PedestrianLane_RO_ComponentLookup = state.GetComponentLookup<PedestrianLane>(isReadOnly: true);
            __Game_Net_SecondaryLane_RO_ComponentLookup = state.GetComponentLookup<SecondaryLane>(isReadOnly: true);
            __Game_Net_Curve_RO_ComponentLookup = state.GetComponentLookup<Curve>(isReadOnly: true);
            __Game_Net_Edge_RO_ComponentLookup = state.GetComponentLookup<Edge>(isReadOnly: true);
            __Game_Net_Node_RO_ComponentLookup = state.GetComponentLookup<Node>(isReadOnly: true);
            __Game_Net_Lane_RO_ComponentLookup = state.GetComponentLookup<Lane>(isReadOnly: true);
            __Game_Common_PointOfInterest_RO_ComponentLookup = state.GetComponentLookup<PointOfInterest>(isReadOnly: true);
            __Game_Tools_Temp_RO_ComponentLookup = state.GetComponentLookup<Temp>(isReadOnly: true);
            __Game_Tools_Hidden_RO_ComponentLookup = state.GetComponentLookup<Hidden>(isReadOnly: true);
            __Game_Prefabs_PrefabRef_RO_ComponentLookup = state.GetComponentLookup<PrefabRef>(isReadOnly: true);
            __Game_Prefabs_CarLaneData_RO_ComponentLookup = state.GetComponentLookup<CarLaneData>(isReadOnly: true);
            __Game_Prefabs_MoveableBridgeData_RO_ComponentLookup = state.GetComponentLookup<MoveableBridgeData>(isReadOnly: true);
            __Game_Net_SubLane_RO_BufferLookup = state.GetBufferLookup<SubLane>(isReadOnly: true);
            __Game_Net_LaneOverlap_RO_BufferLookup = state.GetBufferLookup<LaneOverlap>(isReadOnly: true);
            __Game_Net_ConnectedEdge_RO_BufferLookup = state.GetBufferLookup<ConnectedEdge>(isReadOnly: true);
            __Game_Objects_SubObject_RO_BufferLookup = state.GetBufferLookup<Game.Objects.SubObject>(isReadOnly: true);
            __Game_Net_LaneSignal_RW_ComponentLookup = state.GetComponentLookup<LaneSignal>();
        }
    }

    private EntityQuery m_TrafficLightsQuery;

    private TypeHandle __TypeHandle;

    private ExtraTypeHandle m_ExtraTypeHandle;

    private Game.City.CityConfigurationSystem m_CityConfigurationSystem;

    private ModificationBarrier4B m_ModificationBarrier;

    [Preserve]
    protected override void OnCreate()
    {
        base.OnCreate();
        m_CityConfigurationSystem = World.GetOrCreateSystemManaged<Game.City.CityConfigurationSystem>();
        m_ModificationBarrier = World.GetOrCreateSystemManaged<ModificationBarrier4B>();
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
        m_ExtraTypeHandle.Update(ref base.CheckedStateRef);
        JobHandle dependency = JobChunkExtensions.ScheduleParallel(new InitializeTrafficLightsJob
        {
            m_EntityType = InternalCompilerInterface.GetEntityTypeHandle(ref __TypeHandle.__Unity_Entities_Entity_TypeHandle, ref base.CheckedStateRef),
            m_OwnerType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Common_Owner_RO_ComponentTypeHandle, ref base.CheckedStateRef),
            m_SubLaneType = InternalCompilerInterface.GetBufferTypeHandle(ref __TypeHandle.__Game_Net_SubLane_RO_BufferTypeHandle, ref base.CheckedStateRef),
            m_SubObjectType = InternalCompilerInterface.GetBufferTypeHandle(ref __TypeHandle.__Game_Objects_SubObject_RO_BufferTypeHandle, ref base.CheckedStateRef),
            m_TrafficLightsType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Net_TrafficLights_RW_ComponentTypeHandle, ref base.CheckedStateRef),
            m_MasterLaneData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Net_MasterLane_RO_ComponentLookup, ref base.CheckedStateRef),
            m_SlaveLaneData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Net_SlaveLane_RO_ComponentLookup, ref base.CheckedStateRef),
            m_CarLaneData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Net_CarLane_RO_ComponentLookup, ref base.CheckedStateRef),
            m_PedestrianLaneData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Net_PedestrianLane_RO_ComponentLookup, ref base.CheckedStateRef),
            m_SecondaryLaneData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Net_SecondaryLane_RO_ComponentLookup, ref base.CheckedStateRef),
            m_CurveData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Net_Curve_RO_ComponentLookup, ref base.CheckedStateRef),
            m_EdgeData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Net_Edge_RO_ComponentLookup, ref base.CheckedStateRef),
            m_NodeData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Net_Node_RO_ComponentLookup, ref base.CheckedStateRef),
            m_LaneData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Net_Lane_RO_ComponentLookup, ref base.CheckedStateRef),
            m_PointOfInterestData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Common_PointOfInterest_RO_ComponentLookup, ref base.CheckedStateRef),
            m_TempData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Tools_Temp_RO_ComponentLookup, ref base.CheckedStateRef),
            m_HiddenData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Tools_Hidden_RO_ComponentLookup, ref base.CheckedStateRef),
            m_PrefabRefData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_PrefabRef_RO_ComponentLookup, ref base.CheckedStateRef),
            m_PrefabCarLaneData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_CarLaneData_RO_ComponentLookup, ref base.CheckedStateRef),
            m_PrefabMoveableBridgeData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_MoveableBridgeData_RO_ComponentLookup, ref base.CheckedStateRef),
            m_SubLanes = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Net_SubLane_RO_BufferLookup, ref base.CheckedStateRef),
            m_Overlaps = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Net_LaneOverlap_RO_BufferLookup, ref base.CheckedStateRef),
            m_ConnectedEdges = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Net_ConnectedEdge_RO_BufferLookup, ref base.CheckedStateRef),
            m_SubObjects = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Objects_SubObject_RO_BufferLookup, ref base.CheckedStateRef),
            m_LaneSignalData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Net_LaneSignal_RW_ComponentLookup, ref base.CheckedStateRef),
            m_LeftHandTraffic = m_CityConfigurationSystem.leftHandTraffic,
            m_ExtraTypeHandle = m_ExtraTypeHandle,
            m_Settings = new Settings.Values(Mod.m_Settings),
            m_CommandBuffer = m_ModificationBarrier.CreateCommandBuffer().AsParallelWriter()
        }, m_TrafficLightsQuery, base.Dependency);
        m_ModificationBarrier.AddJobHandleForProducer(dependency);
        base.Dependency = dependency;
    }

    public void SetCompatibilityMode(bool enable)
    {
        if (enable)
        {
            m_TrafficLightsQuery = GetEntityQuery(new EntityQueryDesc
            {
                All = new ComponentType[2]
                {
                    ComponentType.ReadOnly<TrafficLights>(),
                    ComponentType.ReadWrite<CustomTrafficLights>()
                },
                Any = new ComponentType[1] { ComponentType.ReadOnly<Updated>() }
            });
        }
        else
        {
            m_TrafficLightsQuery = GetEntityQuery(new EntityQueryDesc
            {
                All = new ComponentType[1] { ComponentType.ReadOnly<TrafficLights>() },
                Any = new ComponentType[1] { ComponentType.ReadOnly<Updated>() }
            });
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
    public PatchedTrafficLightInitializationSystem()
    {
    }
}