using Content.Server.Atmos.Components;
using Content.Server.Atmos.EntitySystems;

namespace Content.Server._Impstation.UnadaptedToSpace;

public sealed class UnadaptedToSpaceSystem : EntitySystem
{
    public override void Initialize()
    {
        SubscribeLocalEvent<UnadaptedToSpaceComponent, ComponentInit>(OnUnadaptedInit);
    }

    private void OnUnadaptedInit(EntityUid uid, UnadaptedToSpaceComponent component, ComponentInit args)
    {
        if (TryComp<BarotraumaComponent>(uid, out var barotrauma))
        {
            barotrauma.Damage.DamageDict["Blunt"] = 2.5; // gives you 10 seconds of consciousness #realism
        }
    }
}
