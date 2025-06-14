using C2VM.TrafficLightsEnhancement.Components;
using Colossal.UI.Binding;
using Unity.Entities;
using static C2VM.TrafficLightsEnhancement.Systems.UI.UITypes;

namespace C2VM.TrafficLightsEnhancement.Utils;

public partial struct NodeUtils
{
    public struct SubLaneInfo : IJsonWritable
    {
        public Entity m_SubLane;

        public WorldPosition m_Position;

        public int m_CarLaneLeftCount;

        public int m_CarLaneStraightCount;

        public int m_CarLaneRightCount;

        public int m_CarLaneUTurnCount;

        public int m_TrackLaneLeftCount;

        public int m_TrackLaneStraightCount;

        public int m_TrackLaneRightCount;

        public int m_PedestrianLaneCount;

        public SubLaneGroupMask m_SubLaneGroupMask;

        public void Write(IJsonWriter writer)
        {
            writer.TypeBegin(typeof(SubLaneInfo).FullName);
            writer.PropertyName("m_SubLane");
            writer.Write(m_SubLane);
            writer.PropertyName("m_Position");
            writer.Write<WorldPosition>(m_Position);
            writer.PropertyName("m_CarLaneLeftCount");
            writer.Write(m_CarLaneLeftCount);
            writer.PropertyName("m_CarLaneStraightCount");
            writer.Write(m_CarLaneStraightCount);
            writer.PropertyName("m_CarLaneRightCount");
            writer.Write(m_CarLaneRightCount);
            writer.PropertyName("m_CarLaneUTurnCount");
            writer.Write(m_CarLaneUTurnCount);
            writer.PropertyName("m_TrackLaneLeftCount");
            writer.Write(m_TrackLaneLeftCount);
            writer.PropertyName("m_TrackLaneStraightCount");
            writer.Write(m_TrackLaneStraightCount);
            writer.PropertyName("m_TrackLaneRightCount");
            writer.Write(m_TrackLaneRightCount);
            writer.PropertyName("m_PedestrianLaneCount");
            writer.Write(m_PedestrianLaneCount);
            writer.PropertyName("m_SubLaneGroupMask");
            writer.Write(m_SubLaneGroupMask);
            writer.TypeEnd();
        }
    }
}