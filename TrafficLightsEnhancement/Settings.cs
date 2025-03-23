using System.Reflection;
using Colossal.IO.AssetDatabase;
using Game.Input;
using Game.Modding;
using Game.SceneFlow;
using Game.Settings;
using Game.UI.Widgets;
using Unity.Entities;

namespace C2VM.TrafficLightsEnhancement;

[FileLocation("ModsSettings/C2VM.TrafficLightsEnhancement/Settings")]
[SettingsUITabOrder(kTabGeneral, kTabKeyBindings)]
[SettingsUIGroupOrder(kGroupLanguage, kGroupDefault, kGroupVersion, kGroupMainPanel, kGroupKeyBindingReset)]
[SettingsUIShowGroupName]
public class Settings : ModSetting
{
    public const string kTabGeneral = "TabGeneral";

    public const string kTabKeyBindings = "TabKeyBindings";

    public const string kGroupLanguage = "GroupLanguage";

    public const string kGroupDefault = "GroupDefault";

    public const string kGroupVersion = "GroupVersion";

    public const string kGroupMainPanel = "GroupMainPanel";

    public const string kGroupKeyBindingReset = "GroupKeyBindingReset";

    public const string kKeyboardBindingMainPanelToggle = "KeyboardBindingMainPanelToggle";

    public struct Values
    {
        public bool m_DefaultSplitPhasing;

        public bool m_DefaultAlwaysGreenKerbsideTurn;

        public bool m_DefaultExclusivePedestrian;

        public Values(Settings settings)
        {
            m_DefaultSplitPhasing = settings.m_DefaultSplitPhasing;
            m_DefaultAlwaysGreenKerbsideTurn = settings.m_DefaultAlwaysGreenKerbsideTurn;
            m_DefaultExclusivePedestrian = settings.m_DefaultExclusivePedestrian;
        }
    }

    [SettingsUISection(kTabGeneral, kGroupLanguage)]
    [SettingsUIDropdown(typeof(Settings), "GetLanguageValues")]
    public string m_LocaleOption
    {
        get
        {
            return m_Locale;
        }
        set
        {
            m_Locale = value;
            Colossal.Localization.LocalizationManager localizationManager = Game.SceneFlow.GameManager.instance.localizationManager;
            localizationManager.GetType().GetTypeInfo().GetDeclaredMethod("NotifyActiveDictionaryChanged").Invoke(localizationManager, null);
        }
    }

    public string m_Locale { get; private set; }

    [SettingsUISection(kTabGeneral, kGroupVersion)]
    public string m_ReleaseChannel => IsNotCanary() ? "Alpha" : "Canary";

    [SettingsUISection(kTabGeneral, kGroupVersion)]
    public string m_TleVersion => Mod.m_InformationalVersion.Substring(0, 20);

    [SettingsUISection(kTabGeneral, kGroupVersion)]
    public string m_LaneSystemVersion => C2VM.CommonLibraries.LaneSystem.Mod.m_InformationalVersion.Substring(0, 20);

    [SettingsUISection(kTabGeneral, kGroupDefault)]
    public bool m_DefaultSplitPhasing { get; set; }

    [SettingsUISection(kTabGeneral, kGroupDefault)]
    public bool m_DefaultAlwaysGreenKerbsideTurn { get; set; }

    [SettingsUISection(kTabGeneral, kGroupDefault)]
    public bool m_DefaultExclusivePedestrian { get; set; }

    [SettingsUISection(kTabGeneral, kGroupDefault)]
    [SettingsUIButton]
    [SettingsUIConfirmation(null, null)]
    [SettingsUIDisableByCondition(typeof(Settings), "IsNotInGame")]
    public bool m_ForceNodeUpdate
    {
        get
        {
            return false;
        }
        set
        {
            EntityQuery entityQuery = Mod.m_World.EntityManager.CreateEntityQuery(ComponentType.ReadOnly<Game.Net.TrafficLights>());
            Mod.m_World.EntityManager.AddComponent<Game.Common.Updated>(entityQuery);
        }
    }

    [SettingsUISection(kTabGeneral, kGroupVersion)]
    [SettingsUIButton]
    [SettingsUIConfirmation(null, null)]
    [SettingsUIHideByCondition(typeof(Settings), "IsNotCanary")]
    public bool m_SuppressCanaryWarning
    {
        get
        {
            return false;
        }
        set
        {
            if (value == true)
            {
                m_SuppressCanaryWarningVersion = Mod.m_InformationalVersion;
                Systems.UI.UISystem.m_MainPanelBinding?.Update();
            }
        }
    }

    public string m_SuppressCanaryWarningVersion;

    [SettingsUIKeyboardBinding(BindingKeyboard.None, kKeyboardBindingMainPanelToggle)]
    [SettingsUISection(kTabKeyBindings, kGroupMainPanel)]
    public ProxyBinding m_MainPanelToggleKeyboardBinding { get; set; }

    [SettingsUISection(kTabKeyBindings, kGroupKeyBindingReset)]
    [SettingsUIButton]
    [SettingsUIConfirmation(null, null)]
    public bool m_ResetBindings
    {
        set
        {
            ResetKeyBindings();
        }
    }

    public Settings(IMod mod) : base(mod)
    {
        SetDefaults();
        RegisterInOptionsUI();
        RegisterKeyBindings();
        AssetDatabase.global.LoadSettings(nameof(Settings), this);
    }

    public override void SetDefaults()
    {
        m_LocaleOption = "auto";
        m_DefaultSplitPhasing = false;
        m_DefaultAlwaysGreenKerbsideTurn = false;
        m_DefaultExclusivePedestrian = false;
        m_SuppressCanaryWarningVersion = "";
    }

    public override void Apply()
    {
        base.Apply();
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
            new DropdownItem<string>
            {
                value = "it-IT",
                displayName = "Italian"
            },
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

    public bool IsNotInGame()
    {
        return GameManager.instance.gameMode != Game.GameMode.Game;
    }

    public bool IsNotCanary()
    {
        return !Mod.IsCanary();
    }
}