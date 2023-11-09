using HarmonyLib;

namespace TrafficLightImprovements;

[HarmonyPatch]
class Patches
{
    [HarmonyPatch(typeof(Game.Net.TrafficLightInitializationSystem), "OnCreate")]
    [HarmonyPrefix]
    static bool OnCreate(Game.Net.TrafficLightInitializationSystem __instance)
    {
        __instance.World.GetOrCreateSystemManaged<TrafficLightImprovements.TrafficLightInitializationSystem>();
        __instance.World.GetOrCreateSystemManaged<Game.UpdateSystem>().UpdateAt<TrafficLightImprovements.TrafficLightInitializationSystem>(Game.SystemUpdatePhase.GameSimulation);
        return true; // Allow the original method to run so that we only receive update requests when necessary
    }

    [HarmonyPatch(typeof(Game.Net.TrafficLightInitializationSystem), "OnCreateForCompiler")]
    [HarmonyPrefix]
    static bool OnCreateForCompiler()
    {
        return false;
    }

    [HarmonyPatch(typeof(Game.Net.TrafficLightInitializationSystem), "OnUpdate")]
    [HarmonyPrefix]
    static bool OnUpdate(Game.Net.TrafficLightInitializationSystem __instance)
    {
        // For some reason, the cloned TrafficLightInitializationSystem never receives calls to update. So we have to do it manually.
        // Could be something with the EntityQuery. I'm not able to find out the reason behind it.
        __instance.World.GetOrCreateSystemManaged<TrafficLightImprovements.TrafficLightInitializationSystem>().Update();
        return false;
    }
}