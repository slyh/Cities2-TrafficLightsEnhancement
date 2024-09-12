using Colossal.Serialization.Entities;
using Unity.Entities;
using Unity.Mathematics;
using static C2VM.TrafficLightsEnhancement.Systems.UISystem.Types;

namespace C2VM.TrafficLightsEnhancement.Components;

public struct CustomPhaseGroupMask : IBufferElementData, ISerializable
{
    public struct Signal : ISerializable
    {
        private ushort m_SchemaVersion;

        public ushort m_GoGroupMask;

        public ushort m_YieldGroupMask;

        public void Deserialize<TReader>(TReader reader) where TReader : IReader
        {
            reader.Read(out m_SchemaVersion);
            reader.Read(out m_GoGroupMask);
            reader.Read(out m_YieldGroupMask);
        }

        public void Serialize<TWriter>(TWriter writer) where TWriter : IWriter
        {
            writer.Write(m_SchemaVersion);
            writer.Write(m_GoGroupMask);
            writer.Write(m_YieldGroupMask);
        }

        public Signal()
        {
            m_SchemaVersion = 1;
            m_GoGroupMask = 0;
            m_YieldGroupMask = 0;
        }
    }

    public struct Turn : ISerializable
    {
        private ushort m_SchemaVersion;

        public Signal m_Left;

        public Signal m_Straight;

        public Signal m_Right;

        public Signal m_UTurn;

        public void Deserialize<TReader>(TReader reader) where TReader : IReader
        {
            reader.Read(out m_SchemaVersion);
            reader.Read(out m_Left);
            reader.Read(out m_Straight);
            reader.Read(out m_Right);
            reader.Read(out m_UTurn);
        }

        public void Serialize<TWriter>(TWriter writer) where TWriter : IWriter
        {
            writer.Write(m_SchemaVersion);
            writer.Write(m_Left);
            writer.Write(m_Straight);
            writer.Write(m_Right);
            writer.Write(m_UTurn);
        }

        public Turn()
        {
            m_SchemaVersion = 1;
            m_Left = new Signal();
            m_Straight = new Signal();
            m_Right = new Signal();
            m_UTurn = new Signal();
        }
    }

    private ushort m_SchemaVersion;

    public Entity m_Edge;

    public WorldPosition m_EdgePosition;

    public uint m_Group;

    public Turn m_Car;

    public Turn m_PublicCar;

    public Turn m_Track;

    public Signal m_PedestrianStopLine;

    public Signal m_PedestrianNonStopLine;

    public void Serialize<TWriter>(TWriter writer) where TWriter : IWriter
    {
        writer.Write(m_SchemaVersion);
        writer.Write(m_Edge);
        writer.Write((float3)m_EdgePosition);
        writer.Write(m_Group);
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
        reader.Read(out m_Group);
        reader.Read(out m_Car);
        reader.Read(out m_PublicCar);
        reader.Read(out m_Track);
        reader.Read(out m_PedestrianStopLine);
        reader.Read(out m_PedestrianNonStopLine);
        m_EdgePosition = edgePosition;
    }

    public CustomPhaseGroupMask()
    {
        m_SchemaVersion = 1;
        m_Edge = Entity.Null;
        m_EdgePosition = 0.0f;
        m_Group = 0;
        m_Car = new Turn();
        m_PublicCar = new Turn();
        m_Track = new Turn();
        m_PedestrianStopLine = new Signal();
        m_PedestrianNonStopLine = new Signal();
    }

    public CustomPhaseGroupMask(Entity edge, float3 position, uint group)
    {
        m_SchemaVersion = 1;
        m_Edge = edge;
        m_EdgePosition = position;
        m_Group = group;
        m_Car = new Turn();
        m_PublicCar = new Turn();
        m_Track = new Turn();
        m_PedestrianStopLine = new Signal();
        m_PedestrianNonStopLine = new Signal();
    }

    public CustomPhaseGroupMask(Entity edge, float3 position, uint group, CustomPhaseGroupMask newValue)
    {
        m_SchemaVersion = 1;
        m_Edge = edge;
        m_EdgePosition = position;
        m_Group = group;
        m_Car = newValue.m_Car;
        m_PublicCar = newValue.m_PublicCar;
        m_Track = newValue.m_Track;
        m_PedestrianStopLine = newValue.m_PedestrianStopLine;
        m_PedestrianNonStopLine = newValue.m_PedestrianNonStopLine;
    }

    public CustomPhaseGroupMask(CustomPhaseGroupMask oldValue, CustomPhaseGroupMask newValue)
    {
        m_SchemaVersion = 1;
        m_Edge = oldValue.m_Edge;
        m_EdgePosition = oldValue.m_EdgePosition;
        m_Group = oldValue.m_Group;
        m_Car = newValue.m_Car;
        m_PublicCar = newValue.m_PublicCar;
        m_Track = newValue.m_Track;
        m_PedestrianStopLine = newValue.m_PedestrianStopLine;
        m_PedestrianNonStopLine = newValue.m_PedestrianNonStopLine;
    }
}