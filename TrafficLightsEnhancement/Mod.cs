using System.Reflection;
using Colossal.Logging;
using Game;
using Game.Modding;
using Game.SceneFlow;
using HarmonyLib;

namespace C2VM.TrafficLightsEnhancement;

public class Mod : IMod
{
    public static readonly string m_Id = typeof(Mod).Assembly.GetName().Name;

    public static readonly string m_InformationalVersion = ((AssemblyInformationalVersionAttribute) System.Attribute.GetCustomAttribute(Assembly.GetAssembly(typeof(Mod)), typeof(AssemblyInformationalVersionAttribute))).InformationalVersion;

    public static readonly ILog m_Log = LogManager.GetLogger($"{m_Id}.{nameof(Mod)}").SetShowsErrorsInUI(false);

    public static C2VM.TrafficLightsEnhancement.Settings m_Settings;

    public static Unity.Entities.World m_World;

    private Harmony m_Harmony;

    public void OnLoad(UpdateSystem updateSystem)
    {
        m_Log.Info($"Loading {m_Id} v{m_InformationalVersion}");

        var outdatedType = System.Type.GetType("C2VM.TrafficLightsEnhancement.Plugin, C2VM.TrafficLightsEnhancement") ?? System.Type.GetType("C2VM.CommonLibraries.LaneSystem.Plugin, C2VM.CommonLibraries.LaneSystem");
        if (outdatedType != null)
        {
            throw new System.Exception($"An outdated version of Traffic Lights Enhancement has been detected at {outdatedType.Assembly.Location}");
        }

        if (GameManager.instance.modManager.TryGetExecutableAsset(this, out var asset))
        {
            m_Log.Info($"Current mod asset at {asset.path}");
        }

        m_World = updateSystem.World;

        m_Harmony = new Harmony(m_Id);
        m_Harmony.PatchAll();

        m_Settings = new Settings(this);

        m_World.GetOrCreateSystemManaged<Game.Net.TrafficLightInitializationSystem>().Enabled = false;
        m_World.GetOrCreateSystemManaged<Game.Simulation.TrafficLightSystem>().Enabled = false;

        updateSystem.UpdateBefore<C2VM.TrafficLightsEnhancement.Systems.TrafficLightSystems.Initialisation.PatchedTrafficLightInitializationSystem, Game.Net.TrafficLightInitializationSystem>(Game.SystemUpdatePhase.Modification4B);
        updateSystem.UpdateBefore<C2VM.TrafficLightsEnhancement.Systems.TrafficLightSystems.Simulation.PatchedTrafficLightSystem, Game.Simulation.TrafficLightSystem>(Game.SystemUpdatePhase.GameSimulation);
        updateSystem.UpdateAt<C2VM.TrafficLightsEnhancement.Systems.UI.UISystem>(SystemUpdatePhase.UIUpdate);
        updateSystem.UpdateAt<C2VM.TrafficLightsEnhancement.Systems.Tool.ToolSystem>(SystemUpdatePhase.ToolUpdate);
        updateSystem.UpdateAt<C2VM.TrafficLightsEnhancement.Systems.Update.ModificationUpdateSystem>(SystemUpdatePhase.ModificationEnd);
        updateSystem.UpdateAfter<C2VM.TrafficLightsEnhancement.Systems.Update.SimulationUpdateSystem>(SystemUpdatePhase.GameSimulation);

        m_World.GetOrCreateSystemManaged<C2VM.TrafficLightsEnhancement.Systems.UI.LdtRetirementSystem>();
    }

    public void OnDispose()
    {
        m_Log.Info(nameof(OnDispose));
        m_Harmony?.UnpatchAll(m_Id);
        m_Settings?.UnregisterInOptionsUI();

        m_World.GetOrCreateSystemManaged<C2VM.TrafficLightsEnhancement.Systems.TrafficLightSystems.Initialisation.PatchedTrafficLightInitializationSystem>().Enabled = false;
        m_World.GetOrCreateSystemManaged<C2VM.TrafficLightsEnhancement.Systems.TrafficLightSystems.Simulation.PatchedTrafficLightSystem>().Enabled = false;
        m_World.GetOrCreateSystemManaged<C2VM.TrafficLightsEnhancement.Systems.UI.UISystem>().Enabled = false;
        m_World.GetOrCreateSystemManaged<C2VM.TrafficLightsEnhancement.Systems.Tool.ToolSystem>().Enabled = false;
        m_World.GetOrCreateSystemManaged<C2VM.TrafficLightsEnhancement.Systems.Update.ModificationUpdateSystem>().Enabled = false;
        m_World.GetOrCreateSystemManaged<C2VM.TrafficLightsEnhancement.Systems.Update.SimulationUpdateSystem>().Enabled = false;

        m_World.GetOrCreateSystemManaged<Game.Net.TrafficLightInitializationSystem>().Enabled = true;
        m_World.GetOrCreateSystemManaged<Game.Simulation.TrafficLightSystem>().Enabled = true;
    }

    public static bool IsCanary()
    {
        #if SHOW_CANARY_BUILD_WARNING
        return true;
        #else
        return false;
        #endif
    }
}