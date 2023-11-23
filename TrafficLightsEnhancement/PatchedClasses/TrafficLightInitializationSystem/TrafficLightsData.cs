using Colossal.Serialization.Entities;
using Unity.Collections;
using Unity.Entities;

namespace TrafficLightsEnhancement.PatchedClasses;

public struct TrafficLightsData : IComponentData, IQueryTypeParameter, ISerializable
{
    public const int DefaultSelectedPatternLength = 16;

    private NativeArray<int> m_SelectedPattern;

    public void Serialize<TWriter>(TWriter writer) where TWriter : IWriter
    {
        for (int i = 0; i < DefaultSelectedPatternLength; i++)
        {
            writer.Write(m_SelectedPattern[i]);
        }
    }

    public void Deserialize<TReader>(TReader reader) where TReader : IReader
    {
        m_SelectedPattern = new NativeArray<int>(DefaultSelectedPatternLength, Allocator.Persistent);
        for (int i = 0; i < m_SelectedPattern.Length; i++)
        {
            reader.Read(out int pattern);
            m_SelectedPattern[i] = pattern;
        }
    }

    public TrafficLightsData()
    {
        m_SelectedPattern = new NativeArray<int>(DefaultSelectedPatternLength, Allocator.Persistent);
    }

    public TrafficLightsData(int[] patterns)
    {
        m_SelectedPattern = new NativeArray<int>(patterns, Allocator.Persistent);
    }

    public int GetPattern(int ways)
    {
        return m_SelectedPattern[ways];
    }

    public NativeArray<int> GetPatterns()
    {
        return m_SelectedPattern;
    }

    public void SetPatterns(int[] patterns)
    {
        m_SelectedPattern.CopyFrom(patterns);
    }

    public void SetPattern(int ways, int pattern)
    {
        m_SelectedPattern[ways] = pattern;
    }
}