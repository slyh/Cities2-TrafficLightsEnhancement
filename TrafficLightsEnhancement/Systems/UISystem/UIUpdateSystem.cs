using Game;
using Game.Common;
using Unity.Entities;

namespace C2VM.TrafficLightsEnhancement.Systems.UISystem;

public partial class UIUpdateSystem : GameSystemBase
{
    private UISystem m_UISystem;

    protected override void OnCreate()
    {
        base.OnCreate();
        m_UISystem = World.GetOrCreateSystemManaged<UISystem>();
    }

    protected override void OnUpdate()
    {
        if (m_UISystem.m_SelectedEntity != Entity.Null && EntityManager.HasComponent<Updated>(m_UISystem.m_SelectedEntity))
        {
            m_UISystem.RedrawGizmo();
        }
    }
}