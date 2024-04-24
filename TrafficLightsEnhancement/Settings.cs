using System.Reflection;
using Colossal.IO.AssetDatabase;
using Game.Modding;
using Game.Settings;
using Game.UI.Widgets;

namespace C2VM.TrafficLightsEnhancement;

[FileLocation("C2VM-TrafficLightsEnhancement")]
[SettingsUIGroupOrder(["locale", "version"])]
[SettingsUIShowGroupName]
public class Settings : ModSetting
{
    [SettingsUISection("locale")]
    [SettingsUIDropdown(typeof(Settings), "GetLanguageValues")]
    public string locale { get; set; }

    [SettingsUISection("version")]
    public string tleVersion => Mod.informationalVersion.Substring(0, 20);

    [SettingsUISection("version")]
    public string laneSystemVersion => ((AssemblyInformationalVersionAttribute) System.Attribute.GetCustomAttribute(Assembly.GetAssembly(typeof(C2VM.CommonLibraries.LaneSystem.Mod)), typeof(AssemblyInformationalVersionAttribute))).InformationalVersion.Substring(0, 20);

    public Settings(IMod mod) : base(mod)
    {
        SetDefaults();
    }

    public override void SetDefaults()
    {
        locale = "auto";
    }

    public override void Apply()
    {
        base.Apply();
        C2VM.TrafficLightsEnhancement.Systems.UISystem.UISystem.UpdateLocale();
        Colossal.Localization.LocalizationManager localizationManager = Game.SceneFlow.GameManager.instance.localizationManager;
        localizationManager.GetType().GetTypeInfo().GetDeclaredMethod("NotifyActiveDictionaryChanged").Invoke(localizationManager, null);
    }

    public static DropdownItem<string>[] GetLanguageValues()
    {
        DropdownItem<string>[] list = [
            new DropdownItem<string>
            {
                value = "auto",
                displayName = "Auto"
            },
            new DropdownItem<string>
            {
                value = "de-DE",
                displayName = "German"
            },
            new DropdownItem<string>
            {
                value = "en-US",
                displayName = "English"
            },
            new DropdownItem<string>
            {
                value = "es-ES",
                displayName = "Spanish"
            },
            new DropdownItem<string>
            {
                value = "fr-FR",
                displayName = "French"
            },
            // new DropdownItem<string>
            // {
            //     value = "it-IT",
            //     displayName = "Italian"
            // },
            new DropdownItem<string>
            {
                value = "ja-JP",
                displayName = "Japanese"
            },
            new DropdownItem<string>
            {
                value = "ko-KR",
                displayName = "Korean"
            },
            new DropdownItem<string>
            {
                value = "nl-NL",
                displayName = "Dutch"
            },
            new DropdownItem<string>
            {
                value = "pl-PL",
                displayName = "Polish"
            },
            new DropdownItem<string>
            {
                value = "pt-BR",
                displayName = "Portuguese (Brazil)"
            },
            new DropdownItem<string>
            {
                value = "ru-RU",
                displayName = "Russian"
            },
            new DropdownItem<string>
            {
                value = "zh-HANS",
                displayName = "Chinese (Simplified)"
            },
            new DropdownItem<string>
            {
                value = "zh-HANT",
                displayName = "Chinese (Traditional)"
            },
            new DropdownItem<string>
            {
                value = "zh-HK",
                displayName = "Chinese (Hong Kong)"
            },
            new DropdownItem<string>
            {
                value = "zh-TW",
                displayName = "Chinese (Taiwan)"
            }
        ];
        return list;
    }
}