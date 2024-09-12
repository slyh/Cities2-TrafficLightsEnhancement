using C2VM.TrafficLightsEnhancement.Components;
using Colossal.Entities;
using Game.Net;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using static C2VM.TrafficLightsEnhancement.Systems.TrafficLightInitializationSystem.PatchedTrafficLightInitializationSystem;
using static C2VM.TrafficLightsEnhancement.Systems.UISystem.Types;

namespace C2VM.TrafficLightsEnhancement.Utils;

public struct NodeUtils
{
    public struct EdgeInfo
    {
        public Entity m_Edge;

        public uint m_Group;

        public WorldPosition m_Position;

        public int m_CarLaneLeftCount;

        public int m_CarLaneStraightCount;

        public int m_CarLaneRightCount;

        public int m_CarLaneUTurnCount;

        public int m_PublicCarLaneLeftCount;

        public int m_PublicCarLaneStraightCount;

        public int m_PublicCarLaneRightCount;

        public int m_PublicCarLaneUTurnCount;

        public int m_TrackLaneLeftCount;

        public int m_TrackLaneStraightCount;

        public int m_TrackLaneRightCount;

        public int m_PedestrianLaneStopLineCount;

        public int m_PedestrianLaneNonStopLineCount;

        public CustomPhaseGroupMask m_CustomPhaseGroupMask;
    }

    public struct LaneSource
    {
        public Entity m_SubLane;

        public Entity m_Edge;

        public LaneSource()
        {
            m_SubLane = Entity.Null;
            m_Edge = Entity.Null;
        }
    }

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

    public static NativeList<EdgeInfo> GetEdgeInfoList(Allocator allocator, EntityManager em, Entity nodeEntity)
    {
        NativeList<EdgeInfo> edgeInfoList = new NativeList<EdgeInfo>(0, allocator);
        em.TryGetBuffer(nodeEntity, true, out DynamicBuffer<SubLane> nodeSubLaneBuffer);
        em.TryGetBuffer(nodeEntity, true, out DynamicBuffer<ConnectedEdge> connectedEdgeBuffer);
        if (!em.TryGetBuffer(nodeEntity, true, out DynamicBuffer<CustomPhaseGroupMask> customPhaseGroupMaskBuffer))
        {
            customPhaseGroupMaskBuffer = em.AddBuffer<CustomPhaseGroupMask>(nodeEntity);
        }

        NativeHashMap<Entity, LaneConnection> laneConnectionMap = GetLaneConnectionMap(Allocator.Temp, em, nodeEntity);
        bool isLevelCrossing = false;

        foreach (SubLane nodeSubLane in nodeSubLaneBuffer)
        {
            if (em.TryGetComponent<TrackLane>(nodeSubLane.m_SubLane, out var trackLane) && (trackLane.m_Flags & TrackLaneFlags.LevelCrossing) != 0)
            {
                isLevelCrossing = true;
                break;
            }
        }

        foreach (ConnectedEdge connectedEdge in connectedEdgeBuffer)
        {
            EdgeInfo edgeInfo = default;
            Entity edgeEntity = connectedEdge.m_Edge;
            float3 edgePosition = GetEdgePosition(em, nodeEntity, edgeEntity);
            edgeInfo.m_Edge = edgeEntity;
            edgeInfo.m_Group = 0;
            edgeInfo.m_Position = edgePosition;
            if (CustomPhaseUtils.TryGet(customPhaseGroupMaskBuffer, edgeEntity, edgePosition, 0, out edgeInfo.m_CustomPhaseGroupMask) < 0)
            {
                customPhaseGroupMaskBuffer.Add(edgeInfo.m_CustomPhaseGroupMask);
            }

            foreach (SubLane nodeSubLane in nodeSubLaneBuffer)
            {
                em.TryGetComponent<PedestrianLane>(nodeSubLane.m_SubLane, out var nodePedestrianLane);
                LaneConnection laneConnection = GetLaneConnectionFromNodeSubLane(nodeSubLane.m_SubLane, laneConnectionMap, (nodePedestrianLane.m_Flags & PedestrianLaneFlags.Crosswalk) != 0);
                if (laneConnection.m_SourceEdge == edgeEntity)
                {
                    if (em.TryGetComponent<TrackLane>(nodeSubLane.m_SubLane, out var trackLane))
                    {
                        if ((trackLane.m_Flags & TrackLaneFlags.TurnLeft) != 0)
                        {
                            edgeInfo.m_TrackLaneLeftCount++;
                        }
                        else if ((trackLane.m_Flags & TrackLaneFlags.TurnRight) != 0)
                        {
                            edgeInfo.m_TrackLaneRightCount++;
                        }
                        else
                        {
                            edgeInfo.m_TrackLaneStraightCount++;
                        }
                    }
                    if (em.TryGetComponent<CarLane>(nodeSubLane.m_SubLane, out var nodeCarLane))
                    {
                        em.TryGetComponent<CarLane>(laneConnection.m_SourceSubLane, out var edgeCarLane);
                        bool isPublicOnly = (edgeCarLane.m_Flags & CarLaneFlags.PublicOnly) != 0;
                        bool isUTurn = (nodeCarLane.m_Flags & (CarLaneFlags.UTurnLeft | CarLaneFlags.UTurnRight)) != 0;
                        if (!isUTurn && (nodeCarLane.m_Flags & (CarLaneFlags.TurnLeft | CarLaneFlags.GentleTurnLeft)) != 0)
                        {
                            edgeInfo.m_PublicCarLaneLeftCount += System.Convert.ToInt32(isPublicOnly);
                            edgeInfo.m_CarLaneLeftCount += System.Convert.ToInt32(!isPublicOnly);
                        }
                        else if (!isUTurn && (nodeCarLane.m_Flags & (CarLaneFlags.TurnRight | CarLaneFlags.GentleTurnRight)) != 0)
                        {
                            edgeInfo.m_PublicCarLaneRightCount += System.Convert.ToInt32(isPublicOnly);
                            edgeInfo.m_CarLaneRightCount += System.Convert.ToInt32(!isPublicOnly);
                        }
                        else if (!isUTurn)
                        {
                            edgeInfo.m_PublicCarLaneStraightCount += System.Convert.ToInt32(isPublicOnly);
                            edgeInfo.m_CarLaneStraightCount += System.Convert.ToInt32(!isPublicOnly);
                        }
                        else if (isUTurn)
                        {
                            edgeInfo.m_PublicCarLaneUTurnCount += System.Convert.ToInt32(isPublicOnly);
                            edgeInfo.m_CarLaneUTurnCount += System.Convert.ToInt32(!isPublicOnly);
                        }
                    }
                }
                if (!isLevelCrossing)
                {
                    if ((nodePedestrianLane.m_Flags & PedestrianLaneFlags.Crosswalk) != 0 && (nodePedestrianLane.m_Flags & PedestrianLaneFlags.Unsafe) == 0)
                    {
                        if (laneConnection.m_SourceEdge == edgeEntity || laneConnection.m_DestEdge == edgeEntity)
                        {
                            if (IsCrossingStopLine(em, nodeSubLane.m_SubLane, edgeEntity))
                            {
                                edgeInfo.m_PedestrianLaneStopLineCount++;
                            }
                            else
                            {
                                edgeInfo.m_PedestrianLaneNonStopLineCount++;
                            }
                        }
                    }
                }
            }
            edgeInfoList.Add(edgeInfo);
        }

        return edgeInfoList;
    }

