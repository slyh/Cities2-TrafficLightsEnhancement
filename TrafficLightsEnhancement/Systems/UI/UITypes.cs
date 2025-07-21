using System.ComponentModel;
using Colossal.UI.Binding;
using Newtonsoft.Json;

namespace C2VM.TrafficLightsEnhancement.Systems.UI;

public static class UITypes
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

        public float defaultValue;

        public bool enableTextField;

        public string textFieldRegExp;

        public string engineEventName;
    }

    public struct ItemCustomPhase
    {
        [JsonProperty]
        const string itemType = "customPhase";

        public int activeIndex;

        public int currentSignalGroup;

        public int index;

        public int length;

        public uint timer;

        public ushort turnsSinceLastRun;

        public ushort lowFlowTimer;

        public float carFlow;

        public ushort carLaneOccupied;

        public ushort publicCarLaneOccupied;

        public ushort trackLaneOccupied;

        public ushort pedestrianLaneOccupied;

        public float weightedWaiting;

        public float targetDuration;

        public int priority;

        public ushort minimumDuration;

        public ushort maximumDuration;

        public float targetDurationMultiplier;

        public float laneOccupiedMultiplier;

        public float intervalExponent;

        public bool prioritiseTrack;

        public bool prioritisePublicCar;

        public bool prioritisePedestrian;

        public bool linkedWithNextPhase;
    }

    public struct UpdateCustomPhaseData
    {
        [DefaultValue(-1)]
        [JsonProperty(DefaultValueHandling = DefaultValueHandling.Populate)]
        public int index;

        public string key;

        public double value;
    }

    public struct WorldPosition : IJsonWritable
    {
        public float x;

        public float y;

        public float z;

        public string key { get => $"{x.ToString("0.0")},{y.ToString("0.0")},{z.ToString("0.0")}"; }

        public static implicit operator WorldPosition(float pos) => new WorldPosition{x = pos, y = pos, z = pos};

        public static implicit operator WorldPosition(Unity.Mathematics.float3 pos) => new WorldPosition{x = pos.x, y = pos.y, z = pos.z};

        public static implicit operator Unity.Mathematics.float3(WorldPosition pos) => new Unity.Mathematics.float3(pos.x, pos.y, pos.z);

        public static implicit operator UnityEngine.Vector3(WorldPosition pos) => new UnityEngine.Vector3(pos.x, pos.y, pos.z);

        public static implicit operator string(WorldPosition pos) => pos.key;

        public override bool Equals(object obj)
        {
            if (obj is not WorldPosition)
            {
                return false;
            }
            return Equals((WorldPosition)obj);
        }

        public bool Equals(WorldPosition other)
        {
            return x == other.x && y == other.y && z == other.z;
        }

        public override int GetHashCode()
        {
            return x.GetHashCode() ^ (y.GetHashCode() << 2) ^ (z.GetHashCode() >> 2);
        }

        public void Write(IJsonWriter writer)
        {
            writer.TypeBegin(typeof(WorldPosition).FullName);
            writer.PropertyName("x");
            writer.Write(x);
            writer.PropertyName("y");
            writer.Write(y);
            writer.PropertyName("z");
            writer.Write(z);
            writer.PropertyName("key");
            writer.Write(key);
            writer.TypeEnd();
        }
    }

    public struct ScreenPoint : System.IEquatable<ScreenPoint>, IJsonWritable
    {
        public int top;

        public int left;

        public ScreenPoint(int topPos, int leftPos)
        {
            left = leftPos;
            top = topPos;
        }

        public ScreenPoint(UnityEngine.Vector3 pos, int screenHeight)
        {
            left = (int)pos.x;
            top = (int)(screenHeight - pos.y);
        }

        public void Write(IJsonWriter writer)
        {
            writer.TypeBegin(typeof(ScreenPoint).FullName);
            writer.PropertyName("top");
            writer.Write(top);
            writer.PropertyName("left");
            writer.Write(left);
            writer.TypeEnd();
        }

        public override bool Equals(object obj)
        {
            if (obj is ScreenPoint other){
                return Equals(other);
            }
            return false;
        }

        public bool Equals(ScreenPoint other)
        {
            return other.top == top && other.left == left;
        }

        public override int GetHashCode() => (top, left).GetHashCode();
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

    public static ItemRadio MainPanelItemPattern(string label, uint pattern, uint selectedPattern)
    {
        return new ItemRadio{label = label, key = "pattern", value = pattern.ToString(), engineEventName = "C2VM.TLE.CallMainPanelUpdatePattern", isChecked = (selectedPattern & 0xFFFF) == pattern};
    }

    public static ItemCheckbox MainPanelItemOption(string label, uint option, uint selectedPattern)
    {
        return new ItemCheckbox{label = label, key = option.ToString(), value = ((selectedPattern & option) != 0).ToString(), isChecked = (selectedPattern & option) != 0, engineEventName = "C2VM.TLE.CallMainPanelUpdateOption"};
    }
}