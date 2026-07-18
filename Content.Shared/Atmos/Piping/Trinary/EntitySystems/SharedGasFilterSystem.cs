using Content.Shared.Atmos.EntitySystems;
using Content.Shared.Atmos.Piping.Trinary.Components;
using Content.Shared.Examine;

namespace Content.Shared.Atmos.Piping.Trinary.EntitySystems;

public abstract partial class SharedGasFilterSystem : EntitySystem
{
    [Dependency] private SharedAtmosphereSystem _atmosphereSystem = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<GasFilterComponent, ExaminedEvent>(OnExamined);
    }

    private void OnExamined(Entity<GasFilterComponent> ent, ref ExaminedEvent args)
    {
        if (Loc.TryGetString("gas-volume-pump-system-examined",
                out var transferRateStr,
                ("statusColor", "lightblue"),
                ("rate", ent.Comp.TransferRate.ToString("G"))
            ))
        {
            args.PushMarkup(transferRateStr);
        }

        var gasName = Loc.GetString("comp-gas-filter-ui-filter-gas-none");
        if (ent.Comp.FilteredGas.HasValue)
        {
            var gas = _atmosphereSystem.GetGas((Gas)ent.Comp.FilteredGas);
            gasName = Loc.GetString(gas.Name);
        }

        if (Loc.TryGetString("comp-gas-filter-filtered-gas-examine",
                out var filteredGasStr,
                ("statusColor", "lightblue"),
                ("filteredGas", gasName)
            ))
        {
            args.PushMarkup(filteredGasStr);
        }
    }
}
