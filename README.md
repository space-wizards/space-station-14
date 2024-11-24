<div class="header" align="center"> <img alt="Space Station 14" width="880" height="300" src="https://raw.githubusercontent.com/space-wizards/asset-dump/de329a7898bb716b9d5ba9a0cd07f38e61f1ed05/github-logo.svg" </p>

![GitHub License](https://img.shields.io/github/license/space-wizards/space-station-14?style=for-the-badge&logo=github)
![GitHub commit activity](https://img.shields.io/github/commit-activity/y/space-wizards/space-station-14?style=for-the-badge&logo=git&logoColor=white)
![GitHub Issues or Pull Requests](https://img.shields.io/github/issues/space-wizards/space-station-14?style=for-the-badge&logo=helpdesk&logoColor=white)

</div>

Space Station 14 is a remake of SS13 that runs on [Robust Toolbox](https://github.com/space-wizards/RobustToolbox), our homegrown engine written in C#.

This is the primary repo for Space Station 14. To prevent people forking RobustToolbox, a "content" pack is loaded by the client and server. This content pack contains everything needed to play the game on one specific server.

If you want to host or create content for SS14, this is the repo you need. It contains both RobustToolbox and the content pack for development of new content packs.

## Links
<div class="header" align="center">

[![Website](https://img.shields.io/badge/Website-grey?style=for-the-badge&logo=homepage&logoColor=white)](https://spacestation14.io/)
[![Discord](https://img.shields.io/discord/310555209753690112?style=for-the-badge&logo=Discord&logoColor=white&label=Discord)](https://discord.ss14.io/)
[![Forum](https://img.shields.io/badge/Forum-grey?style=for-the-badge&logo=formspree&logoColor=white)](https://forum.spacestation14.io/)
[![Steam](https://img.shields.io/badge/Steam-Playtest-g?style=for-the-badge&logo=steam&logoColor=white)](https://store.steampowered.com/app/1255460/Space_Station_14/)
[![Standalone Download](https://img.shields.io/badge/Standalone_Download-grey?style=for-the-badge&logo=googlecloudstorage&logoColor=white)](https://spacestation14.io/about/nightlies/)
[![Standalone Download](https://img.shields.io/badge/Space_Wizards_Development_Wiki-grey?style=for-the-badge&logo=gitbook&logoColor=white)](https://docs.spacestation14.com/)

</div>

## Documentation/Wiki

Our [docs site](https://docs.spacestation14.io/) has documentation on SS14s content, engine, game design and more. We also have lots of resources for new contributors to the project.

## Contributing

We are happy to accept contributions from anybody. Get in Discord if you want to help. We've got a [list of issues](https://github.com/space-wizards/space-station-14-content/issues) that need to be done and anybody can pick them up. Don't be afraid to ask for help either!  
Just make sure your changes and pull requests are in accordance with the [contribution guidelines](https://docs.spacestation14.com/en/general-development/codebase-info/pull-request-guidelines.html).

We are not currently accepting translations of the game on our main repository. If you would like to translate the game into another language consider creating a fork or contributing to a fork.

## Project activity
<!---
CHANGE THE API!!!! API can be obtained on the website repobeats.axiom.co | HERE, FOR EXAMPLE, THERE IS AN API FROM ANOTHER REPOSITORY
--->
![Project activity](https://repobeats.axiom.co/api/embed/4637fb51923408d570b8e555b3fde24eedb2bfea.svg 'Repobeats analytics image')

## Project contributors

List of people who contributed to the project:

[![Contributors](https://contrib.rocks/image?repo=space-wizards/space-station-14)](https://github.com/space-wizards/space-station-14/graphs/contributors)

## Building

**Clone** this repo:
```shell
# Cloning using HTTPS repository access
git clone https://github.com/{user}/space-station-14.git

# Cloning using SSH repository access
git clone git@github.com:{user}/space-station-14.git
```

> [!NOTE]
> Replace `{user}` to your GitHub username. The easiest way to find it is in the GitHub service at the very top of the repository page.

**Go** to the project folder and run `RUN_THIS.py` to initialize the sub-modules and load the engine:
```shell
cd space-station-14
python RUN_THIS.py
```
Compile the solution: 

Build the server and use `dotnet build` or `dotnet build --configuration Release` (if necessary).

[More detailed instructions on building the project.](https://docs.spacestation14.com/en/general-development/setup.html)

## License

<details>
<summary><a href="#"><img src="https://img.shields.io/badge/licence-MIT-green?style=for-the-badge" alt="MIT license"></a></summary>

>All code for the content repository is licensed under [MIT](https://github.com/space-wizards/space-station-14/blob/master/LICENSE.TXT).
</details>

<details>
<summary><a href="#"><img src="https://img.shields.io/badge/licence-CC_3.0_BY--SA-lightblue?style=for-the-badge" alt="Creative Commons 3.0 BY-SA"></a></summary>

>Most assets are licensed under [CC-BY-SA 3.0](https://creativecommons.org/licenses/by-sa/3.0/) unless stated otherwise. Assets have their license and the copyright in the metadata file. [Example](https://github.com/space-wizards/space-station-14/blob/master/Resources/Textures/Objects/Tools/crowbar.rsi/meta.json).
</details>

<details>
<summary><a href="#"><img src="https://img.shields.io/badge/licence-CC_3.0_BY--NC--SA-lightblue?style=for-the-badge" alt="Creative Commons 3.0 BY-SA"></a></summary>

>Note that some assets are licensed under the non-commercial [CC-BY-NC-SA 3.0](https://creativecommons.org/licenses/by-nc-sa/3.0/) or similar non-commercial licenses and will need to be removed if you wish to use this project commercially.
</details>
