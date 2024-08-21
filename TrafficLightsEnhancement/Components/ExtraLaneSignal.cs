using C2VM.TrafficLightsEnhancement.Systems.TrafficLightInitializationSystem;
using Colossal.Serialization.Entities;
using Unity.Entities;

namespace C2VM.TrafficLightsEnhancement.Components;

public struct ExtraLaneSignal : IComponentData, IQueryTypeParameter, ISerializable
{
    public enum Flags : uint
    {
        Yield = 1 << 0,

        IgnorePriority = 1 << 1
    }

    private int m_SchemaVersion;

    private Flags m_Flags;

    public ushort m_YieldGroupMask;

    public ushort m_IgnorePriorityGroupMask;

    public void Serialize<TWriter>(TWriter writer) where TWriter : IWriter
    {
        writer.Write(m_SchemaVersion);
        writer.Write(m_YieldGroupMask);
        writer.Write(m_IgnorePriorityGroupMask);
    }

    public void Deserialize<TReader>(TReader reader) where TReader : IReader
    {
        reader.Read(out m_SchemaVersion);
        if (m_SchemaVersion == 1)
        {
            reader.Read(out uint flags);
            m_Flags = (Flags)flags;
            if (m_Flags == Flags.Yield)
            {
                m_YieldGroupMask = ushort.MaxValue;
            }
            if (m_Flags == Flags.IgnorePriority)
            {
                m_IgnorePriorityGroupMask = ushort.MaxValue;
            }
        }
        else if (m_SchemaVersion == 2)
        {
            reader.Read(out m_YieldGroupMask);
            reader.Read(out m_IgnorePriorityGroupMask);
        }
    }

    public ExtraLaneSignal()
    {
        m_SchemaVersion = 2;
        m_Flags = 0;
        m_YieldGroupMask = 0;
        m_IgnorePriorityGroupMask = 0;
    }

    public ExtraLaneSignal(LaneGroup laneGroup)
    {
        m_SchemaVersion = 2;
        m_Flags = 0;
        m_YieldGroupMask = laneGroup.m_YieldGroupMask;
        m_IgnorePriorityGroupMask = laneGroup.m_IgnorePriorityGroupMask;
    }
}
