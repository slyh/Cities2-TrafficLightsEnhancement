using Colossal.Serialization.Entities;
using Colossal.UI.Binding;

namespace C2VM.TrafficLightsEnhancement.Components;

public struct GroupMask
{
    public struct Signal : ISerializable, IJsonWritable
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

        public void Write(IJsonWriter writer)
        {
            writer.TypeBegin(typeof(Signal).FullName);
            writer.PropertyName("m_GoGroupMask");
            writer.Write(m_GoGroupMask);
            writer.PropertyName("m_YieldGroupMask");
            writer.Write(m_YieldGroupMask);
            writer.TypeEnd();
        }

        public Signal()
        {
            m_SchemaVersion = 1;
            m_GoGroupMask = 0;
            m_YieldGroupMask = 0;
        }
    }

    public struct Turn : ISerializable, IJsonWritable
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

        public void Write(IJsonWriter writer)
        {
            writer.TypeBegin(typeof(Turn).FullName);
            writer.PropertyName("m_Left");
            writer.Write(m_Left);
            writer.PropertyName("m_Straight");
            writer.Write(m_Straight);
            writer.PropertyName("m_Right");
            writer.Write(m_Right);
            writer.PropertyName("m_UTurn");
            writer.Write(m_UTurn);
            writer.TypeEnd();
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
}