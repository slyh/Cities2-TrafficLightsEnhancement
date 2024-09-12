using Colossal.Serialization.Entities;
using Unity.Entities;

namespace C2VM.TrafficLightsEnhancement.Components;

public struct CustomPhaseData : IBufferElementData, ISerializable
{
    private ushort m_SchemaVersion;

    public float m_MinimumDurationMultiplier;

    public void Serialize<TWriter>(TWriter writer) where TWriter : IWriter
    {
        writer.Write(m_SchemaVersion);
        writer.Write(m_MinimumDurationMultiplier);
    }

    public void Deserialize<TReader>(TReader reader) where TReader : IReader
    {
        reader.Read(out m_SchemaVersion);
        reader.Read(out m_MinimumDurationMultiplier);
    }

    public CustomPhaseData()
    {
        m_SchemaVersion = 1;
        m_MinimumDurationMultiplier = 1.0f;
    }
}