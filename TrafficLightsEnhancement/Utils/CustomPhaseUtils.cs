using C2VM.TrafficLightsEnhancement.Components;
using Game.Net;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using static C2VM.TrafficLightsEnhancement.Systems.TrafficLightInitializationSystem.PatchedTrafficLightInitializationSystem;

namespace C2VM.TrafficLightsEnhancement.Utils;

public struct CustomPhaseUtils
{
    public static void ValidateBuffer(ref InitializeTrafficLightsJob job, Entity nodeEntity, DynamicBuffer<SubLane> nodeSubLaneBuffer, DynamicBuffer<ConnectedEdge> connectedEdgeBuffer, DynamicBuffer<EdgeGroupMask> edgeGroupMaskBuffer, DynamicBuffer<SubLaneGroupMask> subLaneGroupMaskBuffer, BufferLookup<SubLane> subLaneLookup)
    {
        for (int i = 0; i < edgeGroupMaskBuffer.Length; i++)
        {
            var groupMask = edgeGroupMaskBuffer[i];
            bool edgeFound = false;
            foreach (ConnectedEdge edge in connectedEdgeBuffer)
            {
                var edgeEntity = edge.m_Edge;
                var edgePosition = NodeUtils.GetEdgePosition(ref job, nodeEntity, edgeEntity);
                if (groupMask.m_Edge == edgeEntity || LooseMatch(groupMask.m_Position, edgePosition))
                {
                    edgeFound = true;
                    groupMask.m_Edge = edgeEntity;
                    groupMask.m_Position = edgePosition;
                    break;
                }
            }
            if (edgeFound)
            {
                edgeGroupMaskBuffer[i] = groupMask;
            }
            else
            {
                edgeGroupMaskBuffer.RemoveAtSwapBack(i);
            }
        }
        foreach (ConnectedEdge edge in connectedEdgeBuffer)
        {
            var edgeEntity = edge.m_Edge;
            var edgePosition = NodeUtils.GetEdgePosition(ref job, nodeEntity, edgeEntity);
            bool edgeFound = false;
            for (int i = 0; i < edgeGroupMaskBuffer.Length; i++)
            {
                if (edgeGroupMaskBuffer[i].m_Edge == edgeEntity)
                {
                    edgeFound = true;
                    break;
                }
            }
            if (!edgeFound)
            {
                edgeGroupMaskBuffer.Add(new EdgeGroupMask(edgeEntity, edgePosition));
            }
        }

        NativeList<DynamicBuffer<SubLane>> subLaneBufferList = new(16, Allocator.Temp);
        subLaneBufferList.Add(nodeSubLaneBuffer);
        foreach (ConnectedEdge edge in connectedEdgeBuffer)
        {
            subLaneBufferList.Add(subLaneLookup[edge.m_Edge]);
        }
        for (int i = 0; i < subLaneGroupMaskBuffer.Length; i++)
        {
            var groupMask = subLaneGroupMaskBuffer[i];
            bool subLaneFound = false;
            foreach (var subLaneBuffer in subLaneBufferList)
            {
                foreach (SubLane subLane in subLaneBuffer)
                {
                    Entity subLaneEntity = subLane.m_SubLane;
                    float3 subLanePosition = NodeUtils.GetSubLanePosition(subLaneEntity, job.m_CurveData);
                    if (groupMask.m_SubLane == subLaneEntity || LooseMatch(groupMask.m_Position, subLanePosition))
                    {
                        subLaneFound = true;
                        groupMask.m_SubLane = subLaneEntity;
                        groupMask.m_Position = subLanePosition;
                        break;
                    }
                }
            }
            if (subLaneFound)
            {
                subLaneGroupMaskBuffer[i] = groupMask;
            }
            else
            {
                subLaneGroupMaskBuffer.RemoveAtSwapBack(i);
            }
        }
    }

    public static int TryGet(DynamicBuffer<EdgeGroupMask> buffer, EdgeGroupMask searchKey, out EdgeGroupMask result)
    {
        return TryGet(buffer, searchKey.m_Edge, searchKey.m_Position, out result);
    }

    public static int TryGet(DynamicBuffer<SubLaneGroupMask> buffer, SubLaneGroupMask searchKey, out SubLaneGroupMask result)
    {
        return TryGet(buffer, searchKey.m_SubLane, searchKey.m_Position, out result);
    }

