using Content.Shared.Inventory.Events;
using Content.Shared.Ninja.Components;

namespace Content.Shared.Ninja.Systems;

/// <summary>
/// System for katana binding and dash events. Recalling is handled by the suit.
/// </summary>
public sealed class EnergyKatanaSystem : EntitySystem
{
    [Dependency] private readonly SharedSpaceNinjaSystem _ninja = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<EnergyKatanaComponent, GotEquippedEvent>(OnEquipped);
        SubscribeLocalEvent<EnergyKatanaComponent, AddDashActionEvent>(OnAddDashAction);
        SubscribeLocalEvent<EnergyKatanaComponent, DashAttemptEvent>(OnDashAttempt);
    }

    /// <summary>
    /// When equipped by a ninja, try to bind it.
    /// </summary>
    private void OnEquipped(EntityUid uid, EnergyKatanaComponent comp, GotEquippedEvent args)
    {
        // check if user isnt a ninja or already has a katana bound
        var user = args.Equipee;
        if (!TryComp<SpaceNinjaComponent>(user, out var ninja) || ninja.Katana != null)
            return;

        // bind it since its unbound
        _ninja.BindKatana(user, uid, ninja);
    }

    private void OnAddDashAction(EntityUid uid, EnergyKatanaComponent comp, AddDashActionEvent args)
    {
        if (!HasComp<SpaceNinjaComponent>(args.User))
            args.Cancel();
    }

    private void OnDashAttempt(EntityUid uid, EnergyKatanaComponent comp, DashAttemptEvent args)
    {
        if (!TryComp<SpaceNinjaComponent>(args.User, out var ninja) || ninja.Katana != uid)
            args.Cancel();
    }
}
