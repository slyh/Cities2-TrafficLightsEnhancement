using Colossal.Serialization.Entities;
using Unity.Entities;
using Unity.Mathematics;

namespace C2VM.TrafficLightsEnhancement.Components;

public struct CustomPhaseData : IBufferElementData, ISerializable
{
    public enum Options : uint
    {
        PrioritiseTrack = 1 << 0,

        PrioritisePublicCar = 1 << 1,

        PrioritisePedestrian = 1 << 2,

        LinkedWithNextPhase = 1 << 3,

        EndPhasePrematurely = 1 << 4,
    }

    private ushort m_SchemaVersion;

    // Statistics
    public ushort m_TurnsSinceLastRun;

    public ushort m_LowFlowTimer;

    public float3 m_CarFlow;

    public ushort m_CarLaneOccupied;

    public ushort m_PublicCarLaneOccupied;

    public ushort m_TrackLaneOccupied;

    public ushort m_PedestrianLaneOccupied;

    public float m_WeightedWaiting;

    public float m_TargetDuration;

    public int m_Priority;

    // User configurable variables
    public Options m_Options;

    public ushort m_MinimumDuration;

    public ushort m_MaximumDuration;

    public float m_TargetDurationMultiplier;

    public float m_LaneOccupiedMultiplier;

    public float m_IntervalExponent;

    public void Serialize<TWriter>(TWriter writer) where TWriter : IWriter
    {
        writer.Write(m_SchemaVersion);
        writer.Write(m_TurnsSinceLastRun);
        writer.Write(m_LowFlowTimer);
        writer.Write(m_CarFlow);
        writer.Write(m_CarLaneOccupied);
        writer.Write(m_PublicCarLaneOccupied);
        writer.Write(m_TrackLaneOccupied);
        writer.Write(m_PedestrianLaneOccupied);
        writer.Write(m_WeightedWaiting);
        writer.Write(m_TargetDuration);
        writer.Write(m_Priority);
        writer.Write((uint)m_Options);
        writer.Write(m_MinimumDuration);
        writer.Write(m_MaximumDuration);
        writer.Write(m_TargetDurationMultiplier);
        writer.Write(m_LaneOccupiedMultiplier);
        writer.Write(m_IntervalExponent);
    }

    public void Deserialize<TReader>(TReader reader) where TReader : IReader
    {
        Initialisation();
        reader.Read(out m_SchemaVersion);
        reader.Read(out m_TurnsSinceLastRun);
        reader.Read(out m_LowFlowTimer);
        reader.Read(out m_CarFlow);
        reader.Read(out m_CarLaneOccupied);
        reader.Read(out m_PublicCarLaneOccupied);
        reader.Read(out m_TrackLaneOccupied);
        reader.Read(out m_PedestrianLaneOccupied);
        reader.Read(out m_WeightedWaiting);
        reader.Read(out m_TargetDuration);
        reader.Read(out m_Priority);
        reader.Read(out uint options);
        reader.Read(out m_MinimumDuration);
        reader.Read(out m_MaximumDuration);
        reader.Read(out m_TargetDurationMultiplier);
        reader.Read(out m_LaneOccupiedMultiplier);
        reader.Read(out m_IntervalExponent);
        m_Options = (Options)options;
    }

    private void Initialisation()
    {
        m_SchemaVersion = 1;
        m_TurnsSinceLastRun = 0;
        m_LowFlowTimer = 0;
        m_CarFlow = 0;
        m_CarLaneOccupied = 0;
        m_PublicCarLaneOccupied = 0;
        m_TrackLaneOccupied = 0;
        m_PedestrianLaneOccupied = 0;
        m_WeightedWaiting = 0;
        m_TargetDuration = 0;
        m_Priority = 0;
        m_Options = Options.PrioritiseTrack;
        m_MinimumDuration = 2;
        m_MaximumDuration = 300;
        m_TargetDurationMultiplier = 1f;
        m_LaneOccupiedMultiplier = 1f;
        m_IntervalExponent = 2f;
    }

    public CustomPhaseData()
    {
        Initialisation();
    }

    public readonly float AverageCarFlow()
    {
        return (m_CarFlow.x + m_CarFlow.y + m_CarFlow.z) / 3f;
    }

    public readonly int TotalLaneOccupied()
    {
        return m_CarLaneOccupied + m_TrackLaneOccupied + m_PedestrianLaneOccupied;
    }
}