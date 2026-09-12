<div class="header" align="center">
<img alt="Space Station 14" width="880" height="300" src="https://raw.githubusercontent.com/space-wizards/asset-dump/de329a7898bb716b9d5ba9a0cd07f38e61f1ed05/github-logo.svg">
</div>

Astral Reach is a collection of game modes inspired by Space Station 13 and built on [Robust Toolbox](https://github.com/space-wizards/RobustToolbox), our homegrown engine written in C#.

Robust Toolbox separates the engine from server-specific game content through content packs loaded by the client and server. A content pack contains the code and assets necessary to play on a particular server.

This repository contains both Robust Toolbox and the Astral Reach content pack for development.

## Links

<div class="header" align="center">

[Website](#) | [Discord](#) | [Forum](#) | [Mastodon](#) | [Patreon](#) | [Steam](https://store.steampowered.com/app/1255460/Space_Station_14/) | [Standalone Download](https://spacestation14.com/about/nightlies/)

</div>

## Documentation/Wiki

Our [docs site](https://docs.spacestation14.com/) has documentation on content, engine, game design, and more.

Additionally, see these resources for license and attribution information:

* [Robust Generic Attribution](https://docs.spacestation14.com/en/specifications/robust-generic-attribution.html)
* [Robust Station Image](https://docs.spacestation14.com/en/specifications/robust-station-image.html)

We also have lots of resources for new contributors to the project.

## Contributing

We are happy to accept contributions from anybody. Get in Discord if you want to help. We've got a [list of issues](https://github.com/space-wizards/space-station-14-content/issues) that need to be done and anybody can pick them up. Don't be afraid to ask for help either!

Just make sure your changes and pull requests are in accordance with the [contribution guidelines](https://docs.spacestation14.com/en/general-development/codebase-info/pull-request-guidelines.html).

We welcome translations of the game. If you would like to translate the game into another language, please notify our [localization team](#) for how best to implement your work.

## AI-generated contributions

Our position and rationale for this policy can be read here: [Our Stance on Generative AI in Development](#).

In line with precedent set by many large and reputable open-source communities, this project permits responsible AI-assisted development, but does not accept low-effort or unreviewed AI-generated contributions. AI-generated artwork is not accepted.

AI-assisted tools may be used when developing code, documentation, or other non-art contributions. However, the human contributor remains the author and is fully responsible for everything they submit. Contributing means vouching for the quality, correctness, license compliance, and suitability of the contribution for inclusion in the project.

AI-generated output must be treated as a suggestion rather than a finished contribution. Contributors are expected to personally review, test, and understand everything they submit. You must be able to maintain, modify, debug, explain, and defend the technical decisions in your contribution without relying on AI to do so on your behalf.

Review is expected to remain a human-to-human process. Contributors must be able to respond to review comments, answer questions about their work, and make requested changes themselves. A contribution may be rejected or closed if its contributor cannot adequately explain or maintain the submitted work.

When generative AI has been used to create or substantively modify a contribution, that use should be disclosed in the pull request or other location where authorship is normally described. Routine assistive use, such as spelling, grammar, translation, or language clarification, does not require disclosure.

AI tools must not be listed as authors, co-authors, contributors, or commit signatories. AI assistance does not transfer authorship, responsibility, or accountability away from the human contributor. For the purposes of this project, the human submitting the work is always the Contributor.

### Artwork

AI-generated visual artwork is not accepted.

This includes sprites, textures, illustrations, icons, concept art, promotional artwork, and other visual assets generated in whole or in substantial part by generative AI systems. Generative AI may not be used as a substitute for an artist when producing visual assets intended for inclusion in the project.

Minor assistive tools that do not generate the underlying artwork may be considered separately. Contributors remain responsible for establishing the authorship, provenance, licensing, and attribution of every submitted asset.

## Building

1. Clone this repo:

```shell
git clone https://github.com/Astral-Reach/Astral-Reach.git
```

2. Go to the project folder and run `RUN_THIS.py` to initialize the submodules and load the engine:

```shell
cd space-station-14
python RUN_THIS.py
```

3. Compile the solution:

Build the server using `dotnet build`.

[More detailed instructions on building the project.](https://docs.spacestation14.com/en/general-development/setup.html)

## License

All code for the content repository is licensed under the [MIT license](https://github.com/space-wizards/space-station-14/blob/master/LICENSE.TXT).

Most assets are licensed under [CC-BY-SA 3.0](https://creativecommons.org/licenses/by-sa/3.0/) unless stated otherwise. Assets have their license and copyright specified in the metadata file. For example, see the [metadata for a crowbar](https://github.com/space-wizards/space-station-14/blob/master/Resources/Textures/Objects/Tools/crowbar.rsi/meta.json).

> [!NOTE]
> Some assets are licensed under the non-commercial [CC-BY-NC-SA 3.0](https://creativecommons.org/licenses/by-nc-sa/3.0/) or similar non-commercial licenses and will need to be removed if you wish to use this project commercially.
