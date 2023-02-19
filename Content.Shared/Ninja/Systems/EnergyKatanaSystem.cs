using Content.Shared.Inventory.Events;
using Content.Shared.Ninja.Components;

namespace Content.Shared.Ninja.Systems;

public sealed class EnergyKatanaSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<EnergyKatanaComponent, GotEquippedEvent>(OnEquipped);
    }

    private void OnEquipped(EntityUid uid, EnergyKatanaComponent comp, GotEquippedEvent args)
    {
        // check if already bound
        if (comp.Ninja != null)
            return;

        // check if ninja already has a katana bound
        var user = args.Equipee;
        if (!TryComp<SpaceNinjaComponent>(user, out var ninja) || ninja.Katana != null)
            return;

        // bind it
        comp.Ninja = user;
        ninja.Katana = uid;
    }
}
