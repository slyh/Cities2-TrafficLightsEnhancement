using System;
using System.Collections;
using System.Collections.Generic;
using System.Resources;
using C2VM.CommonLibraries.LaneSystem;
using C2VM.TrafficLightsEnhancement.Components;
using C2VM.TrafficLightsEnhancement.Systems.TrafficLightInitializationSystem;
using cohtml.Net;
using Game;
using Game.Common;
using Game.Net;
using Game.SceneFlow;
using Newtonsoft.Json;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace C2VM.TrafficLightsEnhancement.Systems.UISystem;

public class UISystem : GameSystemBase
{
    public struct MenuItemDivider {
        [JsonProperty]
        const string itemType = "divider";
    }

    public struct MenuItemPattern {
        [JsonProperty]
        const string itemType = "radio";

        [JsonProperty]
        const string type = "c2vm-tle-panel-pattern";

        public int ways;

        public int pattern;

        public string label;
    }

    public struct MenuItemTitle {
        [JsonProperty]
        const string itemType = "title";

        public string title;
    }

    public struct MenuItemOption {
        [JsonProperty]
        const string itemType = "checkbox";

        [JsonProperty]
        const string type = "c2vm-tle-panel-option";

        public string key;

        public string value;

        public string label;
    }

    public struct MenuItemButton {
        [JsonProperty]
        const string itemType = "button";

        [JsonProperty]
        const string type = "c2vm-tle-panel-button";

        public string key;

        public string value;

        public string label;

        public string engineEventName;
    }

    public struct MenuItemNotification {
        [JsonProperty]
        const string itemType = "notification";

        [JsonProperty]
        const string type = "c2vm-tle-panel-notification";

        public string label;

        public string notificationType;
    }

    public struct WorldToScreen {
        public float x;

        public float y;

        public float z;
    }

    public bool m_IsLaneManagementToolOpen;

    public bool m_ShowNotificationUnsaved;

    public Dictionary<string, int> m_Options;

    public int m_SelectedPattern;

    public Entity m_SelectedEntity;

    private View m_View;

    private int m_Ways;

    private ResourceManager m_ResourceManager;

    private string m_Locale;

    private Game.City.CityConfigurationSystem m_CityConfigurationSystem;

    private BufferLookup<ConnectPositionSource> m_ConnectPositionSourceLookup;

    private BufferLookup<ConnectPositionTarget> m_ConnectPositionTargetLookup;

    private BufferLookup<CustomLaneDirection> m_CustomLaneDirectionLookup;

    private BufferLookup<ConnectedEdge> m_ConnectedEdgeLookup;

    private ComponentLookup<CarLane> m_CarLaneLookup;

    private ComponentLookup<Curve> m_CurveLookup;

    private ComponentLookup<CustomTrafficLights> m_CustomTrafficLightsLookup;

    private ComponentLookup<Edge> m_EdgeLookup;

    private ComponentLookup<LaneSignal> m_LaneSignalLookup;

    private ComponentLookup<MasterLane> m_MasterLaneLookup;

    private ComponentLookup<PedestrianLane> m_PedestrianLaneLookup;

    private ComponentLookup<SlaveLane> m_SlaveLaneLookup;

    private ComponentLookup<SecondaryLane> m_SecondaryLaneLookup;

    private BufferLookup<SubLane> m_SubLaneLookup;

    private ComponentLookup<TrackLane> m_TrackLaneLookup;

