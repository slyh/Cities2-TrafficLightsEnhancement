using Game.Prefabs;
using HarmonyLib;
using Unity.Entities;

namespace TrafficLightsEnhancement.UI;

[HarmonyPatch]
class Patches
{
    [HarmonyPatch(typeof(Game.Tools.NetToolSystem), "SetAppliedUpgrade")]
    [HarmonyPostfix]
    static void SetAppliedUpgrade(Game.Tools.NetToolSystem __instance, bool removing)
    {
        if (removing)
        {
            return;
        }

        Entity entity = Traverse.Create(__instance).Field("m_AppliedUpgrade").Property("value").Field("m_Entity").GetValue<Entity>();
        CompositionFlags flags = Traverse.Create(__instance).Field("m_AppliedUpgrade").Property("value").Field("m_Flags").GetValue<CompositionFlags>();

        if ((flags.m_General & CompositionFlags.General.TrafficLights) != 0)
        {
            UISystem uiSystem = __instance.World.GetOrCreateSystemManaged<UISystem>();
            ComponentLookup<PatchedClasses.TrafficLightsData> trafficLightsDataLookup = __instance.GetComponentLookup<PatchedClasses.TrafficLightsData>(false);

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
    static void OnGameLoadingComplete(Game.Audio.AudioManager __instance, ref Game.GameMode mode)
    {
        if ((mode & Game.GameMode.GameOrEditor) == 0)
        {
            return;
        }

        __instance.World.GetOrCreateSystem<UISystem>();
    }
}