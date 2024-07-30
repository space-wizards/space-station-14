using Content.Shared.Atmos.Components;
using Content.Shared.Examine;
using Content.Shared.Temperature;

namespace Content.Shared.Atmos.EntitySystems;

public abstract class SharedGasMinerSystem : EntitySystem
{
    [Dependency] private readonly ExamineSystemShared _examineSystemShared = default!;
    [Dependency] private readonly SharedAtmosphereSystem _sharedAtmosphereSystem = default!;

    // Gas miners are traditionally placed in a sealed room behind many layers of glass windows
    // and empty space. Under normal conditions a player is usually at least 5 tiles from
    // a gas miner. The standard examine range (3) is too small for the gas miner's examine text
    // to be really useful, so we have an extended range for examining gas miners.
    private const float MinerExamineRange = 6f;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<GasMinerComponent, ExaminedEvent>(OnExamine);
    }

    private void OnExamine(Entity<GasMinerComponent> ent, ref ExaminedEvent args)
    {
        var component = ent.Comp;

        if (component.SpawnGas == null)
            return;

        if (!_examineSystemShared.InRangeUnOccluded(args.Examiner, args.Examined, MinerExamineRange))
            return;

        using (args.PushGroup(nameof(GasMinerComponent)))
        {
            args.PushMarkup(Loc.GetString("gas-miner-mines-text",
                ("gas", Loc.GetString(_sharedAtmosphereSystem.GetGas(component.SpawnGas.Value).Name))));

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

            if (!component.Enabled)
            {
                args.AddMarkup(Loc.GetString("gas-miner-state-disabled-text"));
            }
            else if (component.Idle)
            {
                args.AddMarkup(Loc.GetString("gas-miner-state-idle-text"));
            }
            else
            {
                args.AddMarkup(Loc.GetString("gas-miner-state-working-text"));
            }
        }
    }
}
