using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Newtonsoft.Json;

namespace C2VM.TrafficLightsEnhancement.Utils;

public class LocalisationUtils
{
    public static readonly string m_DefaultLocale = "en-US";

    public static readonly string[] m_SupportedLocales =
    [
        "de-DE",
        "en-US",
        "es-ES",
        "fr-FR",
        "it-IT",
        "ja-JP",
        "ko-KR",
        "nl-NL",
        "pl-PL",
        "pt-BR",
        "ru-RU",
        "zh-HANS",
        "zh-HANT",
        "zh-HK",
        "zh-TW"
    ];

    public static readonly Dictionary<string, string[]> m_SupportedCultures = new()
    {
        { "en-US", ["nl-NL"] },
        { "zh-HANT", ["zh-HK", "zh-TW"] }
    };
    
    public string m_Locale { get; private set; }

    private Dictionary<string, string> m_Dictionary = new Dictionary<string, string>();

    public LocalisationUtils(string locale)
    {
        SetLocale(locale);
    }

    public void SetLocale(string locale)
    {
        m_Locale = locale;
        if (!m_SupportedLocales.Contains(locale))
        {
            m_Locale = m_DefaultLocale;
        }
        string resourceName = "C2VM.TrafficLightsEnhancement.Resources.Localisations." + m_Locale + ".json";
        using Stream stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(resourceName);
        if (stream == null)
        {
            Mod.m_Log.Error($"{resourceName} does not exist.");
            return;
        }
        using StreamReader reader = new StreamReader(stream, System.Text.Encoding.UTF8);
        string jsonString = reader.ReadToEnd();
        m_Dictionary = JsonConvert.DeserializeObject<Dictionary<string, string>>(jsonString);
    }

    public static string GetAutoLocale(string locale, string culture)
    {
        if (m_SupportedCultures.ContainsKey(locale) && m_SupportedCultures[locale].Contains(culture)) {
            return culture;
        }
        if (m_SupportedLocales.Contains(locale))
        {
            return locale;
        }
        return m_DefaultLocale;
    }

    public string GetString(string key)
    {
        if (m_Dictionary.ContainsKey(key))
        {
            return m_Dictionary[key];
        }
        return key;
    }

