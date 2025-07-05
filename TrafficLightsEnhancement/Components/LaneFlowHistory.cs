using Colossal.Serialization.Entities;
using Unity.Entities;
using Unity.Mathematics;

namespace C2VM.TrafficLightsEnhancement.Components
{
    public struct LaneFlowHistory : IComponentData, IQueryTypeParameter, ISerializable
    {
        private ushort m_SchemaVersion;

        public float4 m_Duration;

        public float4 m_Distance;

        public uint m_Frame;

        public void Serialize<TWriter>(TWriter writer) where TWriter : IWriter
        {
            writer.Write(m_SchemaVersion);
            writer.Write(m_Duration);
            writer.Write(m_Distance);
            writer.Write(m_Frame);
        }

        public void Deserialize<TReader>(TReader reader) where TReader : IReader
        {
            reader.Read(out m_SchemaVersion);
            reader.Read(out m_Duration);
            reader.Read(out m_Distance);
            reader.Read(out m_Frame);
        }

        public LaneFlowHistory()
        {
            m_SchemaVersion = 1;
            m_Duration = 0;
            m_Distance = 0;
            m_Frame = 0;
        }
    }
}