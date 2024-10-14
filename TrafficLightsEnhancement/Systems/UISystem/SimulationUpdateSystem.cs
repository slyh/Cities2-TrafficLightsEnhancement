using Colossal.Entities;
using Game;
using Game.Simulation;
using Unity.Entities;

namespace C2VM.TrafficLightsEnhancement.Systems.UISystem;

public partial class SimulationUpdateSystem : GameSystemBase
{
    private Game.Simulation.SimulationSystem m_SimulationSystem;

    private UISystem m_UISystem;

    protected override void OnCreate()
    {
        base.OnCreate();
        m_SimulationSystem = World.GetOrCreateSystemManaged<Game.Simulation.SimulationSystem>();
        m_UISystem = World.GetOrCreateSystemManaged<UISystem>();
    }

    protected override void OnUpdate()
    {
        if (m_UISystem.m_SelectedEntity != Entity.Null && EntityManager.TryGetSharedComponent<UpdateFrame>(m_UISystem.m_SelectedEntity, out var updateFrame))
        {
            if (updateFrame.m_Index == SimulationUtils.GetUpdateFrameWithInterval(m_SimulationSystem.frameIndex, 4, 16))
            {
                m_UISystem.SimulationUpdate();
            }
        }
    }
}