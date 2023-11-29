if (!document.querySelector("div.c2vm-tle-panel")) {
    const panelDiv = document.createElement("div");
    panelDiv.innerHTML = `
        <div class="c2vm-tle-panel">
            <div class="c2vm-tle-panel-header">
                <img class="c2vm-tle-panel-header-image" src="Media/Game/Icons/TrafficLights.svg" />
                <div class="c2vm-tle-panel-header-title">Traffic Lights Enhancement</div>
            </div>
            <div class="c2vm-tle-panel-content"></div>
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
            .c2vm-tle-panel-button {
                padding: 3rem;
                border-radius: 3rem;
                color: white;
                background-color: rgba(6, 10, 16, 0.7);
                width: 100%;
            }
            @keyframes notification-warning {
                to {
                  background-color: rgba(200, 0, 0, 0.5);
                }
              }
            .c2vm-tle-panel-row[data-notification-type="warning"] {
                animation-timing-function: linear;
                animation-duration: 2s;
                animation-iteration-count: infinite;
                animation-direction: alternate;
                animation-name: notification-warning;
                border-radius: 3rem;
                padding: 8rem;
            }
            .c2vm-tle-panel-notification-image {
                width: 20rem;
                height: 20rem;
                margin-right: 10rem;
            }
            .c2vm-tle-panel-notification-text {
                color: rgba(217, 217, 217, 1);
                flex: 1;
            }

            .c2vm-tle-lane-panel {
                width: 200rem;
                display: none;
            }
            .c2vm-tle-lane-panel-row {
                padding: 3rem 8rem;
                width: 100%;
                display: flex;
            }
            .c2vm-tle-lane-button {
                padding: 3rem;
                border-radius: 3rem;
                background-color: rgba(6, 10, 16, 0.7);
            }
            .c2vm-tle-lane-button > img {
                width: 28rem;
                height: 28rem;
            }
            .c2vm-tle-lane-panel-checkbox {
                margin: 0 10rem 0 0;
                width: 20rem;
                height: 20rem;
                padding: 1px;
                border: 2px solid rgba(255, 255, 255, 0.500000);
                border-radius: 3rem;
            }
            .c2vm-tle-lane-panel-checkbox-checkmark {
                width: 100%;
                height: 100%;
                mask-image: url(Media/Glyphs/Checkmark.svg);
                mask-size: 100% auto;
                background-color: white;
                opacity: 0;
            }
            .c2vm-tle-lane-panel-checkbox-checkmark-checked {
                opacity: 1;
            }
            .c2vm-tle-lane-panel-button {
                padding: 3rem;
                border-radius: 3rem;
                color: white;
                background-color: rgba(6, 10, 16, 0.7);
                width: 100%;
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

    const enginePatternCallback = (jsonString) => {
        resetPatternButtons();
        const result = JSON.parse(jsonString);
        const ways = result.ways;
        const pattern = result.pattern & 0xFFFF;
        const button = document.querySelector(`[data-type="c2vm-tle-panel-pattern"][data-ways="${ways}"][data-pattern="${pattern}"] .c2vm-tle-panel-radio-bullet`);
        if (button) {
            button.classList.add("c2vm-tle-panel-radio-bullet-checked");
        }
    };

    const patternListener = (event) => {
        resetPatternButtons();
        const dataset = event.currentTarget.dataset;
        // console.log(event.currentTarget, dataset, dataset.ways, dataset.pattern);
        engine.call('C2VM-TLE-PatternChanged', `${dataset.ways}_${dataset.pattern}`).then(enginePatternCallback);
    };

    // Options
    const resetOptionCheckboxes = () => {
        const rows = document.querySelectorAll(`.c2vm-tle-panel [data-type="c2vm-tle-panel-option"]`);
        for (const row of rows) {
            row.dataset.value = 0;
            const checkbox = row.querySelector(`.c2vm-tle-panel-checkbox-checkmark`);
            if (checkbox) {
                checkbox.classList.remove("c2vm-tle-panel-checkbox-checkmark-checked");
            }
        }
    };

    const engineOptionCallback = (result) => {
        resetOptionCheckboxes();
        const options = JSON.parse(result);
        // console.log(`C2VM-TLE-OptionChanged result ${result} m_Options ${options}`);
        for (const key in options) {
            const value = options[key];
            const row = document.querySelector(`[data-type="c2vm-tle-panel-option"][data-key="${key}"]`);
            if (!row) {
                continue;
            }
            row.dataset.value = value;
            const checkbox = row.querySelector(`.c2vm-tle-panel-checkbox-checkmark`);
            if (checkbox && value === 1) {
                checkbox.classList.add("c2vm-tle-panel-checkbox-checkmark-checked");
            }
        }
    };

    const optionListener = (event) => {
        const dataset = event.currentTarget.dataset;
        // console.log(event.currentTarget, dataset);
        let value = 0;
        if (dataset.value == 0) {
            value = 1;
        }
        resetOptionCheckboxes();
        engine.call('C2VM-TLE-OptionChanged', `${dataset.key}_${value}`).then(engineOptionCallback);
    };

    const buttonListener = (event) => {
        const dataset = event.currentTarget.dataset;
        if (dataset.engineEventName == "C2VM-TLE-RequestMenuSave") {
            closeTLEPanel();
        } else {
            engine.call(dataset.engineEventName, `${dataset.key}_${dataset.value}`);
        }
    };

    const closeTLEPanel = () => {
        engine.call("C2VM-TLE-RequestMenuSave", `save_1`);
    };

    const closeLaneManagement = () => {
        const body = document.querySelector("body");
        for (const child of body.children) {
            if (child.classList.contains("c2vm-tle-lane-button")) {
                body.removeChild(child);
            }
        }
    };

    const menuUpdateCallback = (result) => {
        const menu = JSON.parse(result);
        const content = document.querySelector(".c2vm-tle-panel-content");

        while (content.firstChild) {
            content.removeChild(content.lastChild);
        }

        let removeLaneManagementButtons = true;

        // console.log(menu, content);
        for (const item of menu) {
            if (!item.itemType) {
                continue;
            }
            if (item.itemType == "divider") {
                const row = document.createElement("div");
                row.classList.add("c2vm-tle-panel-row-divider");
                content.appendChild(row);
            }
            if (item.itemType == "title") {
                const row = document.createElement("div");
                row.classList.add("c2vm-tle-panel-row");
                row.innerHTML = item.title;
                content.appendChild(row);
            }
            if (item.itemType == "radio") {
                const row = document.createElement("div");
                row.classList.add("c2vm-tle-panel-row");
                for (const key in item) {
                    row.dataset[key] = item[key];
                }
                row.innerHTML += `
                    <div class="c2vm-tle-panel-radio">
                        <div class="c2vm-tle-panel-radio-bullet"></div>
                    </div>
                    <span class="c2vm-tle-panel-secondary-text">${item.label}</span>
                `;
                content.appendChild(row);
            }
            if (item.itemType == "checkbox") {
                const row = document.createElement("div");
                row.classList.add("c2vm-tle-panel-row");
                for (const key in item) {
                    row.dataset[key] = item[key];
                }
                row.innerHTML += `
                    <div class="c2vm-tle-panel-checkbox">
                        <div class="c2vm-tle-panel-checkbox-checkmark"></div>
                    </div>
                    <span class="c2vm-tle-panel-secondary-text">${item.label}</span>
                `;
                content.appendChild(row);
            }
            if (item.itemType == "button") {
                const row = document.createElement("div");
                row.classList.add("c2vm-tle-panel-row");
                for (const key in item) {
                    row.dataset[key] = item[key];
                }
                row.innerHTML += `
                    <button class="c2vm-tle-panel-button">${item.label}</button>
                `;
                content.appendChild(row);
                if (item.engineEventName == "C2VM-TLE-ToggleLaneManagement" && item.value == 1) {
                    removeLaneManagementButtons = false;
                }
            }
            if (item.itemType == "notification") {
                const row = document.createElement("div");
                row.classList.add("c2vm-tle-panel-row");
                for (const key in item) {
                    row.dataset[key] = item[key];
                }
                const img = document.createElement("img");
                img.src = "Media/Game/Icons/AdvisorNotifications.svg";
                img.classList.add("c2vm-tle-panel-notification-image");
                const labelDiv = document.createElement("div");
                labelDiv.classList.add("c2vm-tle-panel-notification-text");
                labelDiv.innerHTML = item.label;
                row.appendChild(img);
                row.appendChild(labelDiv);
                content.appendChild(row);
            }
        }

        if (removeLaneManagementButtons) {
            closeLaneManagement();
        }

        const optionItems = document.querySelectorAll(`[data-type="c2vm-tle-panel-option"]`);
        for (const item of optionItems) {
            item.onclick = optionListener;
        }

        const patternItems = document.querySelectorAll(`[data-type="c2vm-tle-panel-pattern"]`);
        for (const item of patternItems) {
            if (item.dataset.ways && item.dataset.ways > 0) {
                item.onclick = patternListener;
            }
        }

        const buttonItems = document.querySelectorAll(`[data-type="c2vm-tle-panel-button"]`);
        for (const item of buttonItems) {
            item.onclick = buttonListener;
        }

        engine.call("C2VM-TLE-PatternChanged", "").then(enginePatternCallback);
        engine.call("C2VM-TLE-OptionChanged", "").then(engineOptionCallback);
        // engine.call('C2VM-TLE-PatternChanged', "3_0").then(enginePatternCallback);
        // engine.call('C2VM-TLE-PatternChanged', "4_0").then(enginePatternCallback);
        // engine.call('C2VM-TLE-OptionChanged', "ExclusivePedestrian_1").then(engineOptionCallback);
        // engine.call('C2VM-TLE-OptionChanged', "AlwaysGreenKerbsideTurn_0").then(engineOptionCallback);
    };

    engine.call("C2VM-TLE-RequestMenuData").then(menuUpdateCallback);

    engine.on("C2VM-TLE-Event-UpdateMenu", menuUpdateCallback);

    const lanePanelCheckboxListener = (event) => {
        const dataset = event.currentTarget.dataset;
        // console.log(event.currentTarget, dataset);
        const checkbox = event.currentTarget.querySelector(`.c2vm-tle-lane-panel-checkbox-checkmark`);
        if (checkbox) {
            checkbox.classList.remove("c2vm-tle-lane-panel-checkbox-checkmark-checked");
        }
        if (dataset.value == "False") {
            dataset.value = "True";
            if (checkbox) {
                checkbox.classList.add("c2vm-tle-lane-panel-checkbox-checkmark-checked");
            }
        } else {
            dataset.value = "False";
        }
    };

    const lanePanelButtonListener = (event) => {
        const dataset = event.currentTarget.dataset;
        // console.log(event.currentTarget, dataset);
        if (dataset.engineEventName == "C2VM-TLE-CustomLaneDirectionChanged") {
            let m_BanLeft = true;
            let m_BanRight = true;
            let m_BanStraight = true;
            let m_BanUTurn = true;
            const position = event.currentTarget.parentElement.parentElement.dataset;
            const checkboxes = event.currentTarget.parentElement.querySelectorAll(`[data-item-type="checkbox"]`);
            // console.log(position, checkboxes);
            for (const checkbox of checkboxes) {
                // console.log(checkbox, checkbox.dataset);
                if (checkbox.dataset.key == "m_BanLeft" && checkbox.dataset.value == "True") {
                    m_BanLeft = false;
                }
                if (checkbox.dataset.key == "m_BanRight" && checkbox.dataset.value == "True") {
                    m_BanRight = false;
                }
                if (checkbox.dataset.key == "m_BanStraight" && checkbox.dataset.value == "True") {
                    m_BanStraight = false;
                }
                if (checkbox.dataset.key == "m_BanUTurn" && checkbox.dataset.value == "True") {
                    m_BanUTurn = false;
                }
            }
            const connection = JSON.stringify({
                m_Type: 0,
                m_Position: {
                    x: position.worldX,
                    y: position.worldY,
                    z: position.worldZ
                },
                m_Tangent: {
                    x: position.tangentX,
                    y: position.tangentY,
                    z: position.tangentZ
                },
                m_GroupIndex: position.groupIndex,
                m_LaneIndex: position.laneIndex,
                m_Restriction: {
                    m_BanLeft,
                    m_BanRight,
                    m_BanStraight,
                    m_BanUTurn
                }
            })
            engine.call(dataset.engineEventName, connection).then(() => {});
            const panel = event.currentTarget.parentElement;
            panel.style.display = "none";
            while (panel.firstChild) {
                panel.removeChild(panel.lastChild);
            }
            const buttons = document.querySelectorAll(".c2vm-tle-lane-button");
            for (const button of buttons) {
                button.style.display = "block";
            }
            event.stopPropagation();
        }
    };

    const laneButtonListener = (event) => {
        const dataset = event.currentTarget.dataset;
        if (event.currentTarget.querySelector(".c2vm-tle-lane-panel") && event.currentTarget.querySelector(".c2vm-tle-lane-panel").children.length == 0) {
            engine.call(
                "C2VM-TLE-RequestLaneManagementData",
                JSON.stringify({
                    m_Position: {
                        x: dataset.worldX,
                        y: dataset.worldY,
                        z: dataset.worldZ
                    },
                    m_Tangent: {
                        x: dataset.tangentX,
                        y: dataset.tangentY,
                        z: dataset.tangentZ
                    },
                    m_GroupIndex: dataset.groupIndex,
                    m_LaneIndex: dataset.laneIndex,
                    m_Restriction: {
                        m_BanLeft: true,
                        m_BanRight: true,
                        m_BanStraight: true,
                        m_BanUTurn: true
                    }
                })
            ).then(engineLanePanelCallback);
        }
        const buttons = document.querySelectorAll(".c2vm-tle-lane-button");
        for (const button of buttons) {
            if (button !== event.currentTarget) {
                button.style.display = "none";
            }
        }
    };

    const engineLanePanelCallback = (jsonString) => {
        try {
            // console.log(jsonString);
            const menu = JSON.parse(jsonString);
            // console.log(menu);
            const pos = menu[0];
            const container = document.querySelector(`[data-world-x="${pos.x}"][data-world-y="${pos.y}"][data-world-z="${pos.z}"]`);
            const panel = container.querySelector(`.c2vm-tle-lane-panel`);

            if (!panel) {
                console.log(`Panel not found.`, pos, panel);
                return;
            }
            panel.style.display = "block";

            while (panel.firstChild) {
                panel.removeChild(panel.lastChild);
            }

            const header = document.createElement("div");
            header.classList.add("c2vm-tle-lane-panel-row");
            header.style.color = "white";
            header.innerHTML = `Lane Direction`;
            panel.appendChild(header);

            for (const item of menu) {
                if (!item.itemType) {
                    continue;
                }
                if (item.itemType == "checkbox") {
                    const row = document.createElement("div");
                    row.classList.add("c2vm-tle-lane-panel-row");
                    for (const key in item) {
                        row.dataset[key] = item[key];
                    }
                    if (item.value == "True") {
                        row.dataset.value = "False";
                    } else {
                        row.dataset.value = "True";
                    }
                    row.innerHTML += `
                        <div class="c2vm-tle-lane-panel-checkbox">
                            <div class="c2vm-tle-lane-panel-checkbox-checkmark ${row.dataset.value == "True" ? "c2vm-tle-lane-panel-checkbox-checkmark-checked" : ""}"></div>
                        </div>
                        <span class="c2vm-tle-panel-secondary-text">${item.label}</span>
                    `;
                    row.onclick = lanePanelCheckboxListener;
                    panel.appendChild(row);
                }
                if (item.itemType == "button") {
                    const row = document.createElement("div");
                    row.classList.add("c2vm-tle-lane-panel-row");
                    for (const key in item) {
                        row.dataset[key] = item[key];
                    }
                    row.innerHTML += `
                        <button class="c2vm-tle-lane-panel-button">${item.label}</button>
                    `;
                    row.onclick = lanePanelButtonListener;
                    panel.appendChild(row);
                }
            }
        } catch (e) {
            console.log(e);
        }
    };

    engine.on("C2VM-TLE-Event-ConnectPosition", function (jsonString) {
        const result = JSON.parse(jsonString);
        // console.log(jsonString, result);
        for (const position of result.source) {
            const laneButton = document.createElement("div");
            laneButton.classList.add("c2vm-tle-lane-button");
            laneButton.innerHTML = `
                <img src="Media/Game/Icons/RoadsServices.svg">
                <div class="c2vm-tle-lane-panel"></div>
            `;
            laneButton.dataset.worldX = position.world.x;
            laneButton.dataset.worldY = position.world.y;
            laneButton.dataset.worldZ = position.world.z;
            laneButton.dataset.tangentX = position.world.tangentX;
            laneButton.dataset.tangentY = position.world.tangentY;
            laneButton.dataset.tangentZ = position.world.tangentZ;
            laneButton.dataset.groupIndex = position.world.groupIndex;
            laneButton.dataset.laneIndex = position.world.laneIndex;
            laneButton.style.position = "absolute";
            laneButton.onclick = laneButtonListener;
            document.querySelector("body").appendChild(laneButton);
        }
    });

    const updateLaneButtonPosition = () => {
        const worldList = [...document.querySelectorAll("div.c2vm-tle-lane-button")].map(e => ({x: e.dataset.worldX, y: e.dataset.worldY, z: e.dataset.worldZ}));
        if (worldList.length == 0) {
            return;
        }
        engine.call(
            "C2VM-TLE-RequestWorldToScreen",
            JSON.stringify(worldList)
        ).then((jsonString) => {
            try {
                // console.log(jsonString);
                const result = JSON.parse(jsonString);
                // console.log(result);
                for (const position of result) {
                    const tooltipDiv = document.querySelector(`div.c2vm-tle-lane-button[data-world-x="${position.world.x}"][data-world-y="${position.world.y}"][data-world-z="${position.world.z}"]`);
                    if (tooltipDiv) {
                        tooltipDiv.style.left = (position.screen.x - 17) + "px";
                        tooltipDiv.style.bottom = (position.screen.y - 17) + "px";
                    }
                }
            } catch (e) {
                console.log(e);
            }
        });
    };

    setInterval(updateLaneButtonPosition, 100);

    engine.call("C2VM-TLE-RequestMenuData").then(menuUpdateCallback);

    const body = document.querySelector("body");
    const config = { attributes: true, childList: true, subtree: true };
    const callback = (mutationList, observer) => {
        const img = document.querySelector("button.selected.item_KJ3.item-hover_WK8.item-active_Spn > img");
        const panel = document.querySelector("div.c2vm-tle-panel");
        if (panel) {
            if (img && img.src == "Media/Game/Icons/TrafficLights.svg") {
                panel.style.display = "block";
            } else if (panel.style.display != "none") {
                panel.style.display = "none";
                closeLaneManagement();
                closeTLEPanel();
            }
        }
    };
    const observer = new MutationObserver(callback);
    observer.observe(body, config);
}