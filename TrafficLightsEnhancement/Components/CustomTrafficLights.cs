using C2VM.TrafficLightsEnhancement.Systems.TrafficLightInitializationSystem;
using Colossal.Serialization.Entities;
using Unity.Collections;
using Unity.Entities;

namespace C2VM.TrafficLightsEnhancement.Components;

public struct CustomTrafficLights : IComponentData, IQueryTypeParameter, ISerializable
{
    private int m_SchemaVersion;

    // Schema 1
    private const int DefaultSelectedPatternLength = 16;

    private TrafficLightPatterns.Pattern m_TwoWayPattern;

    private TrafficLightPatterns.Pattern m_ThreeWayPattern;

    private TrafficLightPatterns.Pattern m_FourWayPattern;

    // Schema 2
    private TrafficLightPatterns.Pattern m_Pattern;

    // Schema 3
    public float m_PedestrianPhaseDurationMultiplier { get; private set; }

    public int m_PedestrianPhaseGroupMask { get; private set; }

    public void Serialize<TWriter>(TWriter writer) where TWriter : IWriter
    {
        if (m_SchemaVersion == 1)
        {
            for (int i = 0; i < DefaultSelectedPatternLength; i++)
            {
                if (i == 3)
                {
                    writer.Write((uint)m_ThreeWayPattern);
                }
                else if (i == 4)
                {
                    writer.Write((uint)m_FourWayPattern);
                }
                else
                {
                    writer.Write((uint)m_TwoWayPattern);
                }
            }
        }
        else if (m_SchemaVersion >= 2)
        {
            m_SchemaVersion = 3;
            writer.Write(uint.MaxValue);
            writer.Write(m_SchemaVersion);
            writer.Write((uint)m_Pattern);
            writer.Write(m_PedestrianPhaseDurationMultiplier);
            writer.Write(m_PedestrianPhaseGroupMask);
        }
        #if VERBOSITY_DEBUG
        System.Console.WriteLine($"CustomTrafficLights Serialize m_SchemaVersion {m_SchemaVersion} m_Pattern {(uint)m_Pattern} m_PedestrianPhaseDurationMultiplier {m_PedestrianPhaseDurationMultiplier} m_PedestrianPhaseGroupMask {m_PedestrianPhaseGroupMask}");
        #endif
    }

    public void Deserialize<TReader>(TReader reader) where TReader : IReader
    {
        m_PedestrianPhaseDurationMultiplier = 1;
        m_PedestrianPhaseGroupMask = 0;
        reader.Read(out uint uint1);
        if (uint1 == uint.MaxValue)
        {
            reader.Read(out m_SchemaVersion);
        }
        else
        {
            m_SchemaVersion = 1;
        }
        if (m_SchemaVersion == 1)
        {
            for (int i = 1; i < DefaultSelectedPatternLength; i++)
            {
                reader.Read(out uint pattern);
                if (i == 2)
                {
                    m_TwoWayPattern = (TrafficLightPatterns.Pattern)pattern;
                }
                else if (i == 3)
                {
                    m_ThreeWayPattern = (TrafficLightPatterns.Pattern)pattern;
                }
                else if (i == 4)
                {
                    m_FourWayPattern = (TrafficLightPatterns.Pattern)pattern;
                }
            }
        }
        if (m_SchemaVersion >= 2)
        {
            reader.Read(out uint pattern);
            m_Pattern = (TrafficLightPatterns.Pattern)pattern;
        }
        if (m_SchemaVersion >= 3)
        {
            reader.Read(out float pedestrianPhaseDurationMultiplier);
            reader.Read(out int pedestrianPhaseGroupMask);
            m_PedestrianPhaseDurationMultiplier = pedestrianPhaseDurationMultiplier;
            m_PedestrianPhaseGroupMask = pedestrianPhaseGroupMask;
        }
        #if VERBOSITY_DEBUG
        System.Console.WriteLine($"CustomTrafficLights Deserialize m_SchemaVersion {m_SchemaVersion}");
        #endif
    }

    public CustomTrafficLights()
    {
        m_SchemaVersion = 3;
        m_Pattern = TrafficLightPatterns.Pattern.Vanilla;
        m_PedestrianPhaseDurationMultiplier = 1;
        m_PedestrianPhaseGroupMask = 0;
    }

    public CustomTrafficLights(TrafficLightPatterns.Pattern pattern)
    {
        m_SchemaVersion = 3;
        m_Pattern = pattern;
        m_PedestrianPhaseDurationMultiplier = 1;
        m_PedestrianPhaseGroupMask = 0;
    }

    public TrafficLightPatterns.Pattern GetPattern(int ways)
    {
        #if VERBOSITY_DEBUG
        System.Console.WriteLine($"CustomTrafficLights GetPattern ways {ways} m_SchemaVersion {m_SchemaVersion} m_Pattern {m_Pattern} m_TwoWayPattern {m_TwoWayPattern} m_ThreeWayPattern {m_ThreeWayPattern} m_FourWayPattern {m_FourWayPattern}");
        #endif
        if (m_SchemaVersion == 1)
        {
            m_SchemaVersion = 3;
            if (ways == 3)
            {
                m_Pattern = m_ThreeWayPattern;
            }
            else if (ways == 4)
            {
                m_Pattern = m_FourWayPattern;
            }
            else
            {
                m_Pattern = m_TwoWayPattern;
            }
            #if VERBOSITY_DEBUG
            System.Console.WriteLine($"CustomTrafficLights Upgrade m_SchemaVersion from 1 to 3, ways {ways} m_Pattern {(uint)m_Pattern}");
            #endif
            return m_Pattern;
        }
        else if (m_SchemaVersion >= 2)
        {
            return m_Pattern;
        }
        return TrafficLightPatterns.Pattern.Vanilla;
    }

    public void SetPattern(uint pattern)
    {
        SetPattern((TrafficLightPatterns.Pattern)pattern);
    }

    public void SetPattern(TrafficLightPatterns.Pattern pattern)
    {
        m_SchemaVersion = 3;
        m_Pattern = pattern;
    }

    public void SetPedestrianPhaseDurationMultiplier(float durationMultiplier)
    {
        if (m_SchemaVersion >= 2)
        {
            m_SchemaVersion = 3;
        }
        m_PedestrianPhaseDurationMultiplier = durationMultiplier;
    }

    public void SetPedestrianPhaseGroupMask(int groupMask)
    {
        if (m_SchemaVersion >= 2)
        {
            m_SchemaVersion = 3;
        }
        m_PedestrianPhaseGroupMask = groupMask;
    }
}