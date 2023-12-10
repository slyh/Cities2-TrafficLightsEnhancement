## Build Instructions

1. Install [Node.js](https://nodejs.org/) and [.NET SDK](https://dotnet.microsoft.com/download)

2. Skip to step 5 if you don't need to rebuild the frontend

3. Clone the frontend repository

```shell
git clone https://github.com/C2VM/tle-frontend.git
```

4. Build the frontend

```shell
cd tle-frontend
npm install
npm run build
```

5. Clone the plugin repository

```shell
git clone https://github.com/slyh/Cities2-Various-Mods.git
cd Cities2-Various-Mods
git submodule update --init --recursive
```

6. Copy `tle-frontend/dist/assets/Payload.cs` to `Cities2-Various-Mods/TrafficLightsEnhancement/Systems/UISystem/`

7. Build the plugin

* For BepInEx 5

```shell
dotnet restore
dotnet build --configuration Release --property:BepInExVersion=5
```

* For BepInEx 6

```shell
dotnet restore
dotnet build --configuration Release --property:BepInExVersion=6
```