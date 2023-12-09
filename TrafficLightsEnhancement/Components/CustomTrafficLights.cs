using C2VM.TrafficLightsEnhancement.Systems.TrafficLightInitializationSystem;
using Colossal.Serialization.Entities;
using Unity.Collections;
using Unity.Entities;

namespace C2VM.TrafficLightsEnhancement.Components;

public struct CustomTrafficLights : IComponentData, IQueryTypeParameter, ISerializable
{
    // Only used in schema 1
    private const int DefaultSelectedPatternLength = 16;

    // Only used in schema 1
    private NativeArray<uint> m_SelectedPatternArray;

    // Only used in schema 2
    private uint m_Pattern;

    private int m_SchemaVersion;

    public void Serialize<TWriter>(TWriter writer) where TWriter : IWriter
    {
        if (m_SchemaVersion == 1)
        {
            for (int i = 0; i < DefaultSelectedPatternLength; i++)
            {
                writer.Write(m_SelectedPatternArray[i]);
            }
        }
        else if (m_SchemaVersion == 2)
        {
            writer.Write(uint.MaxValue);
            writer.Write(m_SchemaVersion);
            writer.Write(m_Pattern);
        }
    }

    public void Deserialize<TReader>(TReader reader) where TReader : IReader
    {
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
            m_SelectedPatternArray = new NativeArray<uint>(DefaultSelectedPatternLength, Allocator.Persistent);
            for (int i = 1; i < m_SelectedPatternArray.Length; i++)
            {
                reader.Read(out uint pattern);
                m_SelectedPatternArray[i] = pattern;
            }
        }
        else if (m_SchemaVersion == 2)
        {
            reader.Read(out m_Pattern);
        }
    }

    public CustomTrafficLights(uint pattern)
    {
        m_SchemaVersion = 2;
        m_Pattern = pattern;
    }

    public uint GetPattern(int ways)
    {
        if (m_SchemaVersion == 1)
        {
            return m_SelectedPatternArray[ways];
        }
        else if (m_SchemaVersion == 2)
        {
            return m_Pattern;
        }
        return (int) TrafficLightPatterns.Pattern.Vanilla;
    }

    public void SetPattern(uint pattern)
    {
        m_SchemaVersion = 2;
        m_Pattern = pattern;
    }
}