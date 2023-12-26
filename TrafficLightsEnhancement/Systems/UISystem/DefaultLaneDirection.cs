using C2VM.CommonLibraries.LaneSystem;
using Unity.Entities;
using static C2VM.CommonLibraries.LaneSystem.CustomLaneDirection;

namespace C2VM.TrafficLightsEnhancement.Systems.UISystem;

public static class DefaultLaneDirection
{
    public static void Build(ref DynamicBuffer<CustomLaneDirection> customLaneDirectionBuffer, ref DynamicBuffer<ConnectPositionSource> connectPositionSourceBuffer)
    {
        int currentGroupIndex = -1;
        int currentLaneStart = -1;
        int currentLaneEnd = -1;
        int laneCount = -1;
        for (int i = 0; i < connectPositionSourceBuffer.Length; i++)
        {
            ConnectPositionSource position = connectPositionSourceBuffer[i];
            if (position.m_GroupIndex != currentGroupIndex)
            {
                currentLaneStart = i;
                for (int j = i + 1; j < connectPositionSourceBuffer.Length; j++)
                {
                    ConnectPositionSource position2 = connectPositionSourceBuffer[j];
                    if (position2.m_GroupIndex != position.m_GroupIndex)
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
                currentGroupIndex = position.m_GroupIndex;
            }
            CustomLaneDirection direction = new CustomLaneDirection(position.m_Position, position.m_Tangent, position.m_Owner, position.m_GroupIndex, position.m_LaneIndex, DefaultRestriction(laneCount, i - currentLaneStart));
            if (!position.m_HasLeftTurnEdge && !direction.m_Restriction.m_BanLeft)
            {
                direction.m_Restriction.m_BanLeft = true;
                if (position.m_HasStraightEdge)
                {
                    direction.m_Restriction.m_BanStraight = false;
                }
                else if (position.m_HasRightTurnEdge)
                {
                    direction.m_Restriction.m_BanRight = false;
                }
            }
            if (!position.m_HasRightTurnEdge && !direction.m_Restriction.m_BanRight)
            {
                direction.m_Restriction.m_BanRight = true;
                if (position.m_HasStraightEdge)
                {
                    direction.m_Restriction.m_BanStraight = false;
                }
                else if (position.m_HasLeftTurnEdge)
                {
                    direction.m_Restriction.m_BanLeft = false;
                }
            }
            if (!position.m_HasStraightEdge && !direction.m_Restriction.m_BanStraight)
            {
                direction.m_Restriction.m_BanStraight = true;
                if (position.m_HasLeftTurnEdge)
                {
                    direction.m_Restriction.m_BanLeft = false;
                }
                else if (position.m_HasRightTurnEdge)
                {
                    direction.m_Restriction.m_BanRight = false;
                }
            }
            customLaneDirectionBuffer.Add(direction);
        }
    }
}