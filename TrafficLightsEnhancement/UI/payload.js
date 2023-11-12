if (!document.querySelector("div.traffic-lights-enhancement-panel")) {
    const panelDiv = document.createElement("div");
    panelDiv.innerHTML = `
        <div class="traffic-lights-enhancement-panel">
            <div class="traffic-lights-enhancement-panel-header">
                <div class="traffic-lights-enhancement-panel-title-bar">
                    Traffic Lights Enhancement
                </div>
            </div>
            <div class="traffic-lights-enhancement-panel-content">
                <div>
                    Selected Pattern: <div class="traffic-lights-enhancement-panel-selected-pattern">Undefined</div>
                </div>
                <div class="traffic-lights-enhancement-panel-subcontent">
                    <a
                        class="traffic-lights-enhancement-panel-item"
                        data-pattern="0"
                    >
                        Vanilla
                        <div class="traffic-lights-enhancement-panel-radio">
                            <div class="traffic-lights-enhancement-panel-radio-bullet traffic-lights-enhancement-panel-radio-bullet-checked"></div>
                        </div>
                    </a>
                    <a
                        class="traffic-lights-enhancement-panel-item"
                        data-pattern="2"
                    >
                        Split Phasing
                        <div class="traffic-lights-enhancement-panel-radio">
                            <div class="traffic-lights-enhancement-panel-radio-bullet"></div>
                        </div>
                    </a>
                    <a
                        class="traffic-lights-enhancement-panel-item"
                        data-pattern="4"
                    >
                        Some Weird Mode
                        <div class="traffic-lights-enhancement-panel-radio">
                            <div class="traffic-lights-enhancement-panel-radio-bullet traffic-lights-enhancement-panel-radio-bullet-checked"></div>
                        </div>
                    </a>
                </div>
            </div>
        </div>
        <style>
            .traffic-lights-enhancement-panel {
                width: 200px;
                height: 200px;
                position: absolute;
                left: 50px;
                top: 300px;
                background-color: white;
            }
            /*
            .traffic-lights-enhancement-panel-header {
                border-top-left-radius:  var(--panelRadius) ;
                border-top-right-radius:  var(--panelRadius) ;
                background-color:  var(--panelColorDark) ;
                backdrop-filter:  var(--panelBlur) ;
                color:  var(--accentColorNormal) ;
            }
            .traffic-lights-enhancement-panel-title-bar {
                font-size:  var(--fontSizeS) ;
                padding-top: 6.000000rem;
                padding-left: 10.000000rem;
                padding-right: 10.000000rem;
                padding-bottom: 6.000000rem;
                min-height: 36.000000rem;
                display: flex;
                flex-direction: row;
                align-items: center;
            }
            .traffic-lights-enhancement-panel-content {
                border-bottom-left-radius:  var(--panelRadius) ;
                border-bottom-right-radius:  var(--panelRadius) ;
                background-color:  var(--panelColorNormal) ;
                backdrop-filter:  var(--panelBlur) ;
                color: rgba(255, 255, 255, 1.000000);
                flex: 1.000000;
                position: relative;
                padding-top: 6.000000rem;
                padding-left: 10.000000rem;
                padding-right: 10.000000rem;
                padding-bottom: 6.000000rem;
            }
            .traffic-lights-enhancement-panel-subcontent {
                --hoverColorNormal: var(--hoverColorBright);
                --activeColorNormal: var(--activeColorBright);
                overflow-x: hidden;
                overflow-y: hidden;
                padding-top: 6.000000rem;
                padding-left: 6.000000rem;
                padding-right: 6.000000rem;
                padding-bottom: 6.000000rem;
                display: flex;
                flex-direction: column;
                align-items: stretch;
                background-color: rgba(255, 255, 255, 0.100000);
                border-top-left-radius:  var(--panelRadiusInnerSIP) ;
                border-top-right-radius:  var(--panelRadiusInnerSIP) ;
                border-bottom-left-radius:  var(--panelRadiusInnerSIP) ;
                border-bottom-right-radius:  var(--panelRadiusInnerSIP) ;
                color:  var(--textColorDim) ;
                text-align: left;
            }
            .traffic-lights-enhancement-panel-radio {
                border-top-color: rgba(32, 164, 255, 1.000000);
                border-left-color: rgba(32, 164, 255, 1.000000);
                border-right-color: rgba(32, 164, 255, 1.000000);
                border-bottom-color: rgba(32, 164, 255, 1.000000);
                margin-top: 0.000000px;
                margin-left: 0.000000px;
                margin-right: 10.000000rem;
                margin-bottom: 0.000000px;
                width: 20.000000rem;
                height: 20.000000rem;
                --bulletColor: white;
                padding-top:  var(--gap3) ;
                padding-right:  var(--gap3) ;
                padding-bottom:  var(--gap3) ;
                padding-left:  var(--gap3) ;
                width: 18.000000rem;
                height: 18.000000rem;
                border-top-style: solid;
                border-left-style: solid;
                border-right-style: solid;
                border-bottom-style: solid;
                border-top-width:  var(--stroke2) ;
                border-left-width:  var(--stroke2) ;
                border-bottom-width:  var(--stroke2) ;
                border-right-width:  var(--stroke2) ;
                border-top-color: rgba(255, 255, 255, 1.000000);
                border-left-color: rgba(255, 255, 255, 1.000000);
                border-right-color: rgba(255, 255, 255, 1.000000);
                border-bottom-color: rgba(255, 255, 255, 1.000000);
                border-top-left-radius: 50.000000% 50.000000%;
                border-top-right-radius: 50.000000% 50.000000%;
                border-bottom-left-radius: 50.000000% 50.000000%;
                border-bottom-right-radius: 50.000000% 50.000000%;
            }
            .traffic-lights-enhancement-panel-radio-bullet {
                width: 100.000000%;
                height: 100.000000%;
                background-color:  var(--bulletColor) ;
                opacity: 0.000000;
                border-top-left-radius: 50.000000% 50.000000%;
                border-top-right-radius: 50.000000% 50.000000%;
                border-bottom-left-radius: 50.000000% 50.000000%;
                border-bottom-right-radius: 50.000000% 50.000000%;
                transition-property: opacity;
                transition-duration: 0.150000s;
                transition-delay: 0.000000s;
                transition-timing-function: ease;
            }
            .traffic-lights-enhancement-panel-radio-bullet-checked {
                opacity: 1.000000;
            }
            */
        </style>
    `;
    document.querySelector("body").appendChild(panelDiv);

    const engineCallback = (result) => {
        console.log("TrafficLightsEnhancementOnPatternChanged result " + result);
        const patternDiv = document.querySelector(".traffic-lights-enhancement-panel-selected-pattern");
        patternDiv.innerHTML = result;
    };
    engine.call('TrafficLightsEnhancementOnPatternChanged', 0).then(engineCallback);

    const listener = (event) => {
        if (event.target.dataset.pattern === undefined) {
            return;
        }
        console.log(event.target, event.target.dataset, event.target.dataset.ways);
        engine.call('TrafficLightsEnhancementOnPatternChanged', parseInt(event.target.dataset.pattern)).then(engineCallback);
    };
    
    const items = document.querySelectorAll(".traffic-lights-enhancement-panel-item");
    for (const item of items) {
        item.onclick = listener;
    }

    const body = document.querySelector("body");
    const config = { attributes: true, childList: true, subtree: true };
    const callback = (mutationList, observer) => {
        const img = document.querySelector("button.selected.item_KJ3.item-hover_WK8.item-active_Spn > img");

        const panel = document.querySelector("div.traffic-lights-enhancement-panel");
        
        if (panel) {
            panel.style.display = (img && img.src == "Media/Game/Icons/TrafficLights.svg") ? "block" : "none";
        }
    };
    const observer = new MutationObserver(callback);
    observer.observe(body, config);
}