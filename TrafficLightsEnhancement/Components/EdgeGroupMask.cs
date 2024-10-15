using Colossal.Serialization.Entities;
using Colossal.UI.Binding;
using Unity.Entities;
using Unity.Mathematics;
using static C2VM.TrafficLightsEnhancement.Systems.UI.UITypes;

namespace C2VM.TrafficLightsEnhancement.Components;

public struct EdgeGroupMask : IBufferElementData, ISerializable, IJsonWritable
{
    public enum Options : uint
    {
        PerLaneSignal = 1 << 0
    }

    private ushort m_SchemaVersion;

    public Entity m_Edge;

    public WorldPosition m_Position;

    public Options m_Options;

    public GroupMask.Turn m_Car;

    public GroupMask.Turn m_PublicCar;

    public GroupMask.Turn m_Track;

    public GroupMask.Signal m_PedestrianStopLine;

    public GroupMask.Signal m_PedestrianNonStopLine;

    public void Serialize<TWriter>(TWriter writer) where TWriter : IWriter
    {
        writer.Write(m_SchemaVersion);
        writer.Write(m_Edge);
        writer.Write((float3)m_Position);
        writer.Write((uint)m_Options);
        writer.Write(m_Car);
        writer.Write(m_PublicCar);
        writer.Write(m_Track);
        writer.Write(m_PedestrianStopLine);
        writer.Write(m_PedestrianNonStopLine);
    }

    public void Deserialize<TReader>(TReader reader) where TReader : IReader
    {
        reader.Read(out m_SchemaVersion);
        reader.Read(out m_Edge);
        reader.Read(out float3 edgePosition);
        reader.Read(out uint options);
        reader.Read(out m_Car);
        reader.Read(out m_PublicCar);
        reader.Read(out m_Track);
        reader.Read(out m_PedestrianStopLine);
        reader.Read(out m_PedestrianNonStopLine);
        m_Position = edgePosition;
        m_Options = (Options)options;
    }

    public void Write(IJsonWriter writer)
    {
        writer.TypeBegin(typeof(EdgeGroupMask).FullName);
        writer.PropertyName("m_Edge");
        writer.Write(m_Edge);
        writer.PropertyName("m_Position");
        writer.Write<WorldPosition>(m_Position);
        writer.PropertyName("m_Options");
        writer.Write((uint)m_Options);
        writer.PropertyName("m_Car");
        writer.Write(m_Car);
        writer.PropertyName("m_PublicCar");
        writer.Write(m_PublicCar);
        writer.PropertyName("m_Track");
        writer.Write(m_Track);
        writer.PropertyName("m_PedestrianStopLine");
        writer.Write(m_PedestrianStopLine);
        writer.PropertyName("m_PedestrianNonStopLine");
        writer.Write(m_PedestrianNonStopLine);
        writer.TypeEnd();
    }

    public EdgeGroupMask()
    {
        m_SchemaVersion = 1;
        m_Edge = Entity.Null;
        m_Position = 0.0f;
        m_Options = 0;
        m_Car = new GroupMask.Turn();
        m_PublicCar = new GroupMask.Turn();
        m_Track = new GroupMask.Turn();
        m_PedestrianStopLine = new GroupMask.Signal();
        m_PedestrianNonStopLine = new GroupMask.Signal();
    }

    public EdgeGroupMask(Entity edge, float3 position)
    {
        m_SchemaVersion = 1;
        m_Edge = edge;
        m_Position = position;
        m_Options = 0;
        m_Car = new GroupMask.Turn();
        m_PublicCar = new GroupMask.Turn();
        m_Track = new GroupMask.Turn();
        m_PedestrianStopLine = new GroupMask.Signal();
        m_PedestrianNonStopLine = new GroupMask.Signal();
    }

    public EdgeGroupMask(Entity edge, float3 position, EdgeGroupMask newValue)
    {
        m_SchemaVersion = 1;
        m_Edge = edge;
        m_Position = position;
        m_Options = newValue.m_Options;
        m_Car = newValue.m_Car;
        m_PublicCar = newValue.m_PublicCar;
        m_Track = newValue.m_Track;
        m_PedestrianStopLine = newValue.m_PedestrianStopLine;
        m_PedestrianNonStopLine = newValue.m_PedestrianNonStopLine;
    }

    public EdgeGroupMask(EdgeGroupMask oldValue, EdgeGroupMask newValue)
    {
        m_SchemaVersion = 1;
        m_Edge = oldValue.m_Edge;
        m_Position = oldValue.m_Position;
        m_Options = newValue.m_Options;
        m_Car = newValue.m_Car;
        m_PublicCar = newValue.m_PublicCar;
        m_Track = newValue.m_Track;
        m_PedestrianStopLine = newValue.m_PedestrianStopLine;
        m_PedestrianNonStopLine = newValue.m_PedestrianNonStopLine;
    }
}