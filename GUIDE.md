> [!NOTE]
> Currently, the mod only supports three-way and four-way junctions. Junctions of other types will offer fewer options in the mod.

> [!TIP]
> The mod only modifies the sequencing of signals, not their duration. The base game's *smart* traffic lights extend green lights if traffic is still flowing, signal timing is not altered by this mod at present.

## Introduction
This mod introduces advanced traffic light controls for Cities: Skylines II, allowing for more efficient traffic management. It supports both Left-Hand Traffic (LHT) and Right-Hand Traffic (RHT).

## Modes

| Mode | Description |
| --- | --- |
| Vanilla | Operates like the base game.<br>LHT: protected straight, protected left, and permissive right.<br>RHT: protected straight, protected right, and permissive left. |
| Split-Phasing | Only one road has a green light at a time. |
| Advanced Split Phasing | Similar to Split Phasing, with additional protected turns for the other road at the same time.[^1] |
| Protected Left/Right-Turns | LHT: Centre lanes perform a protected left turn first, followed by normal traffic flow including straight and right turns.<br>RHT: Centre lanes perform a protected right turn first, followed by normal traffic flow including straight and left turns.<br>[Video Illustration](https://www.youtube.com/watch?v=CIw0Au8qFQ8) |

## Options

| Option | Description |
| --- | --- |
| Exclusive Pedestrian Phase | A dedicated phase for pedestrian crossings, stopping all vehicular traffic. |
| Always Green Left/Right-Turns | Provides functionality similar to 'allow turn on red,' but vehicles will not give way. |

> [!WARNING]
> There may be pedestrian pathfinding issues at junctions, potentially indicating a bug in the game's node or pathfinding system, not addressed by this mod.

## How To Use

1. Open the Roads Tool, switch to the Road Services tab, and select "Traffic Lights"

![Screenshot 2023-12-10 102831](https://github.com/primeinc/Cities2-Various-Mods/assets/80482978/de6a9184-d340-4371-82c9-ef6731a69630)

2. A small window should appear in the top-left corner of your screen. Move your cursor to any existing junction and press the left mouse button

![Screenshot 2023-12-10 103024](https://github.com/primeinc/Cities2-Various-Mods/assets/80482978/c0beae47-9175-4a31-aad4-ea169f81e1e7)

3. Select the signal mode you prefer and save. The selected junction should now operate in your chosen mode

![Screenshot 2023-12-10 103213](https://github.com/primeinc/Cities2-Various-Mods/assets/80482978/ee258c53-0ab4-43a2-a9b8-2ed07a792c1a)

[^1]: This advanced split phasing handles traffic light groups dynamically, considering traffic direction and neighboring lane groups.
