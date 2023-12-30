using HarmonyLib;

namespace C2VM.TrafficLightsEnhancement.Systems.TrafficLightSystem;

[HarmonyPatch]
class Patches
{
    [HarmonyPatch(typeof(Game.Common.SystemOrder), "Initialize")]
    [HarmonyPostfix]
    static void Initialize(Game.UpdateSystem updateSystem)
    {
        updateSystem.UpdateAt<PatchedTrafficLightSystem>(Game.SystemUpdatePhase.GameSimulation);
    }

    [HarmonyPatch(typeof(Game.Simulation.TrafficLightSystem), "OnCreate")]
    [HarmonyPrefix]
    static bool OnCreate()
    {
        return false;
    }

    [HarmonyPatch(typeof(Game.Simulation.TrafficLightSystem), "OnCreateForCompiler")]
    [HarmonyPrefix]
    static bool OnCreateForCompiler()
    {
        return false;
    }

    [HarmonyPatch(typeof(Game.Simulation.TrafficLightSystem), "OnUpdate")]
    [HarmonyPrefix]
    static bool OnUpdate()
    {
        return false;
    }
}