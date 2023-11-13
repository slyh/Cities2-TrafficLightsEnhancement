if (!document.querySelector("div.traffic-lights-enhancement-panel")) {
    const panelDiv = document.createElement("div");
    panelDiv.innerHTML = `
        <div class="traffic-lights-enhancement-panel">
            <div class="traffic-lights-enhancement-panel-header">
                <img class="traffic-lights-enhancement-panel-header-image" src="Media/Game/Icons/TrafficLights.svg" />
                <div class="traffic-lights-enhancement-panel-header-title">Traffic Lights Enhancement</div>
            </div>
            <div class="traffic-lights-enhancement-panel-content">
                <div class="traffic-lights-enhancement-panel-row">
                    Three-Way Junction
                </div>
                <div class="traffic-lights-enhancement-panel-row" data-ways="3" data-pattern="0">
                    <div class="traffic-lights-enhancement-panel-radio">
                        <div class="traffic-lights-enhancement-panel-radio-bullet"></div>
                    </div>
                    <span class="traffic-lights-enhancement-panel-secondary-text">Vanilla</span>
                </div>
                <div class="traffic-lights-enhancement-panel-row" data-ways="3" data-pattern="1">
                    <div class="traffic-lights-enhancement-panel-radio">
                        <div class="traffic-lights-enhancement-panel-radio-bullet"></div>
                    </div>
                    <span class="traffic-lights-enhancement-panel-secondary-text">Split Phasing</span>
                </div>
                <div class="traffic-lights-enhancement-panel-row-divider"></div>
                <div class="traffic-lights-enhancement-panel-row">
                    Four-Way Junction
                </div>
                <div class="traffic-lights-enhancement-panel-row" data-ways="4" data-pattern="0">
                    <div class="traffic-lights-enhancement-panel-radio">
                        <div class="traffic-lights-enhancement-panel-radio-bullet"></div>
                    </div>
                    <span class="traffic-lights-enhancement-panel-secondary-text">Vanilla</span>
                </div>
                <div class="traffic-lights-enhancement-panel-row" data-ways="4" data-pattern="1">
                    <div class="traffic-lights-enhancement-panel-radio">
                        <div class="traffic-lights-enhancement-panel-radio-bullet"></div>
                    </div>
                    <span class="traffic-lights-enhancement-panel-secondary-text">Split Phasing</span>
                </div>
                <div class="traffic-lights-enhancement-panel-row" data-ways="4" data-pattern="2">
                    <div class="traffic-lights-enhancement-panel-radio">
                        <div class="traffic-lights-enhancement-panel-radio-bullet"></div>
                    </div>
                    <span class="traffic-lights-enhancement-panel-secondary-text">Some Weird Mode</span>
                </div>
            </div>
        </div>
        <style>
            .traffic-lights-enhancement-panel {
                width: 300rem;
                position: absolute;
                top: calc(10rem+ var(--floatingToggleSize) +6rem);
                left: 10rem;
            }
            .traffic-lights-enhancement-panel-header {
                border-radius: 4rem 4rem 0rem 0rem;
                background-color: rgba(24, 33, 51, 0.6);
                backdrop-filter: blur(5px);
                color: rgba(75, 195, 241, 1);
                font-size: 14rem;
                padding: 6rem 10rem;
                min-height: 36rem;
                display: flex;
                flex-direction: row;
                align-items: center;
            }
            .traffic-lights-enhancement-panel-header-image {
                width: 24rem;
                height: 24rem;
            }
            .traffic-lights-enhancement-panel-header > .traffic-lights-enhancement-panel-header-title {
                text-transform: uppercase;
                flex: 1;
                text-align: center;
                overflow-x: hidden;
                overflow-y: hidden;
                text-overflow: ellipsis;
                white-space: nowrap;
            }
            .traffic-lights-enhancement-panel-content {
                border-radius: 0rem 0rem 4rem 4rem;
                background-color: rgba(42, 55, 83, 0.437500);
                backdrop-filter: blur(5.000000px);
                color: rgba(255, 255, 255, 1.000000);
                flex: 1.000000;
                position: relative;
                padding: 6rem;
            }
            .traffic-lights-enhancement-panel-row {
                padding: 3rem 8rem;
                width: 100%;
                display: flex;
            }
            .traffic-lights-enhancement-panel-row-divider {
                height: 2px;
                width: auto;
                border: 2px solid rgba(255, 255, 255, 0.100000);
                margin: 6rem -6rem;
            }
            .traffic-lights-enhancement-panel-secondary-text {
                color: rgba(217, 217, 217, 1.000000);
            }
            .traffic-lights-enhancement-panel-radio {
                border: 2px solid rgba(32, 164, 255, 1.000000);
                margin: 0 10rem 0 0;
                width: 20.000000rem;
                height: 20.000000rem;
                padding: 3px;
                border-radius: 50.000000%;
            }
            .traffic-lights-enhancement-panel-radio-bullet {
                width: 100.000000%;
                height: 100.000000%;
                background-color:  white;
                opacity: 0.000000;
                border-radius: 50.000000%;
                transition-property: opacity;
                transition-duration: 0.150000s;
                transition-delay: 0.000000s;
                transition-timing-function: ease;
            }
            .traffic-lights-enhancement-panel-radio-bullet-checked {
                opacity: 1.000000;
            }
        </style>
    `;
    const container = document.querySelector("body");
    container.appendChild(panelDiv);

    const engineCallback = (result) => {
        const buttons = document.querySelectorAll(".traffic-lights-enhancement-panel-radio-bullet");
        for (const button of buttons) {
            button.classList.remove("traffic-lights-enhancement-panel-radio-bullet-checked");
        }
        const m_SelectedPattern = JSON.parse(result);
        console.log(`TrafficLightsEnhancementOnPatternChanged result ${result} m_SelectedPattern ${m_SelectedPattern}`);
        for (const ways in m_SelectedPattern) {
            const pattern = m_SelectedPattern[ways];
            const button = document.querySelector(`.traffic-lights-enhancement-panel-row[data-ways="${ways}"][data-pattern="${pattern}"] .traffic-lights-enhancement-panel-radio-bullet`);
            if (button) {
                button.classList.add("traffic-lights-enhancement-panel-radio-bullet-checked");
            }
        }
    };
    engine.call('TrafficLightsEnhancementOnPatternChanged', "3_0").then(engineCallback);
    engine.call('TrafficLightsEnhancementOnPatternChanged', "4_0").then(engineCallback);

    const listener = (event) => {
        const dataset = event.currentTarget.dataset;
        const buttons = document.querySelectorAll(".traffic-lights-enhancement-panel-radio-bullet");
        for (const button of buttons) {
            button.classList.remove("traffic-lights-enhancement-panel-radio-bullet-checked");
        }
        console.log(event.currentTarget, dataset, dataset.ways, dataset.pattern);
        engine.call('TrafficLightsEnhancementOnPatternChanged', `${dataset.ways}_${dataset.pattern}`).then(engineCallback);
    };
    
    const items = document.querySelectorAll(".traffic-lights-enhancement-panel-row");
    for (const item of items) {
        if (item.dataset.ways > 0) {
            item.onclick = listener;
        }
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