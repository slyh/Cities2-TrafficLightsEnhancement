using C2VM.TrafficLightsEnhancement.Components;
using Game.Net;
using Unity.Entities;
using Unity.Mathematics;

namespace C2VM.TrafficLightsEnhancement.Systems.TrafficLightSystems.Simulation
{
    public struct CustomStateMachine
    {
        public static bool UpdateTrafficLightState(ref TrafficLights trafficLights, ref CustomTrafficLights customTrafficLights, DynamicBuffer<CustomPhaseData> customPhaseDataBuffer)
        {
            if (trafficLights.m_State == TrafficLightState.None || trafficLights.m_State == TrafficLightState.Extending || trafficLights.m_State == TrafficLightState.Extended)
            {
                trafficLights.m_State = TrafficLightState.Beginning;
                trafficLights.m_CurrentSignalGroup = 0;
                trafficLights.m_NextSignalGroup = GetNextSignalGroup(trafficLights.m_CurrentSignalGroup, customPhaseDataBuffer, customTrafficLights, out _);
                trafficLights.m_Timer = 0;
                customTrafficLights.m_Timer = 0;
                return true;
            }
            else if (trafficLights.m_State == TrafficLightState.Beginning)
            {
                if (trafficLights.m_NextSignalGroup <= 0)
                {
                    trafficLights.m_State = TrafficLightState.None; // roll a new group
                    return true;
                }
                trafficLights.m_State = TrafficLightState.Ongoing;
                trafficLights.m_CurrentSignalGroup = trafficLights.m_NextSignalGroup;
                trafficLights.m_NextSignalGroup = 0;
                trafficLights.m_Timer = 0;
                customTrafficLights.m_Timer = 0;
                for (int i = 0; i < customPhaseDataBuffer.Length; i++)
                {
                    CustomPhaseData phase = customPhaseDataBuffer[i];
                    if (trafficLights.m_CurrentSignalGroup == i + 1)
                    {
                        phase.m_TurnsSinceLastRun = 0;
                        phase.m_LowFlowTimer = 0;
                        phase.m_LowPriorityTimer = 0;
                    }
                    else
                    {
                        phase.m_TurnsSinceLastRun++;
                    }
                    phase.m_Options &= ~CustomPhaseData.Options.EndPhasePrematurely;
                    customPhaseDataBuffer[i] = phase;
                }
                return true;
            }
            else if (trafficLights.m_State == TrafficLightState.Ongoing)
            {
                int currentSignalIndex = trafficLights.m_CurrentSignalGroup - 1;
                if (currentSignalIndex < 0 || currentSignalIndex >= customPhaseDataBuffer.Length)
                {
                    trafficLights.m_State = TrafficLightState.None; // roll a new group
                    return true;
                }
                customTrafficLights.m_Timer++;
                CustomPhaseData phase = customPhaseDataBuffer[currentSignalIndex];
                float targetDuration = 10f * (phase.AverageCarFlow() + (float)(phase.m_TrackLaneOccupied * 0.5)) * phase.m_TargetDurationMultiplier;
                bool preferChange = false;
                phase.m_TargetDuration = targetDuration;
                if (customTrafficLights.m_Timer <= phase.m_MinimumDuration)
                {
                    phase.m_LowFlowTimer = 0;
                    phase.m_LowPriorityTimer = 0;
                }
                else if (phase.m_Priority > 0 && phase.m_Priority >= MaxPriority(customPhaseDataBuffer))
                {
                    if (customTrafficLights.m_Timer >= phase.m_MaximumDuration)
                    {
                        preferChange = true;
                    }
                    else if (customTrafficLights.m_Timer <= targetDuration)
                    {
                        phase.m_LowFlowTimer = 0;
                    }
                    else if (phase.m_LowFlowTimer < 3)
                    {
                        phase.m_LowFlowTimer++;
                    }
                    else
                    {
                        preferChange = true;
                    }
                    phase.m_LowPriorityTimer = 0;
                }
                else if (phase.m_Priority < MaxPriority(customPhaseDataBuffer))
                {
                    if (phase.m_LowPriorityTimer >= 1)
                    {
                        preferChange = true;
                    }
                    phase.m_LowPriorityTimer++;
                }
                else
                {
                    preferChange = true;
                }
                if ((phase.m_Options & CustomPhaseData.Options.EndPhasePrematurely) != 0)
                {
                    preferChange = true;
                }
                if (customTrafficLights.m_ManualSignalGroup > 0 && customTrafficLights.m_ManualSignalGroup != trafficLights.m_CurrentSignalGroup)
                {
                    preferChange = true;
                }
                customPhaseDataBuffer[currentSignalIndex] = phase;
                byte nextGroup = GetNextSignalGroup(trafficLights.m_CurrentSignalGroup, customPhaseDataBuffer, customTrafficLights, out var linked);
                if (preferChange && nextGroup != trafficLights.m_CurrentSignalGroup)
                {
                    trafficLights.m_State = TrafficLightState.Ending;
                    trafficLights.m_NextSignalGroup = nextGroup;
                    if (linked)
                    {
                        for (int i = trafficLights.m_CurrentSignalGroup; i < trafficLights.m_NextSignalGroup - 1; i++)
                        {
                            CustomPhaseData nextPhase = customPhaseDataBuffer[i];
                            if (nextPhase.m_Priority <= 0)
                            {
                                nextPhase.m_TurnsSinceLastRun = 0;
                                customPhaseDataBuffer[i] = nextPhase;
                            }
                        }
                    }
                    return true;
                }
                return false;
            }
            else if (trafficLights.m_State == TrafficLightState.Ending)
            {
                trafficLights.m_State = TrafficLightState.Changing;
                return true;
            }
            else if (trafficLights.m_State == TrafficLightState.Changing)
            {
                trafficLights.m_State = TrafficLightState.Beginning;
                return true;
            }
            return false;
        }

