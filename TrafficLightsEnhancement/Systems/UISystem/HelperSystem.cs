using Colossal.Entities;
using Game.Net;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

namespace C2VM.TrafficLightsEnhancement.Systems.UISystem;

public class HelperSystem : Game.GameSystemBase
{
    public struct EdgeInfo
    {
        public Entity m_Edge;

        public float3 m_Position;

        public int m_CarLaneCount;

        public int m_TrackLaneCount;

        public int m_PedestrianLaneCount;
    }

    protected override void OnUpdate()
    {
    }
    
    public NativeList<EdgeInfo> GetEdgeInfoList(Entity node)
    {
        NativeList<EdgeInfo> edgeInfoList = new NativeList<EdgeInfo>(0, Allocator.Temp);
        EntityManager.TryGetBuffer<ConnectedEdge>(node, isReadOnly: true, out DynamicBuffer<ConnectedEdge> connectedEdge);
        for (int i = 0; i < connectedEdge.Length; i++)
        {
            EdgeInfo edgeInfo = default;
            Entity edgeEntity = connectedEdge[i].m_Edge;
            EntityManager.TryGetComponent<Edge>(node, out Edge edge);
            EntityManager.TryGetComponent<EdgeGeometry>(edgeEntity, out EdgeGeometry edgeGeometry);
            edgeInfo.m_Edge = edgeEntity;
            if (edge.m_Start.Equals(node))
            {
                edgeInfo.m_Position = edgeGeometry.m_Start.m_Left.a;
            }
            if (edge.m_End.Equals(node))
            {
                edgeInfo.m_Position = edgeGeometry.m_End.m_Right.d;
            }
            EntityManager.TryGetBuffer<SubLane>(edgeEntity, isReadOnly: true, out DynamicBuffer<SubLane> subLane);
            for (int j = 0; j < subLane.Length; j++)
            {
                if (EntityManager.HasComponent<CarLane>(subLane[j].m_SubLane))
                {
                    edgeInfo.m_CarLaneCount++;
                }
                else if (EntityManager.HasComponent<PedestrianLane>(subLane[j].m_SubLane))
                {
                    edgeInfo.m_PedestrianLaneCount++;
                }
                else if (!EntityManager.HasComponent<SlaveLane>(subLane[j].m_SubLane))
                {
                    edgeInfo.m_TrackLaneCount++;
                }
            }
            edgeInfoList.Add(edgeInfo);
        }
        return edgeInfoList;
    }
}