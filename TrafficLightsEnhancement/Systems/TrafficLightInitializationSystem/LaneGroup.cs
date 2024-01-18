// Decompiled excerpt from TrafficLightInitializationSystem

using Unity.Mathematics;

namespace C2VM.TrafficLightsEnhancement.Systems.TrafficLightInitializationSystem;

public struct LaneGroup
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

    public bool m_IsTurnLeft;

    public bool m_IsTurnRight;

    public bool m_IsUTurn;

    public bool m_IsYield;
}