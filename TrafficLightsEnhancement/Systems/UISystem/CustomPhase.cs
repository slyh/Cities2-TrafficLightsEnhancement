using Game.Net;
using Unity.Entities;

namespace C2VM.TrafficLightsEnhancement.Systems.UISystem;

public static class CustomPhase
{
    public static bool IsLaneConnected(Lane lane, ref DynamicBuffer<SubLane> subLaneBuffer, ref ComponentLookup<Lane> laneLookup, bool startOnly)
    {
        for (int i = 0; i < subLaneBuffer.Length; i++)
        {
            Entity subLane = subLaneBuffer[i].m_SubLane;
            if (!laneLookup.HasComponent(subLane))
            {
                continue;
            }
            Lane nodeLane = laneLookup[subLane];
            if (lane.m_StartNode.Equals(nodeLane.m_StartNode))
            {
                return true;
            }
            if (lane.m_StartNode.Equals(nodeLane.m_EndNode))
            {
                return true;
            }
            if (!startOnly)
            {
                if (lane.m_EndNode.Equals(nodeLane.m_StartNode))
                {
                    return true;
                }
                if (lane.m_EndNode.Equals(nodeLane.m_EndNode))
                {
                    return true;
                }
            }
        }
        return false;
    }
}