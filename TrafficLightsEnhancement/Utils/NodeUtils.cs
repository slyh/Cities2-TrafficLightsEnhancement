using C2VM.TrafficLightsEnhancement.Components;
using Game.Net;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using static C2VM.TrafficLightsEnhancement.Systems.TrafficLightSystems.Initialisation.PatchedTrafficLightInitializationSystem;

namespace C2VM.TrafficLightsEnhancement.Utils;

public partial struct NodeUtils
{
    public struct LaneConnection
    {
        public Entity m_SourceSubLane;

        public Entity m_SourceEdge;

        public Entity m_DestSubLane;

        public Entity m_DestEdge;

        public LaneConnection()
        {
            m_SourceSubLane = Entity.Null;
            m_SourceEdge = Entity.Null;
            m_DestSubLane = Entity.Null;
            m_DestEdge = Entity.Null;
        }
    }

    public static NativeList<EdgeInfo> GetEdgeInfoList
    (
        Allocator allocator,
        Entity nodeEntity,
        DynamicBuffer<SubLane> nodeSubLaneBuffer,
        DynamicBuffer<ConnectedEdge> connectedEdgeBuffer,
        DynamicBuffer<EdgeGroupMask> edgeGroupMaskBuffer,
        DynamicBuffer<SubLaneGroupMask> subLaneGroupMaskBuffer,
        BufferLookup<SubLane> subLaneLookup,
        BufferLookup<LaneOverlap> laneOverlapLookup,
        ComponentLookup<Edge> edgeLookup,
        ComponentLookup<EdgeGeometry> edgeGeometryLookup,
        ComponentLookup<Lane> laneLookup,
        ComponentLookup<PedestrianLane> pedestrianLaneLookup,
        ComponentLookup<MasterLane> masterLaneLookup,
        ComponentLookup<TrackLane> trackLaneLookup,
        ComponentLookup<CarLane> carLaneLookup,
        ComponentLookup<Curve> curveLookup,
        ComponentLookup<TrainTrack> trainTrackLookup
    )
    {
        NativeList<EdgeInfo> edgeInfoList = new(4, allocator);
        NativeHashMap<Entity, LaneConnection> laneConnectionMap = GetLaneConnectionMap(Allocator.Temp, nodeSubLaneBuffer, connectedEdgeBuffer, subLaneLookup, laneLookup);

        foreach (ConnectedEdge connectedEdge in connectedEdgeBuffer)
        {
            EdgeInfo edgeInfo = default;
            Entity edgeEntity = connectedEdge.m_Edge;
            float3 edgePosition = GetEdgePosition(nodeEntity, edgeEntity, edgeLookup, edgeGeometryLookup);
            edgeInfo.m_Edge = edgeEntity;
            edgeInfo.m_Position = edgePosition;
            CustomPhaseUtils.TryGet(edgeGroupMaskBuffer, edgeEntity, edgePosition, out edgeInfo.m_EdgeGroupMask);

            NativeHashMap<Entity, SubLaneInfo> subLaneMap = new(16, Allocator.Temp);
            NativeList<SubLaneInfo> subLaneInfoList = new(16, allocator);

            foreach (SubLane nodeSubLane in nodeSubLaneBuffer)
            {
                pedestrianLaneLookup.TryGetComponent(nodeSubLane.m_SubLane, out var nodePedestrianLane);
                LaneConnection laneConnection = GetLaneConnectionFromNodeSubLane(nodeSubLane.m_SubLane, laneConnectionMap, (nodePedestrianLane.m_Flags & PedestrianLaneFlags.Crosswalk) != 0);
                if (laneConnection.m_SourceEdge == edgeEntity)
                {
                    if (!masterLaneLookup.HasComponent(nodeSubLane.m_SubLane))
                    {
                        SubLaneInfo sourceSubLaneInfo = subLaneMap[laneConnection.m_SourceSubLane];
                        sourceSubLaneInfo.m_SubLane = laneConnection.m_SourceSubLane;
                        sourceSubLaneInfo.m_Position = GetSubLanePosition(sourceSubLaneInfo.m_SubLane, curveLookup);
                        if (trackLaneLookup.TryGetComponent(nodeSubLane.m_SubLane, out var trackLane))
                        {
                            if ((trackLane.m_Flags & TrackLaneFlags.TurnLeft) != 0)
                            {
                                edgeInfo.m_TrackLaneLeftCount++;
                                sourceSubLaneInfo.m_TrackLaneLeftCount++;
                            }
                            else if ((trackLane.m_Flags & TrackLaneFlags.TurnRight) != 0)
                            {
                                edgeInfo.m_TrackLaneRightCount++;
                                sourceSubLaneInfo.m_TrackLaneRightCount++;
                            }
                            else
                            {
                                edgeInfo.m_TrackLaneStraightCount++;
                                sourceSubLaneInfo.m_TrackLaneStraightCount++;
                            }
                            subLaneMap[laneConnection.m_SourceSubLane] = sourceSubLaneInfo;
                        }
                        if (carLaneLookup.TryGetComponent(nodeSubLane.m_SubLane, out var nodeCarLane))
                        {
                            carLaneLookup.TryGetComponent(laneConnection.m_SourceSubLane, out var edgeCarLane);
                            bool isPublicOnly = (edgeCarLane.m_Flags & CarLaneFlags.PublicOnly) != 0;
                            bool isUTurn = (nodeCarLane.m_Flags & (CarLaneFlags.UTurnLeft | CarLaneFlags.UTurnRight)) != 0;
                            if (!isUTurn && (nodeCarLane.m_Flags & (CarLaneFlags.TurnLeft | CarLaneFlags.GentleTurnLeft)) != 0)
                            {
                                edgeInfo.m_PublicCarLaneLeftCount += System.Convert.ToInt32(isPublicOnly);
                                edgeInfo.m_CarLaneLeftCount += System.Convert.ToInt32(!isPublicOnly);
                                sourceSubLaneInfo.m_CarLaneLeftCount++;
                            }
                            else if (!isUTurn && (nodeCarLane.m_Flags & (CarLaneFlags.TurnRight | CarLaneFlags.GentleTurnRight)) != 0)
                            {
                                edgeInfo.m_PublicCarLaneRightCount += System.Convert.ToInt32(isPublicOnly);
                                edgeInfo.m_CarLaneRightCount += System.Convert.ToInt32(!isPublicOnly);
                                sourceSubLaneInfo.m_CarLaneRightCount++;
                            }
                            else if (!isUTurn)
                            {
                                edgeInfo.m_PublicCarLaneStraightCount += System.Convert.ToInt32(isPublicOnly);
                                edgeInfo.m_CarLaneStraightCount += System.Convert.ToInt32(!isPublicOnly);
                                sourceSubLaneInfo.m_CarLaneStraightCount++;
                            }
                            else if (isUTurn)
                            {
                                edgeInfo.m_PublicCarLaneUTurnCount += System.Convert.ToInt32(isPublicOnly);
                                edgeInfo.m_CarLaneUTurnCount += System.Convert.ToInt32(!isPublicOnly);
                                sourceSubLaneInfo.m_CarLaneUTurnCount++;
                            }
                            subLaneMap[laneConnection.m_SourceSubLane] = sourceSubLaneInfo;
                        }
                    }
                }
                if ((nodePedestrianLane.m_Flags & PedestrianLaneFlags.Crosswalk) != 0 && (nodePedestrianLane.m_Flags & PedestrianLaneFlags.Unsafe) == 0)
                {
                    if (laneConnection.m_SourceEdge == edgeEntity || laneConnection.m_DestEdge == edgeEntity)
                    {
                        if (IsCrossingStopLine(nodeSubLane.m_SubLane, edgeEntity, laneLookup, laneOverlapLookup, subLaneLookup))
                        {
                            edgeInfo.m_PedestrianLaneStopLineCount++;
                        }
                        else
                        {
                            edgeInfo.m_PedestrianLaneNonStopLineCount++;
                        }
                        SubLaneInfo subLaneInfo = subLaneMap[nodeSubLane.m_SubLane];
                        subLaneInfo.m_SubLane = nodeSubLane.m_SubLane;
                        subLaneInfo.m_Position = GetSubLanePosition(subLaneInfo.m_SubLane, curveLookup);
                        subLaneInfo.m_PedestrianLaneCount++;
                        subLaneMap[nodeSubLane.m_SubLane] = subLaneInfo;
                    }
                }
            }

            if (trainTrackLookup.HasComponent(edgeInfo.m_Edge))
            {
                edgeInfo.m_TrainTrackCount++;
            }

            foreach (var kV in subLaneMap)
            {
                var subLaneInfo = kV.Value;
                CustomPhaseUtils.TryGet(subLaneGroupMaskBuffer, subLaneInfo.m_SubLane, subLaneInfo.m_Position, out subLaneInfo.m_SubLaneGroupMask);
                subLaneInfoList.Add(subLaneInfo);
            }
            edgeInfo.m_SubLaneInfoList = subLaneInfoList.AsArray();
            edgeInfoList.Add(edgeInfo);
        }

        return edgeInfoList;
    }

