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

    public static C2VM.TrafficLightsEnhancement.Systems.UISystem.UISystem m_UISystem;

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

        var harmony = new Harmony(m_Id);
        harmony.PatchAll();

        m_Settings = new Settings(this);
        m_Settings.RegisterInOptionsUI();

        updateSystem.World.GetOrCreateSystemManaged<Game.Net.TrafficLightInitializationSystem>().Enabled = false;
        updateSystem.World.GetOrCreateSystemManaged<Game.Simulation.TrafficLightSystem>().Enabled = false;

        updateSystem.UpdateBefore<C2VM.TrafficLightsEnhancement.Systems.TrafficLightInitializationSystem.PatchedTrafficLightInitializationSystem, Game.Net.TrafficLightInitializationSystem>(Game.SystemUpdatePhase.Modification4B);
        updateSystem.UpdateBefore<C2VM.TrafficLightsEnhancement.Systems.TrafficLightSystem.PatchedTrafficLightSystem, Game.Simulation.TrafficLightSystem>(Game.SystemUpdatePhase.GameSimulation);

        Colossal.IO.AssetDatabase.AssetDatabase.global.LoadSettings(typeof(Settings).GetCustomAttribute<Colossal.IO.AssetDatabase.FileLocationAttribute>().fileName, m_Settings);
        C2VM.TrafficLightsEnhancement.Systems.UISystem.UISystem.UpdateLocale();

        updateSystem.World.GetOrCreateSystemManaged<C2VM.TrafficLightsEnhancement.Systems.UISystem.LDTRetirementSystem>();

        m_UISystem = updateSystem.World.GetOrCreateSystemManaged<C2VM.TrafficLightsEnhancement.Systems.UISystem.UISystem>();
        updateSystem.UpdateAt<C2VM.TrafficLightsEnhancement.Systems.UISystem.UISystem>(SystemUpdatePhase.UIUpdate);
        updateSystem.UpdateAt<C2VM.TrafficLightsEnhancement.Systems.UISystem.UIUpdateSystem>(SystemUpdatePhase.ModificationEnd);
    }

    public void OnDispose()
    {
        m_Log.Info(nameof(OnDispose));
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