    public void AddToDictionary(Colossal.Localization.LocalizationDictionary dictionary)
    {
        if (Mod.m_Settings == null)
        {
            return;
        }
        dictionary.Add(Mod.m_Settings.GetSettingsLocaleID(), "Traffic Lights Enhancement");
        dictionary.Add(Mod.m_Settings.GetOptionTabLocaleID(Settings.kTabGeneral), this.GetString("General"));
        dictionary.Add(Mod.m_Settings.GetOptionTabLocaleID(Settings.kTabKeyBindings), this.GetString("KeyBindings"));
        dictionary.Add(Mod.m_Settings.GetOptionGroupLocaleID(Settings.kGroupLanguage), this.GetString("LocaleLabel"));
        dictionary.Add(Mod.m_Settings.GetOptionGroupLocaleID(Settings.kGroupDefault), this.GetString("Default"));
        dictionary.Add(Mod.m_Settings.GetOptionGroupLocaleID(Settings.kGroupVersion), this.GetString("Version"));
        dictionary.Add(Mod.m_Settings.GetOptionGroupLocaleID(Settings.kGroupMainPanel), this.GetString("MainPanel"));
        dictionary.Add(Mod.m_Settings.GetOptionGroupLocaleID(Settings.kGroupKeyBindingReset), this.GetString("Reset"));
        dictionary.Add(Mod.m_Settings.GetOptionLabelLocaleID("m_LocaleOption"), this.GetString("LocaleLabel"));
        dictionary.Add(Mod.m_Settings.GetOptionLabelLocaleID("m_ShowFloatingButton"), this.GetString("ShowFloatingButtonLabel"));
        dictionary.Add(Mod.m_Settings.GetOptionLabelLocaleID("m_DefaultSplitPhasing"), this.GetString("DefaultSplitPhasingLabel"));
        dictionary.Add(Mod.m_Settings.GetOptionLabelLocaleID("m_DefaultAlwaysGreenKerbsideTurn"), this.GetString("DefaultAlwaysGreenKerbsideTurnLabel"));
        dictionary.Add(Mod.m_Settings.GetOptionLabelLocaleID("m_DefaultExclusivePedestrian"), this.GetString("DefaultExclusivePedestrianLabel"));
        dictionary.Add(Mod.m_Settings.GetOptionLabelLocaleID("m_ForceNodeUpdate"), this.GetString("ForceAllNodesUpdateLabel"));
        dictionary.Add(Mod.m_Settings.GetOptionLabelLocaleID("m_ReleaseChannel"), this.GetString("ReleaseChannel"));
        dictionary.Add(Mod.m_Settings.GetOptionLabelLocaleID("m_TleVersion"), Mod.m_Id);
        dictionary.Add(Mod.m_Settings.GetOptionLabelLocaleID("m_LaneSystemVersion"), C2VM.CommonLibraries.LaneSystem.Mod.m_Id);
        dictionary.Add(Mod.m_Settings.GetOptionLabelLocaleID("m_SuppressCanaryWarning"), this.GetString("SuppressCanaryWarningLabel"));
        dictionary.Add(Mod.m_Settings.GetOptionLabelLocaleID("m_MainPanelToggleKeyboardBinding"), this.GetString("MainPanelToggleKeyboardBindingLabel"));
        dictionary.Add(Mod.m_Settings.GetOptionLabelLocaleID("m_ResetBindings"), this.GetString("ResetBindingsLabel"));
        dictionary.Add(Mod.m_Settings.GetOptionDescLocaleID("m_LocaleOption"), this.GetString("LocaleDesc"));
        dictionary.Add(Mod.m_Settings.GetOptionDescLocaleID("m_ShowFloatingButton"), this.GetString("ShowFloatingButtonDesc"));
        dictionary.Add(Mod.m_Settings.GetOptionDescLocaleID("m_DefaultSplitPhasing"), this.GetString("DefaultSplitPhasingDesc"));
        dictionary.Add(Mod.m_Settings.GetOptionDescLocaleID("m_DefaultAlwaysGreenKerbsideTurn"), this.GetString("DefaultAlwaysGreenKerbsideTurnDesc"));
        dictionary.Add(Mod.m_Settings.GetOptionDescLocaleID("m_DefaultExclusivePedestrian"), this.GetString("DefaultExclusivePedestrianDesc"));
        dictionary.Add(Mod.m_Settings.GetOptionDescLocaleID("m_ForceNodeUpdate"), this.GetString("ForceAllNodesUpdateDesc"));
        dictionary.Add(Mod.m_Settings.GetOptionDescLocaleID("m_ReleaseChannel"), Mod.m_Settings.m_ReleaseChannel);
        dictionary.Add(Mod.m_Settings.GetOptionDescLocaleID("m_TleVersion"), Mod.m_InformationalVersion);
        dictionary.Add(Mod.m_Settings.GetOptionDescLocaleID("m_LaneSystemVersion"), C2VM.CommonLibraries.LaneSystem.Mod.m_InformationalVersion);
        dictionary.Add(Mod.m_Settings.GetOptionDescLocaleID("m_SuppressCanaryWarning"), this.GetString("SuppressCanaryWarningDesc"));
        dictionary.Add(Mod.m_Settings.GetOptionDescLocaleID("m_MainPanelToggleKeyboardBinding"), this.GetString("MainPanelToggleKeyboardBindingDesc"));
        dictionary.Add(Mod.m_Settings.GetOptionDescLocaleID("m_ResetBindings"), this.GetString("ResetBindingsDesc"));
        dictionary.Add(Mod.m_Settings.GetOptionWarningLocaleID("m_ForceNodeUpdate"), this.GetString("ForceAllNodesUpdateWarning"));
        dictionary.Add(Mod.m_Settings.GetOptionWarningLocaleID("m_SuppressCanaryWarning"), this.GetString("SuppressCanaryWarningDesc"));
        dictionary.Add(Mod.m_Settings.GetOptionWarningLocaleID("m_ResetBindings"), this.GetString("ResetBindingsDesc"));
        dictionary.Add("Menu.NOTIFICATION_TITLE[C2VM.TLE.LdtMigrationNotificationTitle]", this.GetString("LdtMigrationNotificationTitle"));
        dictionary.Add("Menu.NOTIFICATION_DESCRIPTION[C2VM.TLE.LdtMigrationNotificationText]", this.GetString("LdtMigrationNotificationText"));
    }
}