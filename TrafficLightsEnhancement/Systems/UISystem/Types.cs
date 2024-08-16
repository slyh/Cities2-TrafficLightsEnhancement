using System.Collections.Generic;
using Newtonsoft.Json;

namespace C2VM.TrafficLightsEnhancement.Systems.UISystem;

public static class Types
{
    public struct ItemDivider
    {
        [JsonProperty]
        const string itemType = "divider";
    }

    public struct ItemRadio
    {
        [JsonProperty]
        const string itemType = "radio";

        public string type;

        public bool isChecked;

        public string key;

        public string value;

        public string label;

        public string engineEventName;
    }

    public struct ItemTitle
    {
        [JsonProperty]
        const string itemType = "title";

        public string title;
    }

    public struct ItemMessage
    {
        [JsonProperty]
        const string itemType = "message";

        public string message;
    }

    public struct ItemCheckbox
    {
        [JsonProperty]
        const string itemType = "checkbox";

        public string type;

        public bool isChecked;

        public string key;

        public string value;

        public string label;

        public string engineEventName;
    }

    public struct ItemButton
    {
        [JsonProperty]
        const string itemType = "button";

        public string type;

        public string key;

        public string value;

        public string label;

        public string engineEventName;
    }

    public struct ItemNotification
    {
        [JsonProperty]
        const string itemType = "notification";

        [JsonProperty]
        const string type = "c2vm-tle-panel-notification";

        public string label;

        public string notificationType;

        public string key;

        public string value;

        public string engineEventName;
    }

    public struct ItemRange {
        [JsonProperty]
        const string itemType = "range";

        public string key;

        public string label;

        public float value;

        public string valuePrefix;

        public string valueSuffix;

        public float min;

        public float max;

        public float step;

        public string engineEventName;
    }

    public struct WorldPosition
    {
        public float x;

        public float y;

        public float z;
    }

    public struct ScreenPosition
    {
        public float top;

        public float left;
    }

    public struct LaneToolButton
    {
        public string image;

        public bool visible;

        public WorldPosition position;

        public string engineEventName;

        public LaneToolButton(WorldPosition position, bool visible, string engineEventName)
        {
            this.image = "Media/Game/Icons/RoadsServices.svg";
            this.position = position;
            this.visible = visible;
            this.engineEventName = engineEventName;
        }
    }

    public struct LaneDirectionToolPanel
    {
        public string title;

        public string image;

        public bool visible;

        public WorldPosition position;

        public List<LaneDirection> lanes;

        public List<dynamic> items;
    }

    public struct LaneDirection
    {
        [JsonProperty]
        const string itemType = "lane";

        public WorldPosition position;

        public bool leftHandTraffic;
        
        public string label;

        public bool banLeft;

        public bool banRight;

        public bool banStraight;

        public bool banUTurn;
    };

    public static ItemRadio MainPanelItemPattern(string label, uint pattern, uint selectedPattern)
    {
        return new ItemRadio{label = label, key = "pattern", value = pattern.ToString(), engineEventName = "C2VM.TLE.CallMainPanelUpdatePattern", isChecked = (selectedPattern & 0xFFFF) == pattern};
    }

    public static ItemCheckbox MainPanelItemOption(string label, uint option, uint selectedPattern)
    {
        return new ItemCheckbox{label = label, key = option.ToString(), value = ((selectedPattern & option) != 0).ToString(), isChecked = (selectedPattern & option) != 0, engineEventName = "C2VM.TLE.CallMainPanelUpdateOption"};
    }
}


