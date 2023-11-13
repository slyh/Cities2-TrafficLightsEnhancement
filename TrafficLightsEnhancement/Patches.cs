using HarmonyLib;

namespace TrafficLightsEnhancement;

[HarmonyPatch]
class Patches
{
    [HarmonyPatch(typeof(Game.Net.TrafficLightInitializationSystem), "OnCreate")]
    [HarmonyPrefix]
    static bool TrafficLightInitializationSystemOnCreate(Game.Net.TrafficLightInitializationSystem __instance)
    {
        __instance.World.GetOrCreateSystemManaged<PatchedClasses.TrafficLightInitializationSystem>();
        __instance.World.GetOrCreateSystemManaged<Game.UpdateSystem>().UpdateAt<PatchedClasses.TrafficLightInitializationSystem>(Game.SystemUpdatePhase.GameSimulation);
        return true; // Allow the original method to run so that we only receive update requests when necessary
    }

    [HarmonyPatch(typeof(Game.Net.TrafficLightInitializationSystem), "OnCreateForCompiler")]
    [HarmonyPrefix]
    static bool TrafficLightInitializationSystemOnCreateForCompiler()
    {
        return false;
    }

    [HarmonyPatch(typeof(Game.Net.TrafficLightInitializationSystem), "OnUpdate")]
    [HarmonyPrefix]
    static bool TrafficLightInitializationSystemOnUpdate(Game.Net.TrafficLightInitializationSystem __instance)
    {
        // For some reason, the cloned TrafficLightInitializationSystem never receives calls to update. So we have to do it manually.
        // Could be something with the EntityQuery. I'm not able to find out the reason behind it.
        __instance.World.GetOrCreateSystemManaged<PatchedClasses.TrafficLightInitializationSystem>().Update();
        return false;
    }

    [HarmonyPatch(typeof(Game.Tools.NetToolSystem), "SetAppliedUpgrade")]
    [HarmonyPostfix]
    static void SetAppliedUpgrade(Game.Tools.NetToolSystem __instance, bool removing)
    {
        if (removing)
        {
            return;
        }

        Unity.Entities.Entity entity = Traverse.Create(__instance).Field("m_AppliedUpgrade").Property("value").Field("m_Entity").GetValue<Unity.Entities.Entity>();
        Game.Prefabs.CompositionFlags flags = Traverse.Create(__instance).Field("m_AppliedUpgrade").Property("value").Field("m_Flags").GetValue<Game.Prefabs.CompositionFlags>();

        if ((flags.m_General & Game.Prefabs.CompositionFlags.General.TrafficLights) != 0)
        {
            UI.UISystem uiSystem = __instance.World.GetOrCreateSystemManaged<UI.UISystem>();
            Unity.Entities.ComponentLookup<PatchedClasses.TrafficLightsData> trafficLightsDataLookup = __instance.GetComponentLookup<PatchedClasses.TrafficLightsData>(false);

            if (!trafficLightsDataLookup.HasComponent(entity))
            {
                bool result = __instance.EntityManager.AddComponentData(entity, new PatchedClasses.TrafficLightsData(uiSystem.m_SelectedPattern));
                if (!result)
                {
                    System.Console.WriteLine($"[SetAppliedUpgrade] Failed to add TrafficLightsData to entity {entity.ToString()}.");
                }
            }
            else
            {
                PatchedClasses.TrafficLightsData trafficLightsData = trafficLightsDataLookup[entity];
                trafficLightsData.SetPatterns(uiSystem.m_SelectedPattern);
                trafficLightsDataLookup[entity] = trafficLightsData;
            }
        }

    }

    [HarmonyPatch(typeof(Game.Audio.AudioManager), "OnGameLoadingComplete")]
    [HarmonyPostfix]
    static void Postfix(Game.Audio.AudioManager __instance, ref Game.GameMode mode)
    {
        if ((mode & Game.GameMode.GameOrEditor) == 0)
        {
            return;
        }

        __instance.World.GetOrCreateSystem<UI.UISystem>( );
    }
}