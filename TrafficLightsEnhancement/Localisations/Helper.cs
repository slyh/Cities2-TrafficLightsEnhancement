using System.Collections.Generic;
using System.Linq;
using System.Resources;

namespace C2VM.TrafficLightsEnhancement.Localisations;

public class Helper
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

    private ResourceManager m_ResourceManager;
    
    public string m_Locale { get; private set; }

    public Helper(string locale)
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
        try
        {
            m_ResourceManager = new ResourceManager("C2VM.TrafficLightsEnhancement.Localisations." + m_Locale, typeof(Helper).Assembly);
            m_ResourceManager.GetString(""); // Test if the requested resource exists
        }
        catch (System.Exception e)
        {
            Mod.m_Log.Error(e);
            m_ResourceManager = new ResourceManager("C2VM.TrafficLightsEnhancement.Localisations." + m_DefaultLocale, typeof(Helper).Assembly);
        }
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
        return m_ResourceManager.GetString(key);
    }

    public void AddToDictionary(Colossal.Localization.LocalizationDictionary dictionary)
    {
        dictionary.Add(Mod.m_Settings.GetSettingsLocaleID(), "Traffic Lights Enhancement");
        dictionary.Add(Mod.m_Settings.GetOptionGroupLocaleID("General"), this.GetString("General"));
        dictionary.Add(Mod.m_Settings.GetOptionGroupLocaleID("Default"), this.GetString("Default"));
        dictionary.Add(Mod.m_Settings.GetOptionGroupLocaleID("Version"), this.GetString("Version"));
        dictionary.Add(Mod.m_Settings.GetOptionLabelLocaleID("m_LocaleOption"), this.GetString("LocaleLabel"));
        dictionary.Add(Mod.m_Settings.GetOptionLabelLocaleID("m_DefaultSplitPhasing"), this.GetString("DefaultSplitPhasingLabel"));
        dictionary.Add(Mod.m_Settings.GetOptionLabelLocaleID("m_DefaultAlwaysGreenKerbsideTurn"), this.GetString("DefaultAlwaysGreenKerbsideTurnLabel"));
        dictionary.Add(Mod.m_Settings.GetOptionLabelLocaleID("m_DefaultExclusivePedestrian"), this.GetString("DefaultExclusivePedestrianLabel"));
        dictionary.Add(Mod.m_Settings.GetOptionLabelLocaleID("m_ForceNodeUpdate"), this.GetString("ForceAllNodesUpdateLabel"));
        dictionary.Add(Mod.m_Settings.GetOptionLabelLocaleID("m_TleVersion"), Mod.m_Id);
        dictionary.Add(Mod.m_Settings.GetOptionLabelLocaleID("m_LaneSystemVersion"), C2VM.CommonLibraries.LaneSystem.Mod.id);
        dictionary.Add(Mod.m_Settings.GetOptionDescLocaleID("m_LocaleOption"), this.GetString("LocaleDesc"));
        dictionary.Add(Mod.m_Settings.GetOptionDescLocaleID("m_DefaultSplitPhasing"), this.GetString("DefaultSplitPhasingDesc"));
        dictionary.Add(Mod.m_Settings.GetOptionDescLocaleID("m_DefaultAlwaysGreenKerbsideTurn"), this.GetString("DefaultAlwaysGreenKerbsideTurnDesc"));
        dictionary.Add(Mod.m_Settings.GetOptionDescLocaleID("m_DefaultExclusivePedestrian"), this.GetString("DefaultExclusivePedestrianDesc"));
        dictionary.Add(Mod.m_Settings.GetOptionDescLocaleID("m_ForceNodeUpdate"), this.GetString("ForceAllNodesUpdateDesc"));
        dictionary.Add(Mod.m_Settings.GetOptionDescLocaleID("m_TleVersion"), Mod.m_InformationalVersion);
        dictionary.Add(Mod.m_Settings.GetOptionDescLocaleID("m_LaneSystemVersion"), ((System.Reflection.AssemblyInformationalVersionAttribute) System.Attribute.GetCustomAttribute(System.Reflection.Assembly.GetAssembly(typeof(C2VM.CommonLibraries.LaneSystem.Mod)), typeof(System.Reflection.AssemblyInformationalVersionAttribute))).InformationalVersion);
        dictionary.Add(Mod.m_Settings.GetOptionWarningLocaleID("m_ForceNodeUpdate"), this.GetString("ForceAllNodesUpdateWarning"));
    }
}