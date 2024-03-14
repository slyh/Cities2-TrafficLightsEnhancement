using Colossal.Serialization.Entities;
using Unity.Entities;

namespace C2VM.TrafficLightsEnhancement.Components;

public struct TestComponent : IComponentData, IQueryTypeParameter, ISerializable
{
    public long id;

    public void Serialize<TWriter>(TWriter writer) where TWriter : IWriter
    {
        writer.Write(id);
    }

    public void Deserialize<TReader>(TReader reader) where TReader : IReader
    {
        reader.Read(out id);
    }
}