    protected override void OnCreate()
    {
        base.OnCreate();

        m_Options = new Dictionary<string, int>();

        m_CityConfigurationSystem = World.GetOrCreateSystemManaged<Game.City.CityConfigurationSystem>();
        m_ConnectPositionSourceLookup = GetBufferLookup<ConnectPositionSource>(false);
        m_ConnectPositionTargetLookup = GetBufferLookup<ConnectPositionTarget>(false);
        m_CustomLaneDirectionLookup = GetBufferLookup<CustomLaneDirection>(false);
        m_CustomTrafficLightsLookup = GetComponentLookup<CustomTrafficLights>(false);
        m_ConnectedEdgeLookup = GetBufferLookup<ConnectedEdge>(false);
        m_CarLaneLookup = GetComponentLookup<CarLane>(true);
        m_CurveLookup = GetComponentLookup<Curve>(true);
        m_EdgeLookup = GetComponentLookup<Edge>(false);
        m_LaneSignalLookup = GetComponentLookup<LaneSignal>(true);
        m_MasterLaneLookup = GetComponentLookup<MasterLane>(true);
        m_PedestrianLaneLookup = GetComponentLookup<PedestrianLane>(true);
        m_SlaveLaneLookup = GetComponentLookup<SlaveLane>(true);
        m_SecondaryLaneLookup = GetComponentLookup<SecondaryLane>(true);
        m_SubLaneLookup = GetBufferLookup<SubLane>(false);
        m_TrackLaneLookup = GetComponentLookup<TrackLane>(true);

        m_Locale = GetLocale();
        m_ResourceManager = new ResourceManager("C2VM.TrafficLightsEnhancement.Localisations." + m_Locale, typeof(UISystem).Assembly);

        m_View = GameManager.instance.userInterface.view.View;
        m_View.BindCall("C2VM-TLE-ToggleLaneManagement", ToggleLaneManagement);
        m_View.BindCall("C2VM-TLE-RequestWorldToScreen", RequestWorldToScreen);
        m_View.BindCall("C2VM-TLE-RequestMenuSave", RequestMenuSave);
        m_View.BindCall("C2VM-TLE-RequestMenuData", RequestMenuData);
        m_View.BindCall("C2VM-TLE-RequestLaneManagementData", RequestLaneManagementData);
        m_View.BindCall("C2VM-TLE-ResetLaneManagement", ResetLaneManagement);
        m_View.BindCall("C2VM-TLE-OptionChanged", OptionChanged);
        m_View.BindCall("C2VM-TLE-PatternChanged", PatternChanged);
        m_View.BindCall("C2VM-TLE-CustomLaneDirectionChanged", CustomLaneDirectionChanged);
        m_View.ExecuteScript("""
            if(!document.querySelector("div.c2vm-tle-panel")){const e=document.createElement("div");e.innerHTML='\n        <div class="c2vm-tle-panel">\n            <div class="c2vm-tle-panel-header">\n                <img class="c2vm-tle-panel-header-image" src="Media/Game/Icons/TrafficLights.svg" />\n                <div class="c2vm-tle-panel-header-title">Traffic Lights Enhancement</div>\n            </div>\n            <div class="c2vm-tle-panel-content"></div>\n        </div>\n        <style>\n            .c2vm-tle-panel {\n                width: 300rem;\n                position: absolute;\n                top: calc(10rem+ var(--floatingToggleSize) +6rem);\n                left: 10rem;\n            }\n            .c2vm-tle-panel-header {\n                border-radius: 4rem 4rem 0rem 0rem;\n                background-color: rgba(24, 33, 51, 0.6);\n                backdrop-filter: blur(5px);\n                color: rgba(75, 195, 241, 1);\n                font-size: 14rem;\n                padding: 6rem 10rem;\n                min-height: 36rem;\n                display: flex;\n                flex-direction: row;\n                align-items: center;\n            }\n            .c2vm-tle-panel-header-image {\n                width: 24rem;\n                height: 24rem;\n            }\n            .c2vm-tle-panel-header > .c2vm-tle-panel-header-title {\n                text-transform: uppercase;\n                flex: 1;\n                text-align: center;\n                overflow-x: hidden;\n                overflow-y: hidden;\n                text-overflow: ellipsis;\n                white-space: nowrap;\n            }\n            .c2vm-tle-panel-content {\n                border-radius: 0rem 0rem 4rem 4rem;\n                background-color: rgba(42, 55, 83, 0.437500);\n                backdrop-filter: blur(5px);\n                color: rgba(255, 255, 255, 1);\n                flex: 1;\n                position: relative;\n                padding: 6rem;\n            }\n            .c2vm-tle-panel-row {\n                padding: 3rem 8rem;\n                width: 100%;\n                display: flex;\n            }\n            .c2vm-tle-panel-row-divider {\n                height: 2px;\n                width: auto;\n                border: 2px solid rgba(255, 255, 255, 0.1);\n                margin: 6rem -6rem;\n            }\n            .c2vm-tle-panel-secondary-text {\n                color: rgba(217, 217, 217, 1);\n            }\n            .c2vm-tle-panel-radio {\n                border: 2px solid rgba(75, 195, 241, 1);\n                margin: 0 10rem 0 0;\n                width: 20rem;\n                height: 20rem;\n                padding: 3px;\n                border-radius: 50%;\n            }\n            .c2vm-tle-panel-radio-bullet {\n                width: 100%;\n                height: 100%;\n                background-color:  white;\n                opacity: 0;\n                border-radius: 50%;\n            }\n            .c2vm-tle-panel-radio-bullet-checked {\n                opacity: 1;\n            }\n            .c2vm-tle-panel-checkbox {\n                margin: 0 10rem 0 0;\n                width: 20rem;\n                height: 20rem;\n                padding: 1px;\n                border: 2px solid rgba(255, 255, 255, 0.500000);\n                border-radius: 3rem;\n            }\n            .c2vm-tle-panel-checkbox-checkmark {\n                width: 100%;\n                height: 100%;\n                mask-image: url(Media/Glyphs/Checkmark.svg);\n                mask-size: 100% auto;\n                background-color: white;\n                opacity: 0;\n            }\n            .c2vm-tle-panel-checkbox-checkmark-checked {\n                opacity: 1;\n            }\n            .c2vm-tle-panel-button {\n                padding: 3rem;\n                border-radius: 3rem;\n                color: white;\n                background-color: rgba(6, 10, 16, 0.7);\n                width: 100%;\n            }\n            @keyframes notification-warning {\n                to {\n                  background-color: rgba(200, 0, 0, 0.5);\n                }\n              }\n            .c2vm-tle-panel-row[data-notification-type="warning"] {\n                animation-timing-function: linear;\n                animation-duration: 2s;\n                animation-iteration-count: infinite;\n                animation-direction: alternate;\n                animation-name: notification-warning;\n                border-radius: 3rem;\n                padding: 8rem;\n            }\n            .c2vm-tle-panel-notification-image {\n                width: 20rem;\n                height: 20rem;\n                margin-right: 10rem;\n            }\n            .c2vm-tle-panel-notification-text {\n                color: rgba(217, 217, 217, 1);\n                flex: 1;\n            }\n\n            .c2vm-tle-lane-panel {\n                width: 200rem;\n                display: none;\n            }\n            .c2vm-tle-lane-panel-row {\n                padding: 3rem 8rem;\n                width: 100%;\n                display: flex;\n            }\n            .c2vm-tle-lane-button {\n                padding: 3rem;\n                border-radius: 3rem;\n                background-color: rgba(6, 10, 16, 0.7);\n            }\n            .c2vm-tle-lane-button > img {\n                width: 28rem;\n                height: 28rem;\n            }\n            .c2vm-tle-lane-panel-checkbox {\n                margin: 0 10rem 0 0;\n                width: 20rem;\n                height: 20rem;\n                padding: 1px;\n                border: 2px solid rgba(255, 255, 255, 0.500000);\n                border-radius: 3rem;\n            }\n            .c2vm-tle-lane-panel-checkbox-checkmark {\n                width: 100%;\n                height: 100%;\n                mask-image: url(Media/Glyphs/Checkmark.svg);\n                mask-size: 100% auto;\n                background-color: white;\n                opacity: 0;\n            }\n            .c2vm-tle-lane-panel-checkbox-checkmark-checked {\n                opacity: 1;\n            }\n            .c2vm-tle-lane-panel-button {\n                padding: 3rem;\n                border-radius: 3rem;\n                color: white;\n                background-color: rgba(6, 10, 16, 0.7);\n                width: 100%;\n            }\n        </style>\n    ';document.querySelector("body").appendChild(e);const n=()=>{const e=document.querySelectorAll('[data-type="c2vm-tle-panel-pattern"] .c2vm-tle-panel-radio-bullet');for(const n of e)n.classList.remove("c2vm-tle-panel-radio-bullet-checked")},t=e=>{n();const t=JSON.parse(e),a=t.ways,l=65535&t.pattern,c=document.querySelector(`[data-type="c2vm-tle-panel-pattern"][data-ways="${a}"][data-pattern="${l}"] .c2vm-tle-panel-radio-bullet`);c&&c.classList.add("c2vm-tle-panel-radio-bullet-checked")},a=e=>{n();const a=e.currentTarget.dataset;engine.call("C2VM-TLE-PatternChanged",`${a.ways}_${a.pattern}`).then(t)},l=()=>{const e=document.querySelectorAll('.c2vm-tle-panel [data-type="c2vm-tle-panel-option"]');for(const n of e){n.dataset.value=0;const e=n.querySelector(".c2vm-tle-panel-checkbox-checkmark");e&&e.classList.remove("c2vm-tle-panel-checkbox-checkmark-checked")}},c=e=>{l();const n=JSON.parse(e);for(const e in n){const t=n[e],a=document.querySelector(`[data-type="c2vm-tle-panel-option"][data-key="${e}"]`);if(!a)continue;a.dataset.value=t;const l=a.querySelector(".c2vm-tle-panel-checkbox-checkmark");l&&1===t&&l.classList.add("c2vm-tle-panel-checkbox-checkmark-checked")}},o=e=>{const n=e.currentTarget.dataset;let t=0;0==n.value&&(t=1),l(),engine.call("C2VM-TLE-OptionChanged",`${n.key}_${t}`).then(c)},r=e=>{const n=e.currentTarget.dataset;"C2VM-TLE-RequestMenuSave"==n.engineEventName?i():engine.call(n.engineEventName,`${n.key}_${n.value}`)},i=()=>{engine.call("C2VM-TLE-RequestMenuSave","save_1")},d=()=>{const e=document.querySelector("body");for(const n of e.children)n.classList.contains("c2vm-tle-lane-button")&&e.removeChild(n)},s=e=>{const n=JSON.parse(e),l=document.querySelector(".c2vm-tle-panel-content");for(;l.firstChild;)l.removeChild(l.lastChild);let i=!0;for(const e of n)if(e.itemType){if("divider"==e.itemType){const e=document.createElement("div");e.classList.add("c2vm-tle-panel-row-divider"),l.appendChild(e)}if("title"==e.itemType){const n=document.createElement("div");n.classList.add("c2vm-tle-panel-row"),n.innerHTML=e.title,l.appendChild(n)}if("radio"==e.itemType){const n=document.createElement("div");n.classList.add("c2vm-tle-panel-row");for(const t in e)n.dataset[t]=e[t];n.innerHTML+=`\n                    <div class="c2vm-tle-panel-radio">\n                        <div class="c2vm-tle-panel-radio-bullet"></div>\n                    </div>\n                    <span class="c2vm-tle-panel-secondary-text">${e.label}</span>\n                `,l.appendChild(n)}if("checkbox"==e.itemType){const n=document.createElement("div");n.classList.add("c2vm-tle-panel-row");for(const t in e)n.dataset[t]=e[t];n.innerHTML+=`\n                    <div class="c2vm-tle-panel-checkbox">\n                        <div class="c2vm-tle-panel-checkbox-checkmark"></div>\n                    </div>\n                    <span class="c2vm-tle-panel-secondary-text">${e.label}</span>\n                `,l.appendChild(n)}if("button"==e.itemType){const n=document.createElement("div");n.classList.add("c2vm-tle-panel-row");for(const t in e)n.dataset[t]=e[t];n.innerHTML+=`\n                    <button class="c2vm-tle-panel-button">${e.label}</button>\n                `,l.appendChild(n),"C2VM-TLE-ToggleLaneManagement"==e.engineEventName&&1==e.value&&(i=!1)}if("notification"==e.itemType){const n=document.createElement("div");n.classList.add("c2vm-tle-panel-row");for(const t in e)n.dataset[t]=e[t];const t=document.createElement("img");t.src="Media/Game/Icons/AdvisorNotifications.svg",t.classList.add("c2vm-tle-panel-notification-image");const a=document.createElement("div");a.classList.add("c2vm-tle-panel-notification-text"),a.innerHTML=e.label,n.appendChild(t),n.appendChild(a),l.appendChild(n)}}i&&d();const s=document.querySelectorAll('[data-type="c2vm-tle-panel-option"]');for(const e of s)e.onclick=o;const m=document.querySelectorAll('[data-type="c2vm-tle-panel-pattern"]');for(const e of m)e.dataset.ways&&e.dataset.ways>0&&(e.onclick=a);const p=document.querySelectorAll('[data-type="c2vm-tle-panel-button"]');for(const e of p)e.onclick=r;engine.call("C2VM-TLE-PatternChanged","").then(t),engine.call("C2VM-TLE-OptionChanged","").then(c)};engine.call("C2VM-TLE-RequestMenuData").then(s),engine.on("C2VM-TLE-Event-UpdateMenu",s);const m=e=>{const n=e.currentTarget.dataset,t=e.currentTarget.querySelector(".c2vm-tle-lane-panel-checkbox-checkmark");t&&t.classList.remove("c2vm-tle-lane-panel-checkbox-checkmark-checked"),"False"==n.value?(n.value="True",t&&t.classList.add("c2vm-tle-lane-panel-checkbox-checkmark-checked")):n.value="False"},p=e=>{const n=e.currentTarget.dataset;if("C2VM-TLE-CustomLaneDirectionChanged"==n.engineEventName){let t=!0,a=!0,l=!0,c=!0;const o=e.currentTarget.parentElement.parentElement.dataset,r=e.currentTarget.parentElement.querySelectorAll('[data-item-type="checkbox"]');for(const e of r)"m_BanLeft"==e.dataset.key&&"True"==e.dataset.value&&(t=!1),"m_BanRight"==e.dataset.key&&"True"==e.dataset.value&&(a=!1),"m_BanStraight"==e.dataset.key&&"True"==e.dataset.value&&(l=!1),"m_BanUTurn"==e.dataset.key&&"True"==e.dataset.value&&(c=!1);const i=JSON.stringify({m_Type:0,m_Position:{x:o.worldX,y:o.worldY,z:o.worldZ},m_Tangent:{x:o.tangentX,y:o.tangentY,z:o.tangentZ},m_GroupIndex:o.groupIndex,m_LaneIndex:o.laneIndex,m_Restriction:{m_BanLeft:t,m_BanRight:a,m_BanStraight:l,m_BanUTurn:c}});engine.call(n.engineEventName,i).then((()=>{}));const d=e.currentTarget.parentElement;for(d.style.display="none";d.firstChild;)d.removeChild(d.lastChild);const s=document.querySelectorAll(".c2vm-tle-lane-button");for(const e of s)e.style.display="block";e.stopPropagation()}},u=e=>{const n=e.currentTarget.dataset;e.currentTarget.querySelector(".c2vm-tle-lane-panel")&&0==e.currentTarget.querySelector(".c2vm-tle-lane-panel").children.length&&engine.call("C2VM-TLE-RequestLaneManagementData",JSON.stringify({m_Position:{x:n.worldX,y:n.worldY,z:n.worldZ},m_Tangent:{x:n.tangentX,y:n.tangentY,z:n.tangentZ},m_GroupIndex:n.groupIndex,m_LaneIndex:n.laneIndex,m_Restriction:{m_BanLeft:!0,m_BanRight:!0,m_BanStraight:!0,m_BanUTurn:!0}})).then(v);const t=document.querySelectorAll(".c2vm-tle-lane-button");for(const n of t)n!==e.currentTarget&&(n.style.display="none")},v=e=>{try{const n=JSON.parse(e),t=n[0],a=document.querySelector(`[data-world-x="${t.x}"][data-world-y="${t.y}"][data-world-z="${t.z}"]`).querySelector(".c2vm-tle-lane-panel");if(!a)return void console.log("Panel not found.",t,a);for(a.style.display="block";a.firstChild;)a.removeChild(a.lastChild);const l=document.createElement("div");l.classList.add("c2vm-tle-lane-panel-row"),l.style.color="white",l.innerHTML="Lane Direction",a.appendChild(l);for(const e of n)if(e.itemType){if("checkbox"==e.itemType){const n=document.createElement("div");n.classList.add("c2vm-tle-lane-panel-row");for(const t in e)n.dataset[t]=e[t];"True"==e.value?n.dataset.value="False":n.dataset.value="True",n.innerHTML+=`\n                        <div class="c2vm-tle-lane-panel-checkbox">\n                            <div class="c2vm-tle-lane-panel-checkbox-checkmark ${"True"==n.dataset.value?"c2vm-tle-lane-panel-checkbox-checkmark-checked":""}"></div>\n                        </div>\n                        <span class="c2vm-tle-panel-secondary-text">${e.label}</span>\n                    `,n.onclick=m,a.appendChild(n)}if("button"==e.itemType){const n=document.createElement("div");n.classList.add("c2vm-tle-lane-panel-row");for(const t in e)n.dataset[t]=e[t];n.innerHTML+=`\n                        <button class="c2vm-tle-lane-panel-button">${e.label}</button>\n                    `,n.onclick=p,a.appendChild(n)}}}catch(e){console.log(e)}};engine.on("C2VM-TLE-Event-ConnectPosition",(function(e){const n=JSON.parse(e);for(const e of n.source){const n=document.createElement("div");n.classList.add("c2vm-tle-lane-button"),n.innerHTML='\n                <img src="Media/Game/Icons/RoadsServices.svg">\n                <div class="c2vm-tle-lane-panel"></div>\n            ',n.dataset.worldX=e.world.x,n.dataset.worldY=e.world.y,n.dataset.worldZ=e.world.z,n.dataset.tangentX=e.world.tangentX,n.dataset.tangentY=e.world.tangentY,n.dataset.tangentZ=e.world.tangentZ,n.dataset.groupIndex=e.world.groupIndex,n.dataset.laneIndex=e.world.laneIndex,n.style.position="absolute",n.onclick=u,document.querySelector("body").appendChild(n)}}));setInterval((()=>{const e=[...document.querySelectorAll("div.c2vm-tle-lane-button")].map((e=>({x:e.dataset.worldX,y:e.dataset.worldY,z:e.dataset.worldZ})));0!=e.length&&engine.call("C2VM-TLE-RequestWorldToScreen",JSON.stringify(e)).then((e=>{try{const n=JSON.parse(e);for(const e of n){const n=document.querySelector(`div.c2vm-tle-lane-button[data-world-x="${e.world.x}"][data-world-y="${e.world.y}"][data-world-z="${e.world.z}"]`);n&&(n.style.left=e.screen.x-17+"px",n.style.bottom=e.screen.y-17+"px")}}catch(e){console.log(e)}}))}),100),engine.call("C2VM-TLE-RequestMenuData").then(s);const h=document.querySelector("body"),g={attributes:!0,childList:!0,subtree:!0};new MutationObserver(((e,n)=>{const t=document.querySelector("button.selected.item_KJ3.item-hover_WK8.item-active_Spn > img"),a=document.querySelector("div.c2vm-tle-panel");a&&(t&&"Media/Game/Icons/TrafficLights.svg"==t.src?a.style.display="block":"none"!=a.style.display&&(a.style.display="none",d(),i()))})).observe(h,g)}
        """);
    }

