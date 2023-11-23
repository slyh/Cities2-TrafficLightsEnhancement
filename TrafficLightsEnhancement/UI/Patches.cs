using C2VM.CommonLibraries.LaneSystem;
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
            BufferLookup<ConnectPositionSource> connectPositionSourceLookup = __instance.GetBufferLookup<ConnectPositionSource>(false);
            if (!connectPositionSourceLookup.HasBuffer(entity))
            {
                __instance.EntityManager.AddBuffer<ConnectPositionSource>(entity);
            }
            else
            {
                connectPositionSourceLookup[entity].Clear();
            }

            BufferLookup<ConnectPositionTarget> connectPositionTargetLookup = __instance.GetBufferLookup<ConnectPositionTarget>(false);
            if (!connectPositionTargetLookup.HasBuffer(entity))
            {
                __instance.EntityManager.AddBuffer<ConnectPositionTarget>(entity);
            }
            else
            {
                connectPositionTargetLookup[entity].Clear();
            }

            UISystem uiSystem = __instance.World.GetOrCreateSystemManaged<UISystem>();
            uiSystem.UpdateSelectedEntity(entity);
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