    public static NativeList<EdgeInfo> GetEdgeInfoList(Allocator allocator, Entity nodeEntity, Systems.UI.UISystem uISystem)
    {
        uISystem.m_TypeHandle.Update(uISystem);
        uISystem.m_TypeHandle.m_SubLane.TryGetBuffer(nodeEntity, out var nodeSubLaneBuffer);
        uISystem.m_TypeHandle.m_ConnectedEdge.TryGetBuffer(nodeEntity, out var connectedEdgeBuffer);
        uISystem.m_TypeHandle.m_EdgeGroupMask.TryGetBuffer(nodeEntity, out var edgeGroupMaskBuffer);
        uISystem.m_TypeHandle.m_SubLaneGroupMask.TryGetBuffer(nodeEntity, out var subLaneGroupMaskBuffer);
        return GetEdgeInfoList
        (
            allocator,
            nodeEntity,
            nodeSubLaneBuffer,
            connectedEdgeBuffer,
            edgeGroupMaskBuffer,
            subLaneGroupMaskBuffer,
            uISystem.m_TypeHandle.m_SubLane,
            uISystem.m_TypeHandle.m_LaneOverlap,
            uISystem.m_TypeHandle.m_Edge,
            uISystem.m_TypeHandle.m_EdgeGeometry,
            uISystem.m_TypeHandle.m_Lane,
            uISystem.m_TypeHandle.m_PedestrianLane,
            uISystem.m_TypeHandle.m_MasterLane,
            uISystem.m_TypeHandle.m_TrackLane,
            uISystem.m_TypeHandle.m_CarLane,
            uISystem.m_TypeHandle.m_Curve,
            uISystem.m_TypeHandle.m_TrainTrack
        );
    }

