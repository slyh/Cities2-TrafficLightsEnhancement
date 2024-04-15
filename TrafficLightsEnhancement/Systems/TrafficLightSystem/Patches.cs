using HarmonyLib;

namespace C2VM.TrafficLightsEnhancement.Systems.TrafficLightSystem;

[HarmonyPatch]
class Patches
{
    [HarmonyPatch(typeof(Game.Simulation.TrafficLightSystem), "OnCreate")]
    [HarmonyPrefix]
    static bool OnCreate(Game.Simulation.TrafficLightSystem __instance)
    {
        return false;
    }

    [HarmonyPatch(typeof(Game.Simulation.TrafficLightSystem), "OnUpdate")]
    [HarmonyPrefix]
    static bool OnUpdate(Game.Simulation.TrafficLightSystem __instance)
    {
        return false;
    }
}