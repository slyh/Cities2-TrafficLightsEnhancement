using System.Reflection;
using Colossal.Logging;
using Game;
using Game.Modding;
using Game.SceneFlow;
using Unity.Collections;
using Unity.Entities;

namespace C2VM.TrafficLightsEnhancement;

public class Mod : IMod
{
    public static readonly string m_Id = typeof(Mod).Assembly.GetName().Name;

    public static readonly string m_InformationalVersion = ((AssemblyInformationalVersionAttribute)System.Attribute.GetCustomAttribute(Assembly.GetAssembly(typeof(Mod)), typeof(AssemblyInformationalVersionAttribute))).InformationalVersion;

    public static readonly ILog m_Log = LogManager.GetLogger($"{m_Id}.{nameof(Mod)}").SetShowsErrorsInUI(false);

    public static C2VM.TrafficLightsEnhancement.Settings m_Settings;

    public static World m_World;

    private static Game.Net.TrafficLightInitializationSystem m_TrafficLightInitializationSystem;

    private static Game.Simulation.TrafficLightSystem m_TrafficLightSystem;

    private static C2VM.TrafficLightsEnhancement.Systems.TrafficLightSystems.Initialisation.PatchedTrafficLightInitializationSystem m_PatchedTrafficLightInitializationSystem;

    private static C2VM.TrafficLightsEnhancement.Systems.TrafficLightSystems.Simulation.PatchedTrafficLightSystem m_PatchedTrafficLightSystem;

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

        m_TrafficLightInitializationSystem = m_World.GetOrCreateSystemManaged<Game.Net.TrafficLightInitializationSystem>();
        m_TrafficLightSystem = m_World.GetOrCreateSystemManaged<Game.Simulation.TrafficLightSystem>();
        m_PatchedTrafficLightInitializationSystem = m_World.GetOrCreateSystemManaged<C2VM.TrafficLightsEnhancement.Systems.TrafficLightSystems.Initialisation.PatchedTrafficLightInitializationSystem>();
        m_PatchedTrafficLightSystem = m_World.GetOrCreateSystemManaged<C2VM.TrafficLightsEnhancement.Systems.TrafficLightSystems.Simulation.PatchedTrafficLightSystem>();

        m_Settings = new Settings(this);

        SystemSetup(updateSystem);

        string netToolSystemToolID = m_World.GetOrCreateSystemManaged<Game.Tools.NetToolSystem>().toolID;
        Assert(netToolSystemToolID == "Net Tool", $"netToolSystemToolID: {netToolSystemToolID}");
    }

    public void OnDispose()
    {
        m_Log.Info(nameof(OnDispose));
    }

    public void SystemSetup(UpdateSystem updateSystem)
    {
        m_World.GetOrCreateSystemManaged<Game.Tools.NetToolSystem>(); // Ensure NetToolSystem is created before our tool

        var noneList = new NativeList<ComponentType>(1, Allocator.Temp);
        noneList.Add(ComponentType.ReadOnly<Components.CustomTrafficLights>());

        Utils.EntityQueryUtils.UpdateEntityQuery(m_TrafficLightInitializationSystem, "m_TrafficLightsQuery", noneList);
        Utils.EntityQueryUtils.UpdateEntityQuery(m_TrafficLightSystem, "m_TrafficLightQuery", noneList);

        updateSystem.UpdateBefore<C2VM.TrafficLightsEnhancement.Systems.TrafficLightSystems.Initialisation.PatchedTrafficLightInitializationSystem, Game.Net.TrafficLightInitializationSystem>(SystemUpdatePhase.Modification4B);
        updateSystem.UpdateBefore<C2VM.TrafficLightsEnhancement.Systems.TrafficLightSystems.Simulation.PatchedTrafficLightSystem, Game.Simulation.TrafficLightSystem>(SystemUpdatePhase.GameSimulation);
        updateSystem.UpdateAt<C2VM.TrafficLightsEnhancement.Systems.UI.UISystem>(SystemUpdatePhase.UIUpdate);
        updateSystem.UpdateAt<C2VM.TrafficLightsEnhancement.Systems.Tool.ToolSystem>(SystemUpdatePhase.ToolUpdate);
        updateSystem.UpdateAt<C2VM.TrafficLightsEnhancement.Systems.Update.ModificationUpdateSystem>(SystemUpdatePhase.ModificationEnd);
        updateSystem.UpdateAfter<C2VM.TrafficLightsEnhancement.Systems.Update.SimulationUpdateSystem>(SystemUpdatePhase.GameSimulation);

        SetCompatibilityMode(m_Settings != null && m_Settings.m_CompatibilityMode);
    }

    public static void SetCompatibilityMode(bool enable)
    {
        m_TrafficLightInitializationSystem.Enabled = enable;
        m_TrafficLightSystem.Enabled = enable;

        m_PatchedTrafficLightInitializationSystem.SetCompatibilityMode(enable);
        m_PatchedTrafficLightSystem.SetCompatibilityMode(enable);

        m_Log.Info($"Compatibility mode is set to {enable}.");
    }

    public static bool IsCanary()
    {
        #if SHOW_CANARY_BUILD_WARNING
        return true;
        #else
        return false;
        #endif
    }

    public static void Assert(bool condition, string message = "", bool showInUI = false, [System.Runtime.CompilerServices.CallerArgumentExpression(nameof(condition))] string expression = "")
    {
        if (condition == true)
        {
            return;
        }
        bool showsErrorsInUI = m_Log.showsErrorsInUI;
        m_Log.SetShowsErrorsInUI(showInUI);
        m_Log.Error($"Assertion failed!\n{message}\nExpression: {expression}");
        m_Log.SetShowsErrorsInUI(showsErrorsInUI);
    }
}