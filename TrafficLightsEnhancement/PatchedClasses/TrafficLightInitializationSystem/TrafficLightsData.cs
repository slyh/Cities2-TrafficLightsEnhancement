using Colossal.Serialization.Entities;
using Unity.Entities;

namespace TrafficLightsEnhancement.PatchedClasses;

public struct TrafficLightsData : IComponentData, IQueryTypeParameter, ISerializable
{
    public int m_Pattern;

    public void Serialize<TWriter>(TWriter writer) where TWriter : IWriter
    {
        writer.Write(m_Pattern);
    }

    public void Deserialize<TReader>(TReader reader) where TReader : IReader
    {
        reader.Read(out m_Pattern);
    }
}