        public static void CalculateFlow(PatchedTrafficLightSystem.UpdateTrafficLightsJob job, int unfilteredChunkIndex, DynamicBuffer<SubLane> subLaneBuffer, TrafficLights trafficLights, DynamicBuffer<CustomPhaseData> customPhaseDataBuffer)
        {
            float4 timeFactors = job.m_ExtraData.m_TimeFactors * 0.125f;
            for (int i = 0; i < customPhaseDataBuffer.Length; i++)
            {
                CustomPhaseData customPhaseData = customPhaseDataBuffer[i];
                customPhaseData.m_CarFlow.z = customPhaseData.m_CarFlow.y;
                customPhaseData.m_CarFlow.y = customPhaseData.m_CarFlow.x;
                customPhaseData.m_CarFlow.x = 0f;
                customPhaseDataBuffer[i] = customPhaseData;
            }
            foreach (var subLane in subLaneBuffer)
            {
                Entity subLaneEntity = subLane.m_SubLane;
                float4 newDistance = 0f;
                float4 newDuration = 0f;
                float4 oldDistance = 0f;
                float4 oldDuration = 0f;
                float4 diffDistance = 0f;
                float4 diffDuration = 0f;
                uint newFrame = job.m_ExtraData.m_Frame;
                uint oldFrame = 0;
                uint diffFrame = 0;

                if (!job.m_LaneSignalData.TryGetComponent(subLaneEntity, out var laneSignal))
                {
                    continue;
                }
                if (!job.m_ExtraTypeHandle.m_LaneFlow.TryGetComponent(subLaneEntity, out var laneFlow))
                {
                    continue;
                }
                if ((laneSignal.m_GroupMask & (1 << trafficLights.m_CurrentSignalGroup - 1)) == 0)
                {
                    continue;
                }

                newDistance = math.lerp(laneFlow.m_Distance, laneFlow.m_Next.y, timeFactors);
                newDuration = math.lerp(laneFlow.m_Duration, laneFlow.m_Next.x, timeFactors);

                LaneFlowHistory laneFlowHistory = new LaneFlowHistory();
                if (job.m_ExtraTypeHandle.m_LaneFlowHistory.TryGetComponent(subLaneEntity, out laneFlowHistory))
                {
                    oldDistance = laneFlowHistory.m_Distance;
                    oldDuration = laneFlowHistory.m_Duration;
                    oldFrame = laneFlowHistory.m_Frame;
                }
                else
                {
                    job.m_CommandBuffer.AddComponent(unfilteredChunkIndex, subLaneEntity, laneFlowHistory);
                }

                diffDistance = newDistance - oldDistance;
                diffDuration = newDuration - oldDuration;
                diffFrame = newFrame - oldFrame;

                laneFlowHistory.m_Distance = newDistance;
                laneFlowHistory.m_Duration = newDuration;
                laneFlowHistory.m_Frame = newFrame;

                job.m_CommandBuffer.SetComponent(unfilteredChunkIndex, subLaneEntity, laneFlowHistory);

                int group = trafficLights.m_CurrentSignalGroup - 1;
                if (group < customPhaseDataBuffer.Length && diffFrame > 0)
                {
                    CustomPhaseData customPhaseData = customPhaseDataBuffer[group];
                    float totalDiff = math.abs(Max(diffDistance)) + math.abs(Max(diffDuration));
                    customPhaseData.m_CarFlow.x += totalDiff * (64f / (float)diffFrame); // 64 frames per traffic light tick
                    customPhaseDataBuffer[group] = customPhaseData;
                }
            }
        }

