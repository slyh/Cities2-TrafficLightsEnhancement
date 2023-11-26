using C2VM.CommonLibraries.LaneSystem;
using Unity.Entities;
using static C2VM.CommonLibraries.LaneSystem.CustomLaneDirection;

namespace C2VM.TrafficLightsEnhancement.Systems.UISystem;

public static class DefaultLaneDirection
{
    public static void Build(ref DynamicBuffer<CustomLaneDirection> customLaneDirectionBuffer, ref DynamicBuffer<ConnectPositionSource> connectPositionSourceBuffer)
    {
        int currentNodeLaneIndex = -1;
        int currentLaneStart = -1;
        int currentLaneEnd = -1;
        int laneCount = -1;
        for (int i = 0; i < connectPositionSourceBuffer.Length; i++)
        {
            ConnectPositionSource position = connectPositionSourceBuffer[i];
            if (position.m_NodeLaneIndex != currentNodeLaneIndex)
            {
                currentLaneStart = i;
                for (int j = i + 1; j < connectPositionSourceBuffer.Length; j++)
                {
                    ConnectPositionSource position2 = connectPositionSourceBuffer[j];
                    if (position2.m_NodeLaneIndex != position.m_NodeLaneIndex)
                    {
                        currentLaneEnd = j - 1;
                        break;
                    }
                    if (j == connectPositionSourceBuffer.Length - 1)
                    {
                        currentLaneEnd = j;
                        break;
                    }
                }
                laneCount = currentLaneEnd - currentLaneStart + 1;
                currentNodeLaneIndex = position.m_NodeLaneIndex;
            }
            CustomLaneDirection direction = new CustomLaneDirection(position.m_Position, DefaultRestriction(laneCount, i - currentLaneStart));
            customLaneDirectionBuffer.Add(direction);
        }
    }
}