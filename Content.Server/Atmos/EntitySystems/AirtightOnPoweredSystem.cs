using Content.Server.Atmos.Components;
using Content.Server.Power.EntitySystems;
using Content.Shared.Atmos;
using Content.Shared.Power;

namespace Content.Server.Atmos.EntitySystems;

public sealed partial class AirtightOnPoweredSystem : EntitySystem
{
    [Dependency] private AirtightSystem _airtightSystem = default!;

    [Dependency] private EntityQuery<AirtightComponent> _airtightQuery;

    [SubscribeLocalEvent]
    private void OnPowerChanged(Entity<AirtightOnPoweredComponent> ent, ref PowerChangedEvent args)
    {
        if (_airtightQuery.TryComp(ent, out var airtight))
            _airtightSystem.SetAirblocked((ent.Owner, airtight), args.Powered);
    }
}
