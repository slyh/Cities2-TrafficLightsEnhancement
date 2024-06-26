using System.Collections.Generic;
using C2VM.CommonLibraries.LaneSystem;
using Game.Prefabs;
using HarmonyLib;
using Unity.Entities;

namespace C2VM.TrafficLightsEnhancement.Systems.UISystem;

[HarmonyPatch]
class Patches
{
    [HarmonyPatch(typeof(Colossal.Localization.LocalizationManager), "NotifyActiveDictionaryChanged")]
    [HarmonyPostfix]
    static void NotifyActiveDictionaryChanged()
    {
        C2VM.TrafficLightsEnhancement.Systems.UISystem.UISystem.CallUpdateLocale();
    }

    [HarmonyPatch(typeof(Game.SceneFlow.UserInterface), "OnReadyForBindings")]
    [HarmonyPostfix]
    static void OnReadyForBindings()
    {
        if (Mod.m_UISystem != null)
        {
            Mod.m_UISystem.AddCallBinding();
        }
    }

    [HarmonyPatch(typeof(Game.Tools.NetToolSystem), "SetAppliedUpgrade")]
    [HarmonyPostfix]
    static void NetToolSystemSetAppliedUpgrade(Game.Tools.NetToolSystem __instance, bool removing)
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
            uiSystem.ChangeSelectedEntity(entity);
        }
    }

    [HarmonyPatch(typeof(Game.UI.InGame.ToolbarUISystem), "Apply")]
    [HarmonyPostfix]
    static void ToolbarUISystemApply(Game.UI.InGame.ToolbarUISystem __instance, List<Entity> themes, List<Entity> packs, Entity assetMenuEntity, Entity assetCategoryEntity, Entity assetEntity)
    {
        UISystem uiSystem = __instance.World.GetOrCreateSystemManaged<UISystem>();
        if (__instance.EntityManager.HasComponent<PlaceableNetData>(assetEntity))
        {
            PlaceableNetData placeableNetData = __instance.EntityManager.GetComponentData<PlaceableNetData>(assetEntity);
            if ((placeableNetData.m_SetUpgradeFlags.m_General & CompositionFlags.General.TrafficLights) != 0)
            {
                uiSystem.ShouldShowPanel(true);
                return;
            }
        }
        uiSystem.ShouldShowPanel(false);
    }
}