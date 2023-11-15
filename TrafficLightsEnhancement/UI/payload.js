if (!document.querySelector("div.c2vm-tle-panel")) {
    const panelDiv = document.createElement("div");
    panelDiv.innerHTML = `
        <div class="c2vm-tle-panel">
            <div class="c2vm-tle-panel-header">
                <img class="c2vm-tle-panel-header-image" src="Media/Game/Icons/TrafficLights.svg" />
                <div class="c2vm-tle-panel-header-title">Traffic Lights Enhancement</div>
            </div>
            <div class="c2vm-tle-panel-content">
                <div class="c2vm-tle-panel-row">
                    Three-Way Junction
                </div>
                <div class="c2vm-tle-panel-row" data-type="c2vm-tle-panel-pattern" data-ways="3" data-pattern="0">
                    <div class="c2vm-tle-panel-radio">
                        <div class="c2vm-tle-panel-radio-bullet"></div>
                    </div>
                    <span class="c2vm-tle-panel-secondary-text">Vanilla</span>
                </div>
                <div class="c2vm-tle-panel-row" data-type="c2vm-tle-panel-pattern" data-ways="3" data-pattern="1">
                    <div class="c2vm-tle-panel-radio">
                        <div class="c2vm-tle-panel-radio-bullet"></div>
                    </div>
                    <span class="c2vm-tle-panel-secondary-text">Split Phasing</span>
                </div>
                <div class="c2vm-tle-panel-row-divider"></div>
                <div class="c2vm-tle-panel-row">
                    Four-Way Junction
                </div>
                <div class="c2vm-tle-panel-row" data-type="c2vm-tle-panel-pattern" data-ways="4" data-pattern="0">
                    <div class="c2vm-tle-panel-radio">
                        <div class="c2vm-tle-panel-radio-bullet"></div>
                    </div>
                    <span class="c2vm-tle-panel-secondary-text">Vanilla</span>
                </div>
                <div class="c2vm-tle-panel-row" data-type="c2vm-tle-panel-pattern" data-ways="4" data-pattern="1">
                    <div class="c2vm-tle-panel-radio">
                        <div class="c2vm-tle-panel-radio-bullet"></div>
                    </div>
                    <span class="c2vm-tle-panel-secondary-text">Split Phasing</span>
                </div>
                <div class="c2vm-tle-panel-row" data-type="c2vm-tle-panel-pattern" data-ways="4" data-pattern="3">
                    <div class="c2vm-tle-panel-radio">
                        <div class="c2vm-tle-panel-radio-bullet"></div>
                    </div>
                    <span class="c2vm-tle-panel-secondary-text">Advanced Split Phasing</span>
                </div>
                <div class="c2vm-tle-panel-row" data-type="c2vm-tle-panel-pattern" data-ways="4" data-pattern="2">
                    <div class="c2vm-tle-panel-radio">
                        <div class="c2vm-tle-panel-radio-bullet"></div>
                    </div>
                    <span class="c2vm-tle-panel-secondary-text">Some Weird Mode</span>
                </div>
                <div class="c2vm-tle-panel-row-divider"></div>
                <div class="c2vm-tle-panel-row">
                    Options
                </div>
                <div class="c2vm-tle-panel-row" data-type="c2vm-tle-panel-option" data-key="ExclusivePedestrian" data-value="0">
                    <div class="c2vm-tle-panel-checkbox">
                        <div class="c2vm-tle-panel-checkbox-checkmark"></div>
                    </div>
                    <span class="c2vm-tle-panel-secondary-text">Exclusive Pedestrian Phase</span>
                </div>
            </div>
        </div>
        <style>
            .c2vm-tle-panel {
                width: 300rem;
                position: absolute;
                top: calc(10rem+ var(--floatingToggleSize) +6rem);
                left: 10rem;
            }
            .c2vm-tle-panel-header {
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
            .c2vm-tle-panel-header-image {
                width: 24rem;
                height: 24rem;
            }
            .c2vm-tle-panel-header > .c2vm-tle-panel-header-title {
                text-transform: uppercase;
                flex: 1;
                text-align: center;
                overflow-x: hidden;
                overflow-y: hidden;
                text-overflow: ellipsis;
                white-space: nowrap;
            }
            .c2vm-tle-panel-content {
                border-radius: 0rem 0rem 4rem 4rem;
                background-color: rgba(42, 55, 83, 0.437500);
                backdrop-filter: blur(5px);
                color: rgba(255, 255, 255, 1);
                flex: 1;
                position: relative;
                padding: 6rem;
            }
            .c2vm-tle-panel-row {
                padding: 3rem 8rem;
                width: 100%;
                display: flex;
            }
            .c2vm-tle-panel-row-divider {
                height: 2px;
                width: auto;
                border: 2px solid rgba(255, 255, 255, 0.1);
                margin: 6rem -6rem;
            }
            .c2vm-tle-panel-secondary-text {
                color: rgba(217, 217, 217, 1);
            }
            .c2vm-tle-panel-radio {
                border: 2px solid rgba(75, 195, 241, 1);
                margin: 0 10rem 0 0;
                width: 20rem;
                height: 20rem;
                padding: 3px;
                border-radius: 50%;
            }
            .c2vm-tle-panel-radio-bullet {
                width: 100%;
                height: 100%;
                background-color:  white;
                opacity: 0;
                border-radius: 50%;
            }
            .c2vm-tle-panel-radio-bullet-checked {
                opacity: 1;
            }
            .c2vm-tle-panel-checkbox {
                margin: 0 10rem 0 0;
                width: 20rem;
                height: 20rem;
                padding: 1px;
                border: 2px solid rgba(255, 255, 255, 0.500000);
                border-radius: 3rem;
            }
            .c2vm-tle-panel-checkbox-checkmark {
                width: 100%;
                height: 100%;
                mask-image: url(Media/Glyphs/Checkmark.svg);
                mask-size: 100% auto;
                background-color: white;
                opacity: 0;
            }
            .c2vm-tle-panel-checkbox-checkmark-checked {
                opacity: 1;
            }
        </style>
    `;
    const container = document.querySelector("body");
    container.appendChild(panelDiv);

    // Patterns
    const resetPatternButtons = () => {
        const buttons = document.querySelectorAll(`[data-type="c2vm-tle-panel-pattern"] .c2vm-tle-panel-radio-bullet`);
        for (const button of buttons) {
            button.classList.remove("c2vm-tle-panel-radio-bullet-checked");
        }
    };

    const enginePatternCallback = (result) => {
        resetPatternButtons();
        const m_SelectedPattern = JSON.parse(result);
        console.log(`C2VM-TLE-PatternChanged result ${result} m_SelectedPattern ${m_SelectedPattern}`);
        for (const i in m_SelectedPattern) {
            const ways = i;
            const pattern = m_SelectedPattern[ways] & 0xFFFF;
            const button = document.querySelector(`[data-type="c2vm-tle-panel-pattern"][data-ways="${ways}"][data-pattern="${pattern}"] .c2vm-tle-panel-radio-bullet`);
            if (button) {
                button.classList.add("c2vm-tle-panel-radio-bullet-checked");
            }
        }
    };
    engine.call('C2VM-TLE-PatternChanged', "3_0").then(enginePatternCallback);
    engine.call('C2VM-TLE-PatternChanged', "4_0").then(enginePatternCallback);

    const patternListener = (event) => {
        resetPatternButtons();
        const dataset = event.currentTarget.dataset;
        console.log(event.currentTarget, dataset, dataset.ways, dataset.pattern);
        engine.call('C2VM-TLE-PatternChanged', `${dataset.ways}_${dataset.pattern}`).then(enginePatternCallback);
    };
    
    const patternItems = document.querySelectorAll(`[data-type="c2vm-tle-panel-pattern"]`);
    for (const item of patternItems) {
        if (item.dataset.ways > 0) {
            item.onclick = patternListener;
        }
    }

    // Options
    const resetOptionCheckboxes = () => {
        const checkboxes = document.querySelectorAll(`[data-type="c2vm-tle-panel-option"] .c2vm-tle-panel-checkbox-checkmark`);
        for (const checkbox of checkboxes) {
            checkbox.classList.remove("c2vm-tle-panel-checkbox-checkmark-checked");
        }
    };

    const engineOptionCallback = (result) => {
        resetOptionCheckboxes();
        const options = JSON.parse(result);
        console.log(`C2VM-TLE-OptionChanged result ${result} m_Options ${options}`);
        for (const key in options) {
            const value = options[key];
            const row = document.querySelector(`[data-type="c2vm-tle-panel-option"][data-key="${key}"]`);
            row.dataset.value = value;
            const checkbox = row.querySelector(`.c2vm-tle-panel-checkbox-checkmark`);
            if (checkbox && value === 1) {
                checkbox.classList.add("c2vm-tle-panel-checkbox-checkmark-checked");
            }
        }
    };
    engine.call('C2VM-TLE-OptionChanged', "ExclusivePedestrian_1").then(engineOptionCallback);

    const optionListener = (event) => {
        resetOptionCheckboxes();
        const dataset = event.currentTarget.dataset;
        console.log(event.currentTarget, dataset);
        let value = 0;
        if (dataset.value == 0) {
            value = 1;
        }
        engine.call('C2VM-TLE-OptionChanged', `${dataset.key}_${value}`).then(engineOptionCallback);
    };
    
    const optionItems = document.querySelectorAll(`[data-type="c2vm-tle-panel-option"]`);
    for (const item of optionItems) {
        item.onclick = optionListener;
    }

    const body = document.querySelector("body");
    const config = { attributes: true, childList: true, subtree: true };
    const callback = (mutationList, observer) => {
        const img = document.querySelector("button.selected.item_KJ3.item-hover_WK8.item-active_Spn > img");

        const panel = document.querySelector("div.c2vm-tle-panel");
        
        if (panel) {
            panel.style.display = (img && img.src == "Media/Game/Icons/TrafficLights.svg") ? "block" : "none";
        }
    };
    const observer = new MutationObserver(callback);
    observer.observe(body, config);
}