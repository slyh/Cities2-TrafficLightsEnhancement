using HarmonyLib;

namespace C2VM.TrafficLightsEnhancement.Systems.TrafficLightInitializationSystem;

[HarmonyPatch]
class Patches
{
    [HarmonyPatch(typeof(Game.Net.TrafficLightInitializationSystem), "OnCreate")]
    [HarmonyPrefix]
    static bool OnCreate(Game.Net.TrafficLightInitializationSystem __instance)
    {
        return false;
    }

    [HarmonyPatch(typeof(Game.Net.TrafficLightInitializationSystem), "OnUpdate")]
    [HarmonyPrefix]
    static bool OnUpdate(Game.Net.TrafficLightInitializationSystem __instance)
    {
        return false;
    }
}