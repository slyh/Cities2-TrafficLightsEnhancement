using System.Runtime.CompilerServices;
using C2VM.TrafficLightsEnhancement.Components;
using Game.Net;
using Unity.Collections;
using Unity.Entities;

namespace C2VM.TrafficLightsEnhancement.Systems.TrafficLightSystems.Simulation;

public struct ExtraTypeHandle
{
    [ReadOnly]
    public ComponentTypeHandle<CustomTrafficLights> m_CustomTrafficLights;

    [ReadOnly]
    public ComponentLookup<ExtraLaneSignal> m_ExtraLaneSignal;

    public BufferTypeHandle<CustomPhaseData> m_CustomPhaseData;

    [ReadOnly]
    public ComponentLookup<LaneFlow> m_LaneFlow;

    public ComponentLookup<LaneFlowHistory> m_LaneFlowHistory;

    [ReadOnly]
    public ComponentLookup<MasterLane> m_MasterLane;

    [ReadOnly]
    public ComponentLookup<CarLane> m_CarLane;

    [ReadOnly]
    public ComponentLookup<TrackLane> m_TrackLane;

    [ReadOnly]
    public ComponentLookup<PedestrianLane> m_PedestrianLane;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void AssignHandles(ref SystemState state)
    {
        m_CustomTrafficLights = state.GetComponentTypeHandle<CustomTrafficLights>(true);
        m_ExtraLaneSignal = state.GetComponentLookup<ExtraLaneSignal>(true);
        m_CustomPhaseData = state.GetBufferTypeHandle<CustomPhaseData>(false);
        m_LaneFlow = state.GetComponentLookup<LaneFlow>(true);
        m_LaneFlowHistory = state.GetComponentLookup<LaneFlowHistory>(false);
        m_MasterLane = state.GetComponentLookup<MasterLane>(true);
        m_CarLane = state.GetComponentLookup<CarLane>(true);
        m_TrackLane = state.GetComponentLookup<TrackLane>(true);
        m_PedestrianLane = state.GetComponentLookup<PedestrianLane>(true);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ExtraTypeHandle Update(ref SystemState state)
    {
        m_CustomTrafficLights.Update(ref state);
        m_ExtraLaneSignal.Update(ref state);
        m_CustomPhaseData.Update(ref state);
        m_LaneFlow.Update(ref state);
        m_LaneFlowHistory.Update(ref state);
        m_MasterLane.Update(ref state);
        m_CarLane.Update(ref state);
        m_TrackLane.Update(ref state);
        m_PedestrianLane.Update(ref state);
        return this;
    }
}