    public static NativeList<EdgeInfo> GetEdgeInfoList(Allocator allocator, Entity nodeEntity, ref InitializeTrafficLightsJob job, DynamicBuffer<SubLane> nodeSubLaneBuffer, DynamicBuffer<ConnectedEdge> connectedEdgeBuffer, DynamicBuffer<EdgeGroupMask> edgeGroupMaskBuffer, DynamicBuffer<SubLaneGroupMask> subLaneGroupMaskBuffer)
    {
        return GetEdgeInfoList
        (
            allocator,
            nodeEntity,
            nodeSubLaneBuffer,
            connectedEdgeBuffer,
            edgeGroupMaskBuffer,
            subLaneGroupMaskBuffer,
            job.m_ExtraTypeHandle.m_SubLane,
            job.m_Overlaps,
            job.m_ExtraTypeHandle.m_Edge,
            job.m_ExtraTypeHandle.m_EdgeGeometry,
            job.m_ExtraTypeHandle.m_Lane,
            job.m_ExtraTypeHandle.m_PedestrianLane,
            job.m_MasterLaneData,
            job.m_ExtraTypeHandle.m_TrackLane,
            job.m_CarLaneData,
            job.m_CurveData,
            job.m_ExtraTypeHandle.m_TrainTrack
        );
    }

    public static void Dispose(NativeArray<EdgeInfo> edgeInfoList)
    {
        foreach (var edgeInfo in edgeInfoList)
        {
            edgeInfo.m_SubLaneInfoList.Dispose();
        }
        edgeInfoList.Dispose();
    }

