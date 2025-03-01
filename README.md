<p align="center"> <img alt="Impstation" src="https://github.com/impstation/imp-station-14/blob/master/Resources/Textures/Logo/logo.png" /></p>

Impstation is a fork of Space Station 14, a remake of SS13 that runs on [Robust Toolbox](https://github.com/space-wizards/RobustToolbox), a homegrown engine written in C#.

While we are not a server that allows sexual content, <b>we do not allow people under the age of 20 to play on Impstation.</b>

## Links

[Website](https://impstation.gay/) | [Steam](https://store.steampowered.com/app/1255460/Space_Station_14/) | [Standalone Download](https://spacestation14.io/about/nightlies/)

## Documentation/Wiki

SS14's [docs site](https://docs.spacestation14.io/) has documentation on content, engine, game design and more. We also have lots of resources for new contributors to the project.

## Contributing

We are happy to accept contributions from anybody. Get in Discord if you want to help. We've got a [list of issues](https://github.com/impstation/imp-station-14/issues) that need to be done and anybody can pick them up. Don't be afraid to ask for help either!

As a baseline make sure your changes and pull requests are in accordance with the upstream [contribution guidelines](https://docs.spacestation14.com/en/general-development/codebase-info/pull-request-guidelines.html). We're generally not as strict, but it's good practice to follow these examples.

If you are adding completely custom content that would go into the normal SS14 file structure in a certain spot, add that content to the `_Impstation` folder with that same file path instead. For example, when adding the GBsay chat command for ghost bar stuff, it would have gone in `Content.Server/Chat/Commands`. Instead, the `GBsay.cs` file is in `Content.Server/_Impstation/Chat/Commands`. 

The Impstation folders are located at `Content.Client/_Impstation`, `Content.Server/_Impstation`, and `Content.Shared/_Impstation`. The Resources folder is kind of its own beast, and has a lot of depth. For that reason it makes sense to have the _Impstation folder inside of the subfolder it is modifying. As another example, the main prototypes folder for our  custom content is located in `Resources/Prototypes/_Impstation`. The custom Spy vs Spy gamemode is defined in `Resources/Prototypes/_Impstation/GameRules/roundstart.yml`. This applies for recipies, clothing, everything. 

Keeping things defined like this makes the lives of the people maintaining the server much, much easier.


## Building

1. Clone this repo.
2. Run `RUN_THIS.py` to init submodules and download the engine.
3. Compile the solution.

[More detailed instructions on building the project.](https://docs.spacestation14.com/en/general-development/setup.html)

## License

Content contributed to this repository after and including commit 7210960b2b30e17aa001f4e35a5d0f80ca548e53 (`15 August 2024 17:02:49 UTC`) is licensed under the GNU Affero General Public License version 3.0 unless otherwise stated. See [LICENSE-AGPLv3](./LICENSE-AGPLv3.TXT).

Content contributed to this repository before commit 7210960b2b30e17aa001f4e35a5d0f80ca548e53 (`15 August 2024 17:02:49 UTC`) is licensed under the MIT license unless otherwise stated. See [LICENSE-MIT](./LICENSE-MIT.TXT).

To be more specific, code in Content./Impstation, Resources//Impstation and any Impstation specific scripts in Tools are licensed under AGPLv3. Other files are originally from other codebases and are not owned by Impstation, though any code must be relicensable to AGPLv3. SS14 is MIT licensed so this forking is possible.

Most assets are licensed under [CC-BY-SA 3.0](https://creativecommons.org/licenses/by-sa/3.0/) unless stated otherwise. Assets have their license and the copyright in the metadata file. [Example](https://github.com/space-wizards/space-station-14/blob/master/Resources/Textures/Objects/Tools/crowbar.rsi/meta.json).

Note that some assets are licensed under the non-commercial [CC-BY-NC-SA 3.0](https://creativecommons.org/licenses/by-nc-sa/3.0/) or similar non-commercial licenses and will need to be removed if you wish to use this project commercially.

## Attributions

When we pull content from other forks, we try to organize their content to their own subfolders in each of the projects to keep track of attribution and try to prevent merge conflicts.

Content under these subdirectories either originate from their respective fork, or are modifications related to content from their respective fork.

| Subdirectory | Fork Name | Fork Repository | License |
|--------------|-----------|-----------------|---------|
| `_Impstation` | Impstation | https://github.com/impstation/imp-station-14/ | AGPL 3.0 |
| `_CD` | Cosmatic Drift | https://github.com/cosmatic-drift-14/cosmatic-drift | MIT |
| `_DV` | Delta-V | https://github.com/DeltaV-Station/Delta-v/ | AGPL 3.0 |
| `_EinsteinEngine` | Einstein Engines | https://github.com/Simple-Station/Einstein-Engines/ | AGPL 3.0 |
| `_EstacaoPirata` | Estacao Pirata | https://github.com/Day-OS/estacao-pirata-14/ | AGPL 3.0 |
| `_Goobstation` | Goob Station | https://github.com/Goob-Station/Goob-Station/ | AGPL 3.0 |
| `_Harmony` | Harmony | https://github.com/ss14-harmony/ss14-harmony | AGPL 3.0 |
| `_NF` | Frontier Station | https://github.com/new-frontiers-14/frontier-station-14 | AGPL 3.0 |
| `_White` | White Dream | https://github.com/WWhiteDreamProject/wwdpublic/ | AGPL 3.0 |
