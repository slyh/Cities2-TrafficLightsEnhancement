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
            Mod.log.Error(e);
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
        dictionary.Add(Mod.settings.GetSettingsLocaleID(), "Traffic Lights Enhancement");
        dictionary.Add(Mod.settings.GetOptionGroupLocaleID("locale"), this.GetString("LocaleLabel"));
        dictionary.Add(Mod.settings.GetOptionLabelLocaleID("locale"), this.GetString("LocaleLabel"));
        dictionary.Add(Mod.settings.GetOptionDescLocaleID("locale"), this.GetString("LocaleDesc"));
        dictionary.Add(Mod.settings.GetOptionGroupLocaleID("version"), this.GetString("Version"));
        dictionary.Add(Mod.settings.GetOptionLabelLocaleID("tleVersion"), Mod.id);
        dictionary.Add(Mod.settings.GetOptionDescLocaleID("tleVersion"), Mod.informationalVersion);
        dictionary.Add(Mod.settings.GetOptionLabelLocaleID("laneSystemVersion"), C2VM.CommonLibraries.LaneSystem.Mod.id);
        dictionary.Add(Mod.settings.GetOptionDescLocaleID("laneSystemVersion"), ((System.Reflection.AssemblyInformationalVersionAttribute) System.Attribute.GetCustomAttribute(System.Reflection.Assembly.GetAssembly(typeof(C2VM.CommonLibraries.LaneSystem.Mod)), typeof(System.Reflection.AssemblyInformationalVersionAttribute))).InformationalVersion);
    }
}