    public static NativeHashMap<Entity, LaneConnection> GetLaneConnectionMap(Allocator allocator, DynamicBuffer<SubLane> nodeSubLaneBuffer, DynamicBuffer<ConnectedEdge> connectedEdgeBuffer, BufferLookup<SubLane> subLaneLookup, ComponentLookup<Lane> laneLookup)
    {
        NativeHashMap<Entity, LaneConnection> laneConnectionMap = new NativeHashMap<Entity, LaneConnection>(16, allocator);
        foreach (SubLane nodeSubLane in nodeSubLaneBuffer)
        {
            LaneConnection laneConnection = new LaneConnection();
            if (laneLookup.TryGetComponent(nodeSubLane.m_SubLane, out Lane nodeLane))
            {
                foreach (ConnectedEdge connectedEdge in connectedEdgeBuffer)
                {
                    subLaneLookup.TryGetBuffer(connectedEdge.m_Edge, out DynamicBuffer<SubLane> edgeSubLaneBuffer);
                    foreach (SubLane edgeSubLane in edgeSubLaneBuffer)
                    {
                        if (laneLookup.TryGetComponent(edgeSubLane.m_SubLane, out Lane edgeLane))
                        {
                            if (nodeLane.m_StartNode.Equals(edgeLane.m_EndNode))
                            {
                                laneConnection.m_SourceEdge = connectedEdge.m_Edge;
                                laneConnection.m_SourceSubLane = edgeSubLane.m_SubLane;
                            }
                            else if (nodeLane.m_StartNode.Equals(edgeLane.m_StartNode))
                            {
                                laneConnection.m_SourceEdge = connectedEdge.m_Edge;
                                laneConnection.m_SourceSubLane = edgeSubLane.m_SubLane;
                            }
                            if (nodeLane.m_EndNode.Equals(edgeLane.m_StartNode))
                            {
                                laneConnection.m_DestEdge = connectedEdge.m_Edge;
                                laneConnection.m_DestSubLane = edgeSubLane.m_SubLane;
                            }
                            else if (nodeLane.m_EndNode.Equals(edgeLane.m_EndNode))
                            {
                                laneConnection.m_DestEdge = connectedEdge.m_Edge;
                                laneConnection.m_DestSubLane = edgeSubLane.m_SubLane;
                            }
                        }
                    }
                }
                foreach (SubLane nodeSubLane2 in nodeSubLaneBuffer)
                {
                    if (laneConnection.m_SourceSubLane != Entity.Null && laneConnection.m_DestSubLane != Entity.Null)
                    {
                        break;
                    }
                    if (nodeSubLane2.m_SubLane == nodeSubLane.m_SubLane)
                    {
                        continue;
                    }
                    if (laneLookup.TryGetComponent(nodeSubLane2.m_SubLane, out Lane nodeLane2))
                    {
                        if (nodeLane.m_StartNode.Equals(nodeLane2.m_EndNode) && laneConnection.m_SourceSubLane == Entity.Null)
                        {
                            laneConnection.m_SourceSubLane = nodeSubLane2.m_SubLane;
                        }
                        if (nodeLane.m_EndNode.Equals(nodeLane2.m_StartNode) && laneConnection.m_DestSubLane == Entity.Null)
                        {
                            laneConnection.m_DestSubLane = nodeSubLane2.m_SubLane;
                        }
                    }
                }
            }
            laneConnectionMap[nodeSubLane.m_SubLane] = laneConnection;
        }
        return laneConnectionMap;
    }

    public static float3 GetEdgePosition(Entity nodeEntity, Entity edgeEntity, ComponentLookup<Edge> edgeLookup, ComponentLookup<EdgeGeometry> edgeGeometryLookup)
    {
        float3 position = default;
        edgeLookup.TryGetComponent(edgeEntity, out Edge edge);
        edgeGeometryLookup.TryGetComponent(edgeEntity, out EdgeGeometry edgeGeometry);
        if (edge.m_Start.Equals(nodeEntity))
        {
            position = (edgeGeometry.m_Start.m_Left.a + edgeGeometry.m_Start.m_Right.a) / 2;
        }
        else if (edge.m_End.Equals(nodeEntity))
        {
            position = (edgeGeometry.m_End.m_Left.d + edgeGeometry.m_End.m_Right.d) / 2;
        }
        return position;
    }

    public static float3 GetEdgePosition(ref InitializeTrafficLightsJob job, Entity nodeEntity, Entity edgeEntity)
    {
        return GetEdgePosition(nodeEntity, edgeEntity, job.m_ExtraTypeHandle.m_Edge, job.m_ExtraTypeHandle.m_EdgeGeometry);
    }