        public static void CalculatePriority(PatchedTrafficLightSystem.UpdateTrafficLightsJob job, DynamicBuffer<SubLane> subLaneBuffer, DynamicBuffer<CustomPhaseData> customPhaseDataBuffer)
        {
            for (int i = 0; i < customPhaseDataBuffer.Length; i++)
            {
                CustomPhaseData customPhaseData = customPhaseDataBuffer[i];
                customPhaseData.m_CarLaneOccupied = 0;
                customPhaseData.m_PublicCarLaneOccupied = 0;
                customPhaseData.m_TrackLaneOccupied = 0;
                customPhaseData.m_PedestrianLaneOccupied = 0;
                customPhaseData.m_Priority = 0;
                customPhaseDataBuffer[i] = customPhaseData;
            }
            foreach (var subLane in subLaneBuffer)
            {
                Entity subLaneEntity = subLane.m_SubLane;

                if (!job.m_LaneSignalData.TryGetComponent(subLaneEntity, out var laneSignal))
                {
                    continue;
                }

                Entity lanePetitioner = laneSignal.m_Petitioner;
                int lanePriority = laneSignal.m_Priority;

                laneSignal.m_Petitioner = Entity.Null;
                laneSignal.m_Priority = laneSignal.m_Default;
                job.m_LaneSignalData[subLaneEntity] = laneSignal;

                if (job.m_ExtraTypeHandle.m_MasterLane.HasComponent(subLaneEntity))
                {
                    continue;
                }
                if (lanePetitioner == Entity.Null)
                {
                    continue;
                }

                for (int i = 0; i < customPhaseDataBuffer.Length; i++)
                {
                    if ((laneSignal.m_GroupMask & (1 << i)) == 0)
                    {
                        continue;
                    }

                    CustomPhaseData customPhaseData = customPhaseDataBuffer[i];

                    if (job.m_ExtraTypeHandle.m_CarLane.HasComponent(subLaneEntity))
                    {
                        customPhaseData.m_CarLaneOccupied++;
                        if (job.m_ExtraTypeHandle.m_ExtraLaneSignal.TryGetComponent(subLaneEntity, out var extraLaneSignal))
                        {
                            if (extraLaneSignal.m_SourceSubLane != Entity.Null && job.m_ExtraTypeHandle.m_CarLane.TryGetComponent(extraLaneSignal.m_SourceSubLane, out var sourceCarLane))
                            {
                                if ((sourceCarLane.m_Flags & CarLaneFlags.PublicOnly) != 0)
                                {
                                    customPhaseData.m_PublicCarLaneOccupied++;
                                    if ((customPhaseData.m_Options & CustomPhaseData.Options.PrioritisePublicCar) != 0)
                                    {
                                        lanePriority = math.max(lanePriority, 104); // 104 is the priority for trams
                                    }
                                    else
                                    {
                                        lanePriority = math.min(lanePriority, 100); // 100 is the default priority
                                    }
                                }
                            }
                        }
                    }
                    if (job.m_ExtraTypeHandle.m_TrackLane.HasComponent(subLaneEntity))
                    {
                        customPhaseData.m_TrackLaneOccupied++;
                        if ((customPhaseData.m_Options & CustomPhaseData.Options.PrioritiseTrack) == 0)
                        {
                            // Do not lower priority for trains, as they do not stop for signals
                            // 110 is the priority for trains
                            if (lanePriority < 110)
                            {
                                lanePriority = math.min(lanePriority, 100); // 100 is the default priority
                            }
                        }
                    }
                    if (job.m_ExtraTypeHandle.m_PedestrianLane.TryGetComponent(subLaneEntity, out var pedestrianLane))
                    {
                        if ((pedestrianLane.m_Flags & PedestrianLaneFlags.Crosswalk) != 0)
                        {
                            customPhaseData.m_PedestrianLaneOccupied++;
                            if ((customPhaseData.m_Options & CustomPhaseData.Options.PrioritisePedestrian) != 0)
                            {
                                lanePriority = math.max(lanePriority, 104); // 104 is the priority for trams
                            }
                        }
                    }

                    customPhaseData.m_Priority = math.max(customPhaseData.m_Priority, lanePriority);

                    customPhaseDataBuffer[i] = customPhaseData;
                }
            }
        }