    protected override void OnUpdate()
    {
    }

    protected static string GetLocale()
    {
        string locale = GameManager.instance.localizationManager.activeLocaleId;
        List<string> supportedLocales = new List<string>
        {
            "en-US",
            "de-DE",
            "fr-FR",
            "ja-JP",
            "ko-KR",
            "pt-BR",
            "ru-RU",
            "zh-HANS",
            "zh-HANT"
        };
        if (!supportedLocales.Contains(locale))
        {
            locale = supportedLocales[0];
        }
        return locale;
    }

    protected void UpdateEntity()
    {
        if (m_SelectedEntity != Entity.Null)
        {
            if (m_SubLaneLookup.HasBuffer(m_SelectedEntity))
            {
                DynamicBuffer<SubLane> buffer = m_SubLaneLookup[m_SelectedEntity];
                foreach (SubLane subLane in buffer)
                {
                    EntityManager.AddComponentData(subLane.m_SubLane, default(Updated));
                }
            }
            if (m_ConnectedEdgeLookup.HasBuffer(m_SelectedEntity))
            {
                DynamicBuffer<ConnectedEdge> buffer = m_ConnectedEdgeLookup[m_SelectedEntity];
                foreach (ConnectedEdge connectedEdge in buffer)
                {
                    EntityManager.AddComponentData(connectedEdge.m_Edge, default(Updated));
                    if (m_EdgeLookup.HasComponent(connectedEdge.m_Edge))
                    {
                        Edge edge = m_EdgeLookup[connectedEdge.m_Edge];
                        EntityManager.AddComponentData(edge.m_Start, default(Updated));
                        EntityManager.AddComponentData(edge.m_End, default(Updated));
                    }
                }
            }
            EntityManager.AddComponentData(m_SelectedEntity, default(Updated));
        }
    }

