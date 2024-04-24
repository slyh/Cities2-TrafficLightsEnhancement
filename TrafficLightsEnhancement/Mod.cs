using System.Reflection;
using Colossal.Logging;
using Game;
using Game.Modding;
using Game.SceneFlow;
using HarmonyLib;

namespace C2VM.TrafficLightsEnhancement;

public class Mod : IMod
{
    public static readonly string id = typeof(Mod).Assembly.GetName().Name;

    public static readonly string informationalVersion = ((AssemblyInformationalVersionAttribute) System.Attribute.GetCustomAttribute(Assembly.GetAssembly(typeof(Mod)), typeof(AssemblyInformationalVersionAttribute))).InformationalVersion;

    public static readonly ILog log = LogManager.GetLogger($"{id}.{nameof(Mod)}").SetShowsErrorsInUI(false);

    public static C2VM.TrafficLightsEnhancement.Settings settings;

    public void OnLoad(UpdateSystem updateSystem)
    {
        log.Info($"Loading {id} v{informationalVersion}");

        if (GameManager.instance.modManager.TryGetExecutableAsset(this, out var asset))
        {
            log.Info($"Current mod asset at {asset.path}");
        }

        var harmony = new Harmony(id);
        harmony.PatchAll();

        settings = new Settings(this);
        settings.RegisterInOptionsUI();
        Colossal.IO.AssetDatabase.AssetDatabase.global.LoadSettings(typeof(Settings).GetCustomAttribute<Colossal.IO.AssetDatabase.FileLocationAttribute>().fileName, settings);

        updateSystem.UpdateBefore<C2VM.TrafficLightsEnhancement.Systems.TrafficLightInitializationSystem.PatchedTrafficLightInitializationSystem, Game.Net.TrafficLightInitializationSystem>(Game.SystemUpdatePhase.Modification4B);
        updateSystem.UpdateBefore<C2VM.TrafficLightsEnhancement.Systems.TrafficLightSystem.PatchedTrafficLightSystem, Game.Simulation.TrafficLightSystem>(Game.SystemUpdatePhase.GameSimulation);

        updateSystem.World.GetOrCreateSystem<C2VM.TrafficLightsEnhancement.Systems.UISystem.UISystem>();
    }

    public void OnDispose()
    {
        log.Info(nameof(OnDispose));
    }
}