        public static byte GetNextSignalGroup(byte currentGroup, DynamicBuffer<CustomPhaseData> customPhaseDataBuffer, CustomTrafficLights customTrafficLights, out bool linked)
        {
            linked = false;
            byte nextGroup = 0;
            int maxPriority = -1;
            float maxWaiting = -1;
            if (customTrafficLights.m_ManualSignalGroup > 0 && customTrafficLights.m_ManualSignalGroup - 1 < customPhaseDataBuffer.Length)
            {
                return customTrafficLights.m_ManualSignalGroup;
            }
            for (int i = 0; i < customPhaseDataBuffer.Length; i++)
            {
                CustomPhaseData phase = customPhaseDataBuffer[i];
                float weightedWaiting = ((float)phase.TotalLaneOccupied()) * phase.m_LaneOccupiedMultiplier * math.pow((float)phase.m_TurnsSinceLastRun / (float)customPhaseDataBuffer.Length, phase.m_IntervalExponent);
                if (phase.m_Priority > maxPriority)
                {
                    nextGroup = (byte)(i + 1);
                    maxPriority = phase.m_Priority;
                    maxWaiting = weightedWaiting;
                }
                else if (phase.m_Priority == maxPriority && weightedWaiting > maxWaiting)
                {
                    nextGroup = (byte)(i + 1);
                    maxWaiting = weightedWaiting;
                }
                phase.m_WeightedWaiting = weightedWaiting;
                customPhaseDataBuffer[i] = phase;
            }

            int linkedPriority = -1;
            byte linkedNextGroup = 0;
            for (int i = currentGroup - 1; i >= 0 && i < customPhaseDataBuffer.Length - 1; i++)
            {
                CustomPhaseData phase = customPhaseDataBuffer[i];
                if ((phase.m_Options & CustomPhaseData.Options.LinkedWithNextPhase) == 0)
                {
                    break;
                }

                CustomPhaseData nextPhase = customPhaseDataBuffer[i + 1];
                if (linkedNextGroup == 0 && nextPhase.m_Priority > 0)
                {
                    linkedNextGroup = (byte)(i + 2);
                }
                linkedPriority = math.max(linkedPriority, nextPhase.m_Priority);
            }
            if (linkedNextGroup > 0 && linkedPriority >= maxPriority)
            {
                linked = true;
                return linkedNextGroup;
            }

            for (int i = nextGroup - 2; i >= 0; i--)
            {
                CustomPhaseData phase = customPhaseDataBuffer[i];
                if ((phase.m_Options & CustomPhaseData.Options.LinkedWithNextPhase) == 0)
                {
                    break;
                }
                if (phase.m_Priority > 0)
                {
                    nextGroup = (byte)(i + 1);
                }
            }
            return nextGroup;
        }

        private static int MaxPriority(DynamicBuffer<CustomPhaseData> customPhaseDataBuffer)
        {
            int max = int.MinValue;
            foreach (var phase in customPhaseDataBuffer)
            {
                max = math.max(max, phase.m_Priority);
            }
            return max;
        }

        private static float Max(float4 f)
        {
            return math.max(f.w, math.max(f.x, math.max(f.y, f.z)));
        }
    }
}