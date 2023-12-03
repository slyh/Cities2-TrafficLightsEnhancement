using BepInEx;
using HarmonyLib;
using System.Reflection;

#if BEPINEX6
    using BepInEx.Unity.Mono;
#endif

namespace C2VM.TrafficLightsEnhancement;

[BepInPlugin(MyPluginInfo.PLUGIN_GUID, MyPluginInfo.PLUGIN_NAME, MyPluginInfo.PLUGIN_VERSION)]
public class Plugin : BaseUnityPlugin
{
    private void Awake()
    {
        string informationalVersion = ((AssemblyInformationalVersionAttribute) System.Attribute.GetCustomAttribute(Assembly.GetAssembly(typeof(Plugin)), typeof(AssemblyInformationalVersionAttribute))).InformationalVersion;

        Logger.LogInfo($"Plugin {MyPluginInfo.PLUGIN_GUID} {informationalVersion} is loaded!");

        var harmony = new Harmony(MyPluginInfo.PLUGIN_GUID);
        harmony.PatchAll();
    }
}
