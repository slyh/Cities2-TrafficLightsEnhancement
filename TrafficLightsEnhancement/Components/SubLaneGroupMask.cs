using Colossal.Serialization.Entities;
using Unity.Entities;
using Unity.Mathematics;
using static C2VM.TrafficLightsEnhancement.Systems.UISystem.Types;

namespace C2VM.TrafficLightsEnhancement.Components;

public struct SubLaneGroupMask : IBufferElementData, ISerializable
{
    public enum Options : uint
    {
    }

    private ushort m_SchemaVersion;

    public Entity m_SubLane;

    public WorldPosition m_Position;

    public Options m_Options;

    public GroupMask.Turn m_Vehicle;

    public GroupMask.Signal m_Pedestrian;

    public void Serialize<TWriter>(TWriter writer) where TWriter : IWriter
    {
        writer.Write(m_SchemaVersion);
        writer.Write(m_SubLane);
        writer.Write((float3)m_Position);
        writer.Write((uint)m_Options);
        writer.Write(m_Vehicle);
        writer.Write(m_Pedestrian);
    }

    public void Deserialize<TReader>(TReader reader) where TReader : IReader
    {
        reader.Read(out m_SchemaVersion);
        reader.Read(out m_SubLane);
        reader.Read(out float3 subLanePosition);
        reader.Read(out uint options);
        reader.Read(out m_Vehicle);
        reader.Read(out m_Pedestrian);
        m_Position = subLanePosition;
        m_Options = (Options)options;
    }

    public SubLaneGroupMask()
    {
        m_SchemaVersion = 1;
        m_SubLane = Entity.Null;
        m_Position = 0.0f;
        m_Options = 0;
        m_Vehicle = new GroupMask.Turn();
        m_Pedestrian = new GroupMask.Signal();
    }

    public SubLaneGroupMask(Entity subLane, float3 position)
    {
        m_SchemaVersion = 1;
        m_SubLane = subLane;
        m_Position = position;
        m_Options = 0;
        m_Vehicle = new GroupMask.Turn();
        m_Pedestrian = new GroupMask.Signal();
    }

    public SubLaneGroupMask(Entity subLane, float3 position, SubLaneGroupMask newValue)
    {
        m_SchemaVersion = 1;
        m_SubLane = subLane;
        m_Position = position;
        m_Options = newValue.m_Options;
        m_Vehicle = newValue.m_Vehicle;
        m_Pedestrian = newValue.m_Pedestrian;
    }

    public SubLaneGroupMask(SubLaneGroupMask oldValue, SubLaneGroupMask newValue)
    {
        m_SchemaVersion = 1;
        m_SubLane = oldValue.m_SubLane;
        m_Position = oldValue.m_Position;
        m_Options = newValue.m_Options;
        m_Vehicle = newValue.m_Vehicle;
        m_Pedestrian = newValue.m_Pedestrian;
    }
}