    protected void ResetLaneManagement(string input)
    {
        if (m_SelectedEntity != Entity.Null)
        {
            EntityManager.RemoveComponent<CustomLaneDirection>(m_SelectedEntity);
            UpdateEntity();
            m_IsLaneManagementToolOpen = false;
            TriggerUpdateMenu();
        }
    }

    protected void ResetMenuState()
    {
        m_IsLaneManagementToolOpen = false;

        m_Options.Clear();

        m_SelectedPattern = 0;

        if (m_CustomTrafficLightsLookup.HasComponent(m_SelectedEntity))
        {
            m_SelectedPattern = m_CustomTrafficLightsLookup[m_SelectedEntity].GetPattern(m_Ways);
        }

        foreach(int pattern in Enum.GetValues(typeof(TrafficLightPatterns.Pattern)))
        {
            string key = Enum.GetName(typeof(TrafficLightPatterns.Pattern), pattern);
            if ((pattern & m_SelectedPattern) != 0)
            {
                m_Options[key] = 1;
            }
        }
    }

    public void UpdateSelectedEntity(Entity entity)
    {
        if (entity != m_SelectedEntity && entity != Entity.Null && m_SelectedEntity != Entity.Null)
        {
            m_ShowNotificationUnsaved = true;
            TriggerUpdateMenu();
            return;
        }

        if (entity != m_SelectedEntity)
        {
            m_ShowNotificationUnsaved = false;

            // Clean up old entity
            if (m_ConnectPositionSourceLookup.HasBuffer(m_SelectedEntity))
            {
                EntityManager.RemoveComponent<ConnectPositionSource>(m_SelectedEntity);
            }

            if (m_ConnectPositionTargetLookup.HasBuffer(m_SelectedEntity))
            {
                EntityManager.RemoveComponent<ConnectPositionTarget>(m_SelectedEntity);
            }

            m_SelectedEntity = entity;

            m_Ways = 0;

            m_SelectedPattern = 0;

            // Retrieve info of new entity
            if (m_SubLaneLookup.HasBuffer(m_SelectedEntity))
            {
                Dictionary<float3, bool> lanes = new Dictionary<float3, bool>();
                DynamicBuffer<SubLane> buffer = m_SubLaneLookup[m_SelectedEntity];
                foreach (SubLane subLane in buffer)
                {
                    if (m_SecondaryLaneLookup.HasComponent(subLane.m_SubLane))
                    {
                        continue;
                    }

                    if (m_PedestrianLaneLookup.HasComponent(subLane.m_SubLane))
                    {
                        continue;
                    }

                    if (!m_CarLaneLookup.HasComponent(subLane.m_SubLane) && !m_TrackLaneLookup.HasComponent(subLane.m_SubLane))
                    {
                        continue;
                    }

                    if (m_MasterLaneLookup.HasComponent(subLane.m_SubLane) || !m_SlaveLaneLookup.HasComponent(subLane.m_SubLane))
                    {
                        if (m_CurveLookup.HasComponent(subLane.m_SubLane))
                        {
                            Curve curve = m_CurveLookup[subLane.m_SubLane];
                            if (lanes.ContainsKey(curve.m_Bezier.a))
                            {
                                continue;
                            }
                            lanes[curve.m_Bezier.a] = true;
                        }
                    }
                }
                m_Ways = lanes.Count;
            }

            ResetMenuState();

            TriggerUpdateMenu();
        }
    }