    public static float3 GetSubLanePosition(Entity subLane, ComponentLookup<Curve> curveLookup)
    {
        curveLookup.TryGetComponent(subLane, out Curve curve);
        return curve.m_Bezier.d;
    }

    public static LaneConnection GetLaneConnectionFromNodeSubLane(Entity nodeSubLaneEntity, NativeHashMap<Entity, LaneConnection> laneConnectionMap, bool recursive)
    {
        if (nodeSubLaneEntity == Entity.Null || !laneConnectionMap.ContainsKey(nodeSubLaneEntity))
        {
            return new LaneConnection();
        }
        LaneConnection laneConnection = laneConnectionMap[nodeSubLaneEntity];
        if (recursive)
        {
            if (laneConnection.m_SourceSubLane != Entity.Null)
            {
                int depth = 0;
                while (laneConnection.m_SourceEdge == Entity.Null)
                {
                    var nextLaneConnection = laneConnectionMap[laneConnection.m_SourceSubLane];
                    if (nextLaneConnection.m_SourceSubLane != Entity.Null)
                    {
                        laneConnection.m_SourceEdge = nextLaneConnection.m_SourceEdge;
                        laneConnection.m_SourceSubLane = nextLaneConnection.m_SourceSubLane;
                    }
                    else
                    {
                        break;
                    }
                    if (depth++ > 64)
                    {
                        break;
                    }
                }
            }
            if (laneConnection.m_DestSubLane != Entity.Null)
            {
                int depth = 0;
                while (laneConnection.m_DestEdge == Entity.Null)
                {
                    var nextLaneConnection = laneConnectionMap[laneConnection.m_DestSubLane];
                    if (nextLaneConnection.m_DestSubLane != Entity.Null)
                    {
                        laneConnection.m_DestEdge = nextLaneConnection.m_DestEdge;
                        laneConnection.m_DestSubLane = nextLaneConnection.m_DestSubLane;
                    }
                    else
                    {
                        break;
                    }
                    if (depth++ > 64)
                    {
                        break;
                    }
                }
            }
        }
        return laneConnection;
    }

    public static bool IsCrossingStopLine(Entity nodeSubLaneEntity, Entity edgeEntity, ComponentLookup<Lane> laneLookup, BufferLookup<LaneOverlap> laneOverlapLookup, BufferLookup<SubLane> subLaneLookup)
    {
        if (laneOverlapLookup.TryGetBuffer(nodeSubLaneEntity, out DynamicBuffer<LaneOverlap> laneOverlapBuffer))
        {
            if (!subLaneLookup.TryGetBuffer(edgeEntity, out DynamicBuffer<SubLane> edgeSubLaneBuffer))
            {
                return false;
            }
            foreach (LaneOverlap laneOverlap in laneOverlapBuffer)
            {
                bool isStart = false;
                bool isEnd = false;
                foreach (SubLane edgeSubLane in edgeSubLaneBuffer)
                {
                    if (laneLookup.TryGetComponent(edgeSubLane.m_SubLane, out Lane edgeLane) && laneLookup.TryGetComponent(laneOverlap.m_Other, out Lane overlapLane))
                    {
                        if (overlapLane.m_StartNode.Equals(edgeLane.m_EndNode))
                        {
                            isStart = true;
                        }
                        else if (overlapLane.m_EndNode.Equals(edgeLane.m_StartNode))
                        {
                            isEnd = true;
                        }
                    }
                }
                if (isStart && !isEnd)
                {
                    return true;
                }
            }
        }
        return false;
    }

    public static bool IsCrossingStopLine(ref InitializeTrafficLightsJob job, Entity nodeSubLaneEntity, Entity edgeEntity)
    {
        return IsCrossingStopLine(nodeSubLaneEntity, edgeEntity, job.m_ExtraTypeHandle.m_Lane, job.m_Overlaps, job.m_ExtraTypeHandle.m_SubLane);
    }

    public static bool HasTrainTrack(NativeArray<EdgeInfo> edgeInfoArray)
    {
        foreach (var edgeInfo in edgeInfoArray)
        {
            if (edgeInfo.m_TrainTrackCount > 0)
            {
                return true;
            }
        }
        return false;
    }
}