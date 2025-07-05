using Colossal.Serialization.Entities;
using Unity.Entities;

namespace C2VM.TrafficLightsEnhancement.Components;

public struct ExtraLaneSignal : IComponentData, IQueryTypeParameter, ISerializable
{
    private enum Flags : uint
    {
        Yield = 1 << 0,

        IgnorePriority = 1 << 1
    }

    private int m_SchemaVersion;

    public ushort m_YieldGroupMask;

    public ushort m_IgnorePriorityGroupMask;

    public Entity m_SourceSubLane;

    public void Serialize<TWriter>(TWriter writer) where TWriter : IWriter
    {
        writer.Write(m_SchemaVersion);
        writer.Write(m_YieldGroupMask);
        writer.Write(m_IgnorePriorityGroupMask);
        writer.Write(m_SourceSubLane);
    }

    public void Deserialize<TReader>(TReader reader) where TReader : IReader
    {
        Initialisation();
        reader.Read(out int schemaVersion);
        if (schemaVersion == 1)
        {
            reader.Read(out uint flags);
            if ((flags & (uint)Flags.Yield) != 0)
            {
                m_YieldGroupMask = ushort.MaxValue;
            }
            if ((flags & (uint)Flags.IgnorePriority) != 0)
            {
                m_IgnorePriorityGroupMask = ushort.MaxValue;
            }
        }
        if (schemaVersion >= 2)
        {
            reader.Read(out m_YieldGroupMask);
            reader.Read(out m_IgnorePriorityGroupMask);
        }
        if (schemaVersion >= 3)
        {
            reader.Read(out m_SourceSubLane);
        }
    }

    private void Initialisation()
    {
        m_SchemaVersion = 3;
        m_YieldGroupMask = 0;
        m_IgnorePriorityGroupMask = 0;
        m_SourceSubLane = Entity.Null;
    }

    public ExtraLaneSignal()
    {
        Initialisation();
    }
}