    protected void TriggerUpdateMenu()
    {
        m_View.TriggerEvent("C2VM-TLE-Event-UpdateMenu", RequestMenuData());
    }

    protected void ToggleLaneManagement(string value)
    {
        if (!m_IsLaneManagementToolOpen)
        {
            m_View.TriggerEvent("C2VM-TLE-Event-ConnectPosition", ConnectPosition());
        }
        m_IsLaneManagementToolOpen = !m_IsLaneManagementToolOpen;
        TriggerUpdateMenu();
    }

    protected string ConnectPosition()
    {
        var source = new ArrayList();
        var target = new ArrayList();
        if (m_SelectedEntity != Entity.Null)
        {
            if (m_ConnectPositionSourceLookup.HasBuffer(m_SelectedEntity))
            {
                DynamicBuffer<ConnectPositionSource> connectPositionSourceBuffer = m_ConnectPositionSourceLookup[m_SelectedEntity];
                Dictionary<Unity.Mathematics.float3, bool> sourceExist = new Dictionary<Unity.Mathematics.float3, bool>();
                for (int i = 0; i < connectPositionSourceBuffer.Length; i++)
                {
                    Unity.Mathematics.float3 worldPosition = connectPositionSourceBuffer[i].m_Position;
                    sourceExist.Add(worldPosition, true);
                    source.Add(new {
                        world = new {
                            x = worldPosition.x,
                            y = worldPosition.y,
                            z = worldPosition.z,
                            tangentX = connectPositionSourceBuffer[i].m_Tangent.x,
                            tangentY = connectPositionSourceBuffer[i].m_Tangent.y,
                            tangentZ = connectPositionSourceBuffer[i].m_Tangent.z,
                            groupIndex = connectPositionSourceBuffer[i].m_GroupIndex,
                            laneIndex = connectPositionSourceBuffer[i].m_LaneIndex,
                        }
                    });
                }

                // Remove CustomLaneDirection that is no longer exists
                if (m_CustomLaneDirectionLookup.HasBuffer(m_SelectedEntity))
                {
                    DynamicBuffer<CustomLaneDirection> customLaneDirectionBuffer = m_CustomLaneDirectionLookup[m_SelectedEntity];
                    for (int i = 0; i < customLaneDirectionBuffer.Length; i++)
                    {
                        Unity.Mathematics.float3 customLaneDirectionPosition = customLaneDirectionBuffer[i].m_Position;
                        if (!sourceExist.ContainsKey(customLaneDirectionPosition))
                        {
                            customLaneDirectionBuffer.RemoveAt(i);
                            i--;
                        }
                    }
                }
                // Build default config if CustomLaneDirection doesn't exist
                else
                {
                    DynamicBuffer<CustomLaneDirection> customLaneDirectionBuffer = EntityManager.AddBuffer<CustomLaneDirection>(m_SelectedEntity);
                    DefaultLaneDirection.Build(ref customLaneDirectionBuffer, ref connectPositionSourceBuffer);
                }
            }

            if (m_ConnectPositionTargetLookup.HasBuffer(m_SelectedEntity))
            {
            }
        }
        return JsonConvert.SerializeObject(new {source, target});
    }

