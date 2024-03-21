<p align="center"> <img alt="Space Station 14" width="880" height="300" src="https://raw.githubusercontent.com/space-wizards/asset-dump/de329a7898bb716b9d5ba9a0cd07f38e61f1ed05/github-logo.svg" /></p>

Space Station 14 is a remake of SS13 that runs on [Robust Toolbox](https://github.com/space-wizards/RobustToolbox), our homegrown engine written in C#.

This is the primary repo for Space Station 14. To prevent people forking RobustToolbox, a "content" pack is loaded by the client and server. This content pack contains everything needed to play the game on one specific server.

If you want to host or create content for SS14, this is the repo you need. It contains both RobustToolbox and the content pack for development of new content packs.

//

Space Station 14 PLUS - is a SS14 with some updates (TTS, new content, ...) written on RobustToolbox (maybe it's been optimised in future)

//

## SS14 Links

[Website](https://spacestation14.io/) | [Discord](https://discord.ss14.io/) | [Forum](https://forum.spacestation14.io/) | [Steam](https://store.steampowered.com/app/1255460/Space_Station_14/) | [Standalone Download](https://spacestation14.io/about/nightlies/)

## Documentation/Wiki

Our [docs site](https://docs.spacestation14.io/) has documentation on SS14s content, engine, game design and more. We also have lots of resources for new contributors to the project.

## Contributing

We are happy to accept contributions from anybody. Get in Discord if you want to help. We've got a [list of issues](https://github.com/space-wizards/space-station-14-content/issues) that need to be done and anybody can pick them up. Don't be afraid to ask for help either!
Just make sure your changes and pull requests are in accordance with the [contribution guidelines](https://docs.spacestation14.com/en/general-development/codebase-info/pull-request-guidelines.html).

We are not currently accepting translations of the game on our main repository. If you would like to translate the game into another language consider creating a fork or contributing to a fork.

## Building

### Unix

```bash
sudo apt update -y && sudo apt upgrade -y # Updating system
sudo apt install -y git python3 dotnet8 # Downloading the required tools
git clone https://github.com/Lines115/space-station-14-plus.git # Clone the repo
cd space-station-14-plus
python3 RUN_THIS.py # init submodules and download engine (RobustToolbox)
dotnet build # Compile the solution
```

### Windows

1. First, download the required tools: [python3](https://python.org/), [git](https://git-scm.com/) and [dotnet8 SDK](https://dotnet.microsoft.com/en-us/download/dotnet/8.0)

2. Secondary, run this in cmd, or powershell (if it's alredy opened, rerun it):

```DOS
git clone https://github.com/Lines115/space-station-14-plus.git :: Download repo
cd space-station-14-plus
py RUN_THIS.py :: init submodules and download engine (RobustToolbox)
dotnet build :: Compile (build) the solution
```

[More detailed instructions on building the project.](https://docs.spacestation14.com/en/general-development/setup.html)

## License

All code for the content repository is licensed under [MIT](https://github.com/space-wizards/space-station-14/blob/master/LICENSE.TXT).

Most assets are licensed under [CC-BY-SA 3.0](https://creativecommons.org/licenses/by-sa/3.0/) unless stated otherwise. Assets have their license and the copyright in the metadata file. [Example](https://github.com/space-wizards/space-station-14/blob/master/Resources/Textures/Objects/Tools/crowbar.rsi/meta.json).

Note that some assets are licensed under the non-commercial [CC-BY-NC-SA 3.0](https://creativecommons.org/licenses/by-nc-sa/3.0/) or similar non-commercial licenses and will need to be removed if you wish to use this project commercially.
