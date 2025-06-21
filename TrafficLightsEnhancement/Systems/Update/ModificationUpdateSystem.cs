using Game;
using Game.Common;
using Unity.Entities;

namespace C2VM.TrafficLightsEnhancement.Systems.Update;

public partial class ModificationUpdateSystem : GameSystemBase
{
    private C2VM.TrafficLightsEnhancement.Systems.UI.UISystem m_UISystem;

    protected override void OnCreate()
    {
        base.OnCreate();
        m_UISystem = World.GetOrCreateSystemManaged<C2VM.TrafficLightsEnhancement.Systems.UI.UISystem>();
    }

    protected override void OnUpdate()
    {
        if (m_UISystem.m_SelectedEntity != Entity.Null && EntityManager.HasComponent<Updated>(m_UISystem.m_SelectedEntity))
        {
            if (EntityManager.HasComponent<Game.Net.TrafficLights>(m_UISystem.m_SelectedEntity))
            {
                m_UISystem.RedrawGizmo();
                m_UISystem.UpdateEdgeInfo(m_UISystem.m_SelectedEntity);
            }
            else
            {
                m_UISystem.ChangeSelectedEntity(Entity.Null);
            }
        }
    }
}