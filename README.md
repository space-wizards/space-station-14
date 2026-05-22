<div class="header" align="center">

# vgstation14

</div>

vgstation14 is a fork of [Space Station 14](https://github.com/space-wizards/space-station-14),
the C# / [RobustToolbox](https://github.com/space-wizards/RobustToolbox) remake
of Space Station 13. It is maintained by the /vg/station13 community as the
successor to [vgstation13](https://github.com/vgstation-coders/vgstation13) — its
BYOND-based SS13 codebase — and ports content forward from it.

During the soft-fork era the project tracks upstream Space Station 14 closely;
all divergence is deliberate and recorded. See [`docs/vg/`](docs/vg/) for the
design pillars and divergence policy.

## Contributing

Contributions are welcome. Before you start, read:

- [AGENTS.md](AGENTS.md) — the golden rule, build/test commands, PR conventions.
- [CONTRIBUTING.md](CONTRIBUTING.md) — contribution guidelines.
- [docs/vg/divergence-policy.md](docs/vg/divergence-policy.md) — how divergence
  is kept isolated so upstream merges stay cheap.

## Building

Requires the .NET 10 SDK.

1. Clone the repository:
   ```shell
   git clone https://github.com/vg14-developers/vgstation14.git
   ```
2. Initialise submodules and the engine:
   ```shell
   cd vgstation14
   python RUN_THIS.py
   ```
3. Build:
   ```shell
   dotnet build
   ```

For detailed setup, the upstream [SS14 setup guide](https://docs.spacestation14.com/en/general-development/setup.html)
applies — the codebase and engine are shared.

## License

Code in this repository is licensed under the [MIT license](LICENSE.TXT).

Most assets are licensed under [CC-BY-SA 3.0](https://creativecommons.org/licenses/by-sa/3.0/)
unless stated otherwise; some are under the non-commercial
[CC-BY-NC-SA 3.0](https://creativecommons.org/licenses/by-nc-sa/3.0/). Each
asset's license and copyright are recorded in its `meta.json`.

vgstation14 is derived from Space Station 14 by the Space Wizards Federation and
retains all upstream attribution.
