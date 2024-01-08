using HarmonyLib;
using Unity.Collections;
using Unity.Entities;

namespace C2VM.TrafficLightsEnhancement.Systems.TrafficLightInitializationSystem;

[HarmonyPatch]
class Patches
{
    [HarmonyPatch(typeof(Game.Common.SystemOrder), "Initialize")]
    [HarmonyPostfix]
    static void Initialize(Game.UpdateSystem updateSystem)
    {
        updateSystem.UpdateAt<PatchedTrafficLightInitializationSystem>(Game.SystemUpdatePhase.Modification4B);
    }

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