using Content.Shared.Atmos.Components;
using Content.Shared.Examine;
using Content.Shared.Temperature;

namespace Content.Shared.Atmos.EntitySystems;

public abstract class SharedGasMinerSystem : EntitySystem
{
    [Dependency] private readonly SharedAtmosphereSystem _sharedAtmosphereSystem = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<GasMinerComponent, ExaminedEvent>(OnExamine);
    }

    private void OnExamine(Entity<GasMinerComponent> ent, ref ExaminedEvent args)
    {
        var component = ent.Comp;

        using (args.PushGroup(nameof(GasMinerComponent)))
        {
            args.PushMarkup(Loc.GetString("gas-miner-mines-text",
                ("gas", Loc.GetString(_sharedAtmosphereSystem.GetGas(component.SpawnGas).Name))));

            args.PushText(Loc.GetString("gas-miner-amount-text",
                ("moles", $"{component.SpawnAmount:0.#}")));

            args.PushText(Loc.GetString("gas-miner-temperature-text",
                ("tempK", $"{component.SpawnTemperature:0.#}"),
                ("tempC", $"{TemperatureHelpers.KelvinToCelsius(component.SpawnTemperature):0.#}")));

            if (component.MaxExternalAmount < float.PositiveInfinity)
            {
                args.PushText(Loc.GetString("gas-miner-moles-cutoff-text",
                    ("moles", $"{component.MaxExternalAmount:0.#}")));
            }

            if (component.MaxExternalPressure < float.PositiveInfinity)
            {
                args.PushText(Loc.GetString("gas-miner-pressure-cutoff-text",
                    ("pressure", $"{component.MaxExternalPressure:0.#}")));
            }

            args.AddMarkup(component.MinerState switch
            {
                GasMinerState.Disabled => Loc.GetString("gas-miner-state-disabled-text"),
                GasMinerState.Idle => Loc.GetString("gas-miner-state-idle-text"),
                GasMinerState.Working => Loc.GetString("gas-miner-state-working-text"),
                // C# pattern matching is not exhaustive for enums
                _ => throw new IndexOutOfRangeException(nameof(component.MinerState)),
            });
        }
    }
}
