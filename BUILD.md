## Build Instructions

1. Install [Node.js 20](https://nodejs.org/), [.NET 8.0 SDK](https://dotnet.microsoft.com/download) and Modding Toolchain in-game

2. Clone the repository

```shell
git clone git@github.com:slyh/Cities2-TrafficLightsEnhancement.git
cd Cities2-TrafficLightsEnhancement
git submodule update --init --recursive
```

3. Build the plugin

```shell
dotnet restore
dotnet build --configuration Release
```