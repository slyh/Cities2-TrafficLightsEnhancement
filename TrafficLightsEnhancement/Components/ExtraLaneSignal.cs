using Colossal.Serialization.Entities;
using Unity.Entities;

namespace C2VM.TrafficLightsEnhancement.Components;

public struct ExtraLaneSignal : IComponentData, IQueryTypeParameter, ISerializable
{
    public enum Flags : uint
    {
        Yield = 1 << 0
    }

    private int m_SchemaVersion;

    public Flags m_Flags;

    public void Serialize<TWriter>(TWriter writer) where TWriter : IWriter
    {
        writer.Write(m_SchemaVersion);
        writer.Write((uint)m_Flags);
    }

    public void Deserialize<TReader>(TReader reader) where TReader : IReader
    {
        reader.Read(out m_SchemaVersion);
        reader.Read(out uint flags);
        m_Flags = (Flags)flags;
    }

    public ExtraLaneSignal()
    {
        m_SchemaVersion = 1;
        m_Flags = 0;
    }
}
