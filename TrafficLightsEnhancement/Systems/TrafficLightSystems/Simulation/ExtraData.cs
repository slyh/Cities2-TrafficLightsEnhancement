using Unity.Mathematics;

namespace C2VM.TrafficLightsEnhancement.Systems.TrafficLightSystems.Simulation
{
    public struct ExtraData
    {
        public uint m_Frame;

        public float4 m_TimeFactors;

        public ExtraData(PatchedTrafficLightSystem system)
        {
            float num = system.m_TimeSystem.normalizedTime * 4f;
            float4 x = new float4(math.max(num - 3f, 1f - num), 1f - math.abs(num - new float3(1f, 2f, 3f)));
            x = math.saturate(x);
            m_TimeFactors = x;
            m_Frame = system.m_SimulationSystem.frameIndex;
        }
    }
}