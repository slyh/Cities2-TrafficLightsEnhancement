using System.Runtime.CompilerServices;
using C2VM.TrafficLightsEnhancement.Components;
using Game.Net;
using Unity.Entities;

namespace C2VM.TrafficLightsEnhancement.Systems.TrafficLightInitializationSystem;

public struct ExtraTypeHandle
{
    public EntityTypeHandle m_Entity;

    public BufferTypeHandle<ConnectedEdge> m_ConnectedEdge;

    public ComponentLookup<Edge> m_Edge;

    public ComponentLookup<EdgeGeometry> m_EdgeGeometry;

    public ComponentLookup<Lane> m_Lane;

    public ComponentLookup<PedestrianLane> m_PedestrianLane;

    public BufferLookup<SubLane> m_SubLane;

    public ComponentLookup<TrackLane> m_TrackLane;

    public ComponentTypeHandle<CustomTrafficLights> m_CustomTrafficLights;

    public ComponentLookup<ExtraLaneSignal> m_ExtraLaneSignal;

    public BufferTypeHandle<CustomPhaseGroupMask> m_CustomPhaseGroupMask;

    public BufferTypeHandle<CustomPhaseData> m_CustomPhaseData;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void AssignHandles(ref SystemState state)
    {
        m_Entity = state.GetEntityTypeHandle();
        m_ConnectedEdge = state.GetBufferTypeHandle<ConnectedEdge>();
        m_Edge = state.GetComponentLookup<Edge>();
        m_EdgeGeometry = state.GetComponentLookup<EdgeGeometry>();
        m_Lane = state.GetComponentLookup<Lane>();
        m_PedestrianLane = state.GetComponentLookup<PedestrianLane>();
        m_SubLane = state.GetBufferLookup<SubLane>();
        m_TrackLane = state.GetComponentLookup<TrackLane>();
        m_CustomTrafficLights = state.GetComponentTypeHandle<CustomTrafficLights>();
        m_ExtraLaneSignal = state.GetComponentLookup<ExtraLaneSignal>();
        m_CustomPhaseGroupMask = state.GetBufferTypeHandle<CustomPhaseGroupMask>();
        m_CustomPhaseData = state.GetBufferTypeHandle<CustomPhaseData>();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Update(ref SystemState state)
    {
        m_Entity.Update(ref state);
        m_ConnectedEdge.Update(ref state);
        m_Edge.Update(ref state);
        m_EdgeGeometry.Update(ref state);
        m_Lane.Update(ref state);
        m_PedestrianLane.Update(ref state);
        m_SubLane.Update(ref state);
        m_TrackLane.Update(ref state);
        m_CustomTrafficLights.Update(ref state);
        m_ExtraLaneSignal.Update(ref state);
        m_CustomPhaseGroupMask.Update(ref state);
        m_CustomPhaseData.Update(ref state);
    }
}