    protected string RequestWorldToScreen(string input)
    {
        List<WorldToScreen> worldList = JsonConvert.DeserializeObject<List<WorldToScreen>>(input);
        var screenList = new ArrayList();
        foreach (WorldToScreen worldPosition in worldList)
        {
            Unity.Mathematics.float3 screenPosition = Camera.main.WorldToScreenPoint(new Unity.Mathematics.float3(worldPosition.x, worldPosition.y, worldPosition.z));
            screenList.Add(new {
                world = new {
                    x = worldPosition.x,
                    y = worldPosition.y,
                    z = worldPosition.z,
                },
                screen = new {
                    x = screenPosition.x,
                    y = screenPosition.y,
                    z = screenPosition.z,
                }
            });
        }
        return JsonConvert.SerializeObject(screenList);
    }

    protected string CustomLaneDirectionChanged(string input)
    {
        // System.Console.WriteLine(input);
        if (m_SelectedEntity == Entity.Null)
        {
            return "{}";
        }

        CustomLaneDirection direction = default;
        if (input.Length > 0)
        {
            direction = JsonConvert.DeserializeObject<CustomLaneDirection>(input);
            direction.m_Initialised = true;

            // System.Console.WriteLine($"connection.m_Restriction m_BanLeft {connection.m_Restriction.m_BanLeft} m_BanStraight {connection.m_Restriction.m_BanStraight} m_BanRight {connection.m_Restriction.m_BanRight} m_BanUTurn {connection.m_Restriction.m_BanUTurn}");
                    
            if (m_CustomLaneDirectionLookup.HasBuffer(m_SelectedEntity))
            {
                DynamicBuffer<CustomLaneDirection> buffer = m_CustomLaneDirectionLookup[m_SelectedEntity];
                bool foundExistingDirection = false;
                for (int i = 0; i < buffer.Length; i++)
                {
                    CustomLaneDirection existingDirection = buffer[i];
                    if (existingDirection.Equals(direction))
                    {
                        buffer[i] = direction;
                        foundExistingDirection = true;
                        break;
                    }
                }
                if (!foundExistingDirection)
                {
                    m_CustomLaneDirectionLookup[m_SelectedEntity].Add(direction);
                }
            }
            else
            {
                DynamicBuffer<CustomLaneDirection> buffer = EntityManager.AddBuffer<CustomLaneDirection>(m_SelectedEntity);
                buffer.Add(direction);
            }
            UpdateEntity();
        }
        return "{}";
        // return RequestLaneManagementData(JsonConvert.SerializeObject(direction));
    }

