using C2VM.CommonLibraries.LaneSystem;
using Colossal.Serialization.Entities;
using Game;
using Game.PSI;
using Unity.Entities;

namespace C2VM.TrafficLightsEnhancement.Systems.UI;

public partial class LdtRetirementSystem : GameSystemBase
{
    public static readonly string kRetirementNoticeLink = "https://github.com/slyh/Cities2-TrafficLightsEnhancement/discussions/96";

    public int m_UnmigratedNodeCount = 0;

    protected override void OnCreate()
    {
        base.OnCreate();
        ShowMigrationNotification();
    }

    protected override void OnUpdate()
    {
    }

    protected void ShowMigrationNotification()
    {
        if (Mod.m_Settings != null && Mod.m_Settings.m_HasReadLdtRetirementNotice)
        {
            return;
        }
        NotificationSystem.Push(
            identifier: "C2VM.TLE.LdtMigrationNotification",
            titleId: "C2VM.TLE.LdtMigrationNotificationTitle",
            textId: "C2VM.TLE.LdtMigrationNotificationText",
            thumbnail: "https://modscontent.paradox-interactive.com/cities_skylines_2/4fe3685f-92d4-4386-8c73-c7f19ab80916/content/cover.jpg",
            progressState: Colossal.PSI.Common.ProgressState.Warning,
            onClicked: OpenMigrationWebPage
        );
    }

    public static void OpenMigrationWebPage()
    {
        System.Diagnostics.Process.Start(kRetirementNoticeLink);
        NotificationSystem.Pop(identifier: "C2VM.TLE.LdtMigrationNotification");
        if (Mod.m_Settings != null)
        {
            Mod.m_Settings.m_HasReadLdtRetirementNotice = true;
            Mod.m_Settings.Apply();
        }
    }

    protected override void OnGameLoadingComplete(Purpose purpose, GameMode mode)
    {
        EntityQuery entityQuery = EntityManager.CreateEntityQuery(ComponentType.ReadOnly<CustomLaneDirection>());
        m_UnmigratedNodeCount = entityQuery.CalculateEntityCount();
    }
}