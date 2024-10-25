using HarmonyLib;

namespace C2VM.TrafficLightsEnhancement.Systems.UI;

[HarmonyPatch]
class Patches
{
    [HarmonyPatch(typeof(Colossal.Localization.LocalizationManager), "NotifyActiveDictionaryChanged")]
    [HarmonyPostfix]
    static void NotifyActiveDictionaryChanged()
    {
        C2VM.TrafficLightsEnhancement.Systems.UI.UISystem.UpdateLocale();
    }
}