    protected string RequestLaneManagementData(string input)
    {
        if (m_SelectedEntity == Entity.Null)
        {
            return "{}";
            // return JsonConvert.SerializeObject(default(CustomLaneDirection));
        }

        CustomLaneDirection direction = default;
        if (input.Length > 0)
        {
            direction = JsonConvert.DeserializeObject<CustomLaneDirection>(input);
            if (m_CustomLaneDirectionLookup.HasBuffer(m_SelectedEntity))
            {
                DynamicBuffer<CustomLaneDirection> buffer = m_CustomLaneDirectionLookup[m_SelectedEntity];
                CustomLaneDirection.Get(buffer, direction.m_Position, direction.m_Tangent, direction.m_GroupIndex, direction.m_LaneIndex, out CustomLaneDirection directionFound);
                direction.m_Restriction = directionFound.m_Restriction;
            }
        }
    
        var menu = new ArrayList();
        menu.Add(new {
            x = direction.m_Position.x,
            y = direction.m_Position.y,
            z = direction.m_Position.z,
            tangentX = direction.m_Tangent.x,
            tangentY = direction.m_Tangent.y,
            tangentZ = direction.m_Tangent.z,
            groupIndex = direction.m_GroupIndex,
            laneIndex = direction.m_LaneIndex,
        });
        menu.Add(new MenuItemOption{label = m_ResourceManager.GetString("Left"), key = "m_BanLeft", value = direction.m_Restriction.m_BanLeft.ToString()});
        menu.Add(new MenuItemOption{label = m_ResourceManager.GetString("Ahead"), key = "m_BanStraight", value = direction.m_Restriction.m_BanStraight.ToString()});
        menu.Add(new MenuItemOption{label = m_ResourceManager.GetString("Right"), key = "m_BanRight", value = direction.m_Restriction.m_BanRight.ToString()});
        menu.Add(new MenuItemOption{label = m_ResourceManager.GetString("UTurn"), key = "m_BanUTurn", value = direction.m_Restriction.m_BanUTurn.ToString()});
        menu.Add(new MenuItemButton{label = m_ResourceManager.GetString("Save"), key = "save", value = "1", engineEventName = "C2VM-TLE-CustomLaneDirectionChanged"});
        return JsonConvert.SerializeObject(menu);
    }

    protected void RequestMenuSave(string value)
    {
        UpdateSelectedEntity(Entity.Null);
        TriggerUpdateMenu();
    }

