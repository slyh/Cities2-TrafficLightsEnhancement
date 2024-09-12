using System.Runtime.CompilerServices;
using C2VM.TrafficLightsEnhancement.Components;
using Unity.Collections;
using Unity.Entities;

namespace C2VM.TrafficLightsEnhancement.Systems.TrafficLightSystem;

public struct ExtraTypeHandle
{
    [ReadOnly]
    public ComponentTypeHandle<CustomTrafficLights> m_CustomTrafficLights;

    [ReadOnly]
    public ComponentLookup<ExtraLaneSignal> m_ExtraLaneSignal;

    [ReadOnly]
    public BufferTypeHandle<CustomPhaseData> m_CustomPhaseData;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void AssignHandles(ref SystemState state)
    {
        m_CustomTrafficLights = state.GetComponentTypeHandle<CustomTrafficLights>(true);
        m_ExtraLaneSignal = state.GetComponentLookup<ExtraLaneSignal>(true);
        m_CustomPhaseData = state.GetBufferTypeHandle<CustomPhaseData>(true);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Update(ref SystemState state)
    {
        m_CustomTrafficLights.Update(ref state);
        m_ExtraLaneSignal.Update(ref state);
        m_CustomPhaseData.Update(ref state);
    }
}