    public static int TryGet(DynamicBuffer<EdgeGroupMask> buffer, Entity entity, float3 position, out EdgeGroupMask result)
    {
        if (buffer.IsCreated)
        {
            for (int i = 0; i < buffer.Length; i++)
            {
                EdgeGroupMask phase = buffer[i];
                if (phase.m_Edge.Equals(entity))
                {
                    result = phase;
                    return i;
                }
            }
            for (int i = 0; i < buffer.Length; i++)
            {
                EdgeGroupMask phase = buffer[i];
                if (LooseMatch(phase.m_Position, position))
                {
                    result = phase;
                    return i;
                }
            }
        }
        result = new EdgeGroupMask(entity, position);
        return -1;
    }

    public static int TryGet(DynamicBuffer<SubLaneGroupMask> buffer, Entity entity, float3 position, out SubLaneGroupMask result)
    {
        if (buffer.IsCreated)
        {
            for (int i = 0; i < buffer.Length; i++)
            {
                SubLaneGroupMask phase = buffer[i];
                if (phase.m_SubLane.Equals(entity))
                {
                    result = phase;
                    return i;
                }
            }
            for (int i = 0; i < buffer.Length; i++)
            {
                SubLaneGroupMask phase = buffer[i];
                if (LooseMatch(phase.m_Position, position))
                {
                    result = phase;
                    return i;
                }
            }
        }
        result = new SubLaneGroupMask(entity, position);
        return -1;
    }

    public static void SwapBit(DynamicBuffer<EdgeGroupMask> buffer, int index1, int index2)
    {
        if (!buffer.IsCreated)
        {
            return;
        }
        for (int i = 0; i < buffer.Length; i++)
        {
            var phase = buffer[i];
            SwapBit(ref phase, index1, index2);
            buffer[i] = phase;
        }
    }

    public static void SwapBit(DynamicBuffer<SubLaneGroupMask> buffer, int index1, int index2)
    {
        if (!buffer.IsCreated)
        {
            return;
        }
        for (int i = 0; i < buffer.Length; i++)
        {
            var phase = buffer[i];
            SwapBit(ref phase, index1, index2);
            buffer[i] = phase;
        }
    }

    public static void SwapBit(ref EdgeGroupMask phase, int index1, int index2)
    {
        TurnSwapBit(ref phase.m_Car, index1, index2);
        TurnSwapBit(ref phase.m_PublicCar, index1, index2);
        TurnSwapBit(ref phase.m_Track, index1, index2);
        SignalSwapBit(ref phase.m_PedestrianStopLine, index1, index2);
        SignalSwapBit(ref phase.m_PedestrianNonStopLine, index1, index2);
    }

    public static void SwapBit(ref SubLaneGroupMask phase, int index1, int index2)
    {
        TurnSwapBit(ref phase.m_Vehicle, index1, index2);
        SignalSwapBit(ref phase.m_Pedestrian, index1, index2);
    }

    public static void TurnSwapBit(ref GroupMask.Turn signal, int index1, int index2)
    {
        SignalSwapBit(ref signal.m_Left, index1, index2);
        SignalSwapBit(ref signal.m_Straight, index1, index2);
        SignalSwapBit(ref signal.m_Right, index1, index2);
        SignalSwapBit(ref signal.m_UTurn, index1, index2);
    }

    public static void SignalSwapBit(ref GroupMask.Signal signal, int index1, int index2)
    {
        signal.m_GoGroupMask = SwapBit(signal.m_GoGroupMask, index1, index2);
        signal.m_YieldGroupMask = SwapBit(signal.m_YieldGroupMask, index1, index2);
    }

    public static ushort SwapBit(ushort input, int index1, int index2)
    {
        if (index1 < index2)
        {
            (index2, index1) = (index1, index2);
        }
        ushort mask1 = (ushort)(1 << index1);
        ushort mask2 = (ushort)(1 << index2);
        ushort newValue1 = (ushort)((input & mask1) >> (index1 - index2));
        ushort newValue2 = (ushort)((input & mask2) << (index1 - index2));
        return (ushort)((input & (~(mask1 | mask2))) | newValue1 | newValue2);
    }

    public static ushort SetBit(ushort input, int index, int value)
    {
        return (ushort)((input & (~(1 << index))) | (value << index));
    }

    public static bool LooseMatch(float3 a, float3 b)
    {
        float3 diff = math.abs(a - b);
        if (diff.x < 0.1f && diff.y < 0.1f && diff.z < 0.1f)
        {
            return true;
        }
        return false;
    }
}