    protected string RequestMenuData()
    {
        var menu = new ArrayList();
        if (m_SelectedEntity != Entity.Null)
        {
            menu.Add(new MenuItemTitle{title = m_ResourceManager.GetString("TrafficSignal")});
            menu.Add(new MenuItemPattern{label = m_ResourceManager.GetString("Vanilla"), ways = m_Ways, pattern = (int) TrafficLightPatterns.Pattern.Vanilla});
            if (TrafficLightPatterns.IsValidPattern(m_Ways, (int) TrafficLightPatterns.Pattern.SplitPhasing))
            {
                menu.Add(new MenuItemPattern{label = m_ResourceManager.GetString("SplitPhasing"), ways = m_Ways, pattern = (int) TrafficLightPatterns.Pattern.SplitPhasing});
            }
            if (TrafficLightPatterns.IsValidPattern(m_Ways, (int) TrafficLightPatterns.Pattern.SplitPhasingAdvanced))
            {
                menu.Add(new MenuItemPattern{label = m_ResourceManager.GetString("AdvancedSplitPhasing"), ways = m_Ways, pattern = (int) TrafficLightPatterns.Pattern.SplitPhasingAdvanced});
            }
            if (TrafficLightPatterns.IsValidPattern(m_Ways, (int) TrafficLightPatterns.Pattern.ProtectedCentreTurn))
            {
                if (m_CityConfigurationSystem.leftHandTraffic)
                {
                    menu.Add(new MenuItemPattern{label = m_ResourceManager.GetString("ProtectedRightTurns"), ways = m_Ways, pattern = (int) TrafficLightPatterns.Pattern.ProtectedCentreTurn});
                }
                else
                {
                    menu.Add(new MenuItemPattern{label = m_ResourceManager.GetString("ProtectedLeftTurns"), ways = m_Ways, pattern = (int) TrafficLightPatterns.Pattern.ProtectedCentreTurn});
                }
            }
            menu.Add(default(MenuItemDivider));
            menu.Add(new MenuItemTitle{title = m_ResourceManager.GetString("Options")});
            menu.Add(new MenuItemOption{label = m_ResourceManager.GetString("ExclusivePedestrianPhase"), key = TrafficLightPatterns.Pattern.ExclusivePedestrian.ToString(), value = "1"});
            if (m_CityConfigurationSystem.leftHandTraffic)
            {
                menu.Add(new MenuItemOption{label = m_ResourceManager.GetString("AlwaysGreenLeftTurns"), key = TrafficLightPatterns.Pattern.AlwaysGreenKerbsideTurn.ToString(), value = "0"});
            }
            else
            {
                menu.Add(new MenuItemOption{label = m_ResourceManager.GetString("AlwaysGreenRightTurns"), key = TrafficLightPatterns.Pattern.AlwaysGreenKerbsideTurn.ToString(), value = "0"});
            }
            menu.Add(default(MenuItemDivider));
            menu.Add(new MenuItemTitle{title = m_ResourceManager.GetString("LaneDirectionTool")});
            if (m_IsLaneManagementToolOpen)
            {
                menu.Add(new MenuItemButton{label = m_ResourceManager.GetString("Close"), key = "status", value = "1", engineEventName = "C2VM-TLE-ToggleLaneManagement"});
            }
            else
            {
                menu.Add(new MenuItemButton{label = m_ResourceManager.GetString("Open"), key = "status", value = "0", engineEventName = "C2VM-TLE-ToggleLaneManagement"});
            }
            menu.Add(new MenuItemButton{label = m_ResourceManager.GetString("Reset"), key = "status", value = "0", engineEventName = "C2VM-TLE-ResetLaneManagement"});
            menu.Add(default(MenuItemDivider));
            menu.Add(new MenuItemButton{label = m_ResourceManager.GetString("Save"), key = "save", value = "1", engineEventName = "C2VM-TLE-RequestMenuSave"});
            if (m_ShowNotificationUnsaved)
            {
                menu.Add(default(MenuItemDivider));
                menu.Add(new MenuItemNotification{label = m_ResourceManager.GetString("PleaseSave"), notificationType = "warning"});
            }
        }
        else
        {
            menu.Add(new MenuItemTitle{title = "<div style=\"margin: 20rem auto; flex: 1; text-align: center;\">" + m_ResourceManager.GetString("PleaseSelectJunction") + "</div>"});
        }
        return JsonConvert.SerializeObject(menu);
    }

    protected string OptionChanged(string input)
    {
        if (input.Length > 0)
        {
            string[] splitInput = input.Split('_');
            if (splitInput.Length == 2)
            {
                string key = splitInput[0];
                int.TryParse(splitInput[1], out int value);
                if (value != 1)
                {
                    value = 0;
                }
                m_Options[key] = value;
            }
        }
        UpdatePatterns();
        string result = JsonConvert.SerializeObject(m_Options);
        // Console.WriteLine($"UISystem OptionChanged {input} {result}");
        return result;
    }

    protected string PatternChanged(string input)
    {
        if (input.Length > 0)
        {
            string[] splitInput = input.Split('_');
            if (splitInput.Length == 2)
            {
                int ways;
                int pattern;
                int.TryParse(splitInput[0], out ways);
                int.TryParse(splitInput[1], out pattern);
                if (!TrafficLightPatterns.IsValidPattern(ways, pattern))
                {
                    pattern = 0;
                }
                m_SelectedPattern = pattern;
            }
        }
        UpdatePatterns();
        string result = JsonConvert.SerializeObject(new {
            ways = m_Ways,
            pattern = m_SelectedPattern
        });
        // Console.WriteLine($"UISystem PatternChanged {input} {result}");
        return result;
    }

    protected void UpdatePatterns()
    {
        if (
            m_Options.ContainsKey(TrafficLightPatterns.Pattern.ExclusivePedestrian.ToString()) &&
            m_Options[TrafficLightPatterns.Pattern.ExclusivePedestrian.ToString()] == 1
        )
        {
            m_SelectedPattern = m_SelectedPattern | (int) TrafficLightPatterns.Pattern.ExclusivePedestrian;
        }
        else
        {
            m_SelectedPattern = m_SelectedPattern & (int) ~TrafficLightPatterns.Pattern.ExclusivePedestrian;
        }

        if (
            m_Options.ContainsKey(TrafficLightPatterns.Pattern.AlwaysGreenKerbsideTurn.ToString()) &&
            m_Options[TrafficLightPatterns.Pattern.AlwaysGreenKerbsideTurn.ToString()] == 1
        )
        {
            m_SelectedPattern = m_SelectedPattern | (int) TrafficLightPatterns.Pattern.AlwaysGreenKerbsideTurn;
        }
        else
        {
            m_SelectedPattern = m_SelectedPattern & (int) ~TrafficLightPatterns.Pattern.AlwaysGreenKerbsideTurn;
        }

        if (m_SelectedEntity == Entity.Null)
        {
            return;
        }

        if (!m_CustomTrafficLightsLookup.HasComponent(m_SelectedEntity))
        {
            EntityManager.AddComponentData(m_SelectedEntity, new CustomTrafficLights(m_SelectedPattern));
        }
        else
        {
            CustomTrafficLights customTrafficLights = m_CustomTrafficLightsLookup[m_SelectedEntity];
            customTrafficLights.SetPattern(m_SelectedPattern);
            m_CustomTrafficLightsLookup[m_SelectedEntity] = customTrafficLights;
        }

        UpdateEntity();
    }
}