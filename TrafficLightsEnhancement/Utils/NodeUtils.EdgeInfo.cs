using C2VM.TrafficLightsEnhancement.Components;
using Colossal.UI.Binding;
using Unity.Collections;
using Unity.Entities;
using static C2VM.TrafficLightsEnhancement.Systems.UI.UITypes;

namespace C2VM.TrafficLightsEnhancement.Utils;

public partial struct NodeUtils
{
    public struct EdgeInfo : IJsonWritable
    {
        public Entity m_Edge;

        public WorldPosition m_Position;

        public int m_CarLaneLeftCount;

        public int m_CarLaneStraightCount;

        public int m_CarLaneRightCount;

        public int m_CarLaneUTurnCount;

        public int m_PublicCarLaneLeftCount;

        public int m_PublicCarLaneStraightCount;

        public int m_PublicCarLaneRightCount;

        public int m_PublicCarLaneUTurnCount;

        public int m_TrackLaneLeftCount;

        public int m_TrackLaneStraightCount;

        public int m_TrackLaneRightCount;

        public int m_TrainTrackCount;

        public int m_PedestrianLaneStopLineCount;

        public int m_PedestrianLaneNonStopLineCount;

        public NativeArray<SubLaneInfo> m_SubLaneInfoList;

        public EdgeGroupMask m_EdgeGroupMask;

        public void Write(IJsonWriter writer)
        {
            writer.TypeBegin(typeof(EdgeInfo).FullName);
            writer.PropertyName("m_Edge");
            writer.Write(m_Edge);
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
            writer.PropertyName("m_PublicCarLaneLeftCount");
            writer.Write(m_PublicCarLaneLeftCount);
            writer.PropertyName("m_PublicCarLaneStraightCount");
            writer.Write(m_PublicCarLaneStraightCount);
            writer.PropertyName("m_PublicCarLaneRightCount");
            writer.Write(m_PublicCarLaneRightCount);
            writer.PropertyName("m_PublicCarLaneUTurnCount");
            writer.Write(m_PublicCarLaneUTurnCount);
            writer.PropertyName("m_TrackLaneLeftCount");
            writer.Write(m_TrackLaneLeftCount);
            writer.PropertyName("m_TrackLaneStraightCount");
            writer.Write(m_TrackLaneStraightCount);
            writer.PropertyName("m_TrackLaneRightCount");
            writer.Write(m_TrackLaneRightCount);
            writer.PropertyName("m_PedestrianLaneStopLineCount");
            writer.Write(m_PedestrianLaneStopLineCount);
            writer.PropertyName("m_PedestrianLaneNonStopLineCount");
            writer.Write(m_PedestrianLaneNonStopLineCount);
            writer.PropertyName("m_SubLaneInfoList");
            writer.ArrayBegin(m_SubLaneInfoList.Length);
            foreach (var subLaneInfo in m_SubLaneInfoList)
            {
                writer.Write(subLaneInfo);
            }
            writer.ArrayEnd();
            writer.PropertyName("m_EdgeGroupMask");
            writer.Write(m_EdgeGroupMask);
            writer.TypeEnd();
        }
    }
}