// Мёртвый Космос, Licensed under custom terms with restrictions on public hosting and commercial use, full text: https://raw.githubusercontent.com/dead-space-server/space-station-14-fobos/master/LICENSE.TXT

using Content.Shared.Hands;
using Content.Shared.Inventory;
using Content.Shared.Inventory.Events;
using Content.Shared.Movement.Systems;

namespace Content.Shared.DeadSpace.TheCircle.CursedVant;

public sealed class CursedVantSystem : EntitySystem
{
    [Dependency] private readonly MovementSpeedModifierSystem _movement = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<CursedVantComponent, GotEquippedHandEvent>(OnEquippedHand);
        SubscribeLocalEvent<CursedVantComponent, GotUnequippedHandEvent>(OnUnequippedHand);
        SubscribeLocalEvent<CursedVantComponent, GotEquippedEvent>(OnEquipped);
        SubscribeLocalEvent<CursedVantComponent, GotUnequippedEvent>(OnUnequipped);
        SubscribeLocalEvent<CursedVantComponent, HeldRelayedEvent<RefreshMovementSpeedModifiersEvent>>(OnHeldRefresh);
        SubscribeLocalEvent<CursedVantComponent, InventoryRelayedEvent<RefreshMovementSpeedModifiersEvent>>(OnEquippedRefresh);
    }

    private void OnEquippedHand(Entity<CursedVantComponent> ent, ref GotEquippedHandEvent args)
    {
        _movement.RefreshMovementSpeedModifiers(args.User);
    }

    private void OnUnequippedHand(Entity<CursedVantComponent> ent, ref GotUnequippedHandEvent args)
    {
        _movement.RefreshMovementSpeedModifiers(args.User);
    }

    private void OnEquipped(Entity<CursedVantComponent> ent, ref GotEquippedEvent args)
    {
        _movement.RefreshMovementSpeedModifiers(args.Equipee);
    }

    private void OnUnequipped(Entity<CursedVantComponent> ent, ref GotUnequippedEvent args)
    {
        _movement.RefreshMovementSpeedModifiers(args.Equipee);
    }

    private void OnHeldRefresh(
        Entity<CursedVantComponent> ent,
        ref HeldRelayedEvent<RefreshMovementSpeedModifiersEvent> args)
    {
        var holder = Transform(ent).ParentUid;
        if (HasComp<CircleDeaconComponent>(holder))
            return;

        args.Args.ModifySpeed(ent.Comp.SpeedModifier);
    }

    private void OnEquippedRefresh(
        Entity<CursedVantComponent> ent,
        ref InventoryRelayedEvent<RefreshMovementSpeedModifiersEvent> args)
    {
        if (HasComp<CircleDeaconComponent>(args.Owner))
            return;

        args.Args.ModifySpeed(ent.Comp.SpeedModifier);
    }
}