    public static NativeHashMap<Entity, LaneConnection> GetLaneConnectionMap(Allocator allocator, EntityManager em, Entity nodeEntity)
    {
        NativeHashMap<Entity, LaneConnection> laneConnectionMap = new NativeHashMap<Entity, LaneConnection>(16, allocator);
        em.TryGetBuffer(nodeEntity, true, out DynamicBuffer<SubLane> nodeSubLaneBuffer);
        em.TryGetBuffer(nodeEntity, true, out DynamicBuffer<ConnectedEdge> connectedEdgeBuffer);
        foreach (SubLane nodeSubLane in nodeSubLaneBuffer)
        {
            LaneConnection laneConnection = new LaneConnection();
            if (em.TryGetComponent(nodeSubLane.m_SubLane, out Lane nodeLane))
            {
                foreach (ConnectedEdge connectedEdge in connectedEdgeBuffer)
                {
                    em.TryGetBuffer(connectedEdge.m_Edge, true, out DynamicBuffer<SubLane> edgeSubLaneBuffer);
                    foreach (SubLane edgeSubLane in edgeSubLaneBuffer)
                    {
                        if (em.TryGetComponent(edgeSubLane.m_SubLane, out Lane edgeLane))
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
                    if (em.TryGetComponent(nodeSubLane2.m_SubLane, out Lane nodeLane2))
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

    public static float3 GetEdgePosition(EntityManager em, Entity nodeEntity, Entity edgeEntity)
    {
        float3 position = default;
        em.TryGetComponent(edgeEntity, out Edge edge);
        em.TryGetComponent(edgeEntity, out EdgeGeometry edgeGeometry);
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

    public static float3 GetEdgePosition(Entity nodeEntity, Entity edgeEntity, ComponentLookup<Edge> edgeLookup, ComponentLookup<EdgeGeometry> edgeGeometryLookup)
    {
        float3 position = default;
        edgeLookup.TryGetComponent(edgeEntity, out Edge edge);
        edgeGeometryLookup.TryGetComponent(edgeEntity, out EdgeGeometry edgeGeometry);
        if (edgeLookup.HasComponent(edgeEntity) && edgeLookup[edgeEntity].m_Start.Equals(nodeEntity))
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

    public static LaneSource GetEdgeFromNodeSubLane(EntityManager em, Entity nodeEntity, Entity nodeSubLaneEntity)
    {
        if (em.TryGetComponent(nodeSubLaneEntity, out Lane nodeLane))
        {
            em.TryGetBuffer(nodeEntity, true, out DynamicBuffer<ConnectedEdge> connectedEdgeBuffer);
            foreach (ConnectedEdge connectedEdge in connectedEdgeBuffer)
            {
                Entity edgeEntity = connectedEdge.m_Edge;
                em.TryGetBuffer(edgeEntity, true, out DynamicBuffer<SubLane> edgeSubLaneBuffer);
                foreach (SubLane edgeSubLane in edgeSubLaneBuffer)
                {
                    if (em.TryGetComponent(edgeSubLane.m_SubLane, out Lane edgeLane))
                    {
                        if (nodeLane.m_StartNode.Equals(edgeLane.m_EndNode) || (em.HasComponent<PedestrianLane>(nodeSubLaneEntity) && nodeLane.m_EndNode.Equals(edgeLane.m_StartNode)))
                        {
                            return new LaneSource
                            {
                                m_SubLane = edgeSubLane.m_SubLane,
                                m_Edge = edgeEntity
                            };
                        }
                    }
                }
            }
        }
        return new LaneSource();
    }

    public static LaneSource GetEdgeFromNodeSubLane(Entity nodeSubLaneEntity, DynamicBuffer<ConnectedEdge> connectedEdgeBuffer, ComponentLookup<Lane> laneLookup, ComponentLookup<PedestrianLane> pedestrianLaneLookup, BufferLookup<SubLane> subLaneLookup)
    {
        if (laneLookup.TryGetComponent(nodeSubLaneEntity, out Lane nodeLane))
        {
            foreach (ConnectedEdge connectedEdge in connectedEdgeBuffer)
            {
                Entity edgeEntity = connectedEdge.m_Edge;
                subLaneLookup.TryGetBuffer(edgeEntity, out DynamicBuffer<SubLane> edgeSubLaneBuffer);
                foreach (SubLane edgeSubLane in edgeSubLaneBuffer)
                {
                    if (laneLookup.TryGetComponent(edgeSubLane.m_SubLane, out Lane edgeLane))
                    {
                        if (nodeLane.m_StartNode.Equals(edgeLane.m_EndNode) || (pedestrianLaneLookup.HasComponent(nodeSubLaneEntity) && nodeLane.m_EndNode.Equals(edgeLane.m_StartNode)))
                        {
                            return new LaneSource
                            {
                                m_SubLane = edgeSubLane.m_SubLane,
                                m_Edge = edgeEntity
                            };
                        }
                    }
                }
            }
        }
        return new LaneSource();
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
                }
            }
            if (laneConnection.m_DestSubLane != Entity.Null)
            {
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
                }
            }
        }
        return laneConnection;
    }

    public static LaneSource GetEdgeFromNodeSubLane(ref InitializeTrafficLightsJob job, Entity nodeSubLaneEntity, DynamicBuffer<ConnectedEdge> connectedEdgeBuffer)
    {
        return GetEdgeFromNodeSubLane(nodeSubLaneEntity, connectedEdgeBuffer, job.m_ExtraTypeHandle.m_Lane, job.m_ExtraTypeHandle.m_PedestrianLane, job.m_ExtraTypeHandle.m_SubLane);
    }

    public static bool IsCrossingStopLine(EntityManager em, Entity nodeSubLaneEntity, Entity edgeEntity)
    {
        if (em.TryGetBuffer(nodeSubLaneEntity, true, out DynamicBuffer<LaneOverlap> laneOverlapBuffer))
        {
            if (!em.TryGetBuffer(edgeEntity, true, out DynamicBuffer<SubLane> edgeSubLaneBuffer))
            {
                return false;
            }
            foreach (LaneOverlap laneOverlap in laneOverlapBuffer)
            {
                bool isStart = false;
                bool isEnd = false;
                foreach (SubLane edgeSubLane in edgeSubLaneBuffer)
                {
                    if (em.TryGetComponent(edgeSubLane.m_SubLane, out Lane edgeLane) && em.TryGetComponent(laneOverlap.m_Other, out Lane overlapLane))
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
}