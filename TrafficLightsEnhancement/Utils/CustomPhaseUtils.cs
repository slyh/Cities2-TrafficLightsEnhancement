using C2VM.TrafficLightsEnhancement.Components;
using Colossal.Entities;
using Unity.Entities;
using Unity.Mathematics;

namespace C2VM.TrafficLightsEnhancement.Utils;

public struct CustomPhaseUtils
{
    public static void ValidateBuffer(EntityManager em, Entity nodeEntity)
    {
        var edgeInfoList = NodeUtils.GetEdgeInfoList(Unity.Collections.Allocator.Temp, em, nodeEntity);
        if (!em.TryGetBuffer(nodeEntity, true, out DynamicBuffer<CustomPhaseGroupMask> customPhaseGroupMaskBuffer))
        {
            customPhaseGroupMaskBuffer = em.AddBuffer<CustomPhaseGroupMask>(nodeEntity);
        }
        customPhaseGroupMaskBuffer.Clear();
        foreach (var edgeInfo in edgeInfoList)
        {
            customPhaseGroupMaskBuffer.Add(new CustomPhaseGroupMask(edgeInfo.m_Edge, edgeInfo.m_Position, edgeInfo.m_Group, edgeInfo.m_CustomPhaseGroupMask));
        }
    }

    public static int TryGet(DynamicBuffer<CustomPhaseGroupMask> buffer, CustomPhaseGroupMask searchKey, out CustomPhaseGroupMask result)
    {
        return TryGet(buffer, searchKey.m_Edge, searchKey.m_EdgePosition, searchKey.m_Group, out result);
    }

    public static int TryGet(DynamicBuffer<CustomPhaseGroupMask> buffer, Entity edgeEntity, float3 edgePosition, uint group, out CustomPhaseGroupMask result)
    {
        for (int i = 0; i < buffer.Length; i++)
        {
            CustomPhaseGroupMask phase = buffer[i];
            if (phase.m_Edge.Equals(edgeEntity) && phase.m_Group == group)
            {
                result = phase;
                return i;
            }
        }
        for (int i = 0; i < buffer.Length; i++)
        {
            CustomPhaseGroupMask phase = buffer[i];
            if (LooseMatch(phase.m_EdgePosition, edgePosition) && phase.m_Group == group)
            {
                result = phase;
                return i;
            }
        }
        result = new CustomPhaseGroupMask(edgeEntity, edgePosition, group);
        return -1;
    }

    public static void SwapBit(DynamicBuffer<CustomPhaseGroupMask> buffer, int index1, int index2)
    {
        for (int i = 0; i < buffer.Length; i++)
        {
            var phase = buffer[i];
            SwapBit(ref phase, index1, index2);
            buffer[i] = phase;
        }
    }

    public static void SwapBit(ref CustomPhaseGroupMask phase, int index1, int index2)
    {
        TurnSwapBit(ref phase.m_Car, index1, index2);
        TurnSwapBit(ref phase.m_PublicCar, index1, index2);
        TurnSwapBit(ref phase.m_Track, index1, index2);
        SignalSwapBit(ref phase.m_PedestrianStopLine, index1, index2);
        SignalSwapBit(ref phase.m_PedestrianNonStopLine, index1, index2);
    }

    public static void TurnSwapBit(ref CustomPhaseGroupMask.Turn signal, int index1, int index2)
    {
        SignalSwapBit(ref signal.m_Left, index1, index2);
        SignalSwapBit(ref signal.m_Straight, index1, index2);
        SignalSwapBit(ref signal.m_Right, index1, index2);
        SignalSwapBit(ref signal.m_UTurn, index1, index2);
    }

    public static void SignalSwapBit(ref CustomPhaseGroupMask.Signal signal, int index1, int index2)
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