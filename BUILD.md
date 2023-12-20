## Build Instructions

1. Install [Node.js](https://nodejs.org/) and [.NET SDK](https://dotnet.microsoft.com/download)

2. Clone the repository

```shell
git clone git@github.com:slyh/Cities2-TrafficLightsEnhancement.git
cd Cities2-TrafficLightsEnhancement
git submodule update --init --recursive
```

3. Copy the `Cities Skylines II/Cities2_Data/Managed/` folder to `Cities2-TrafficLightsEnhancement/`

4. Build the plugin

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