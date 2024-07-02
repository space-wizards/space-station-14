using Content.Shared.Eye.Blinding.Components;
using Content.Shared.Inventory.Events;
using Content.Shared.Inventory;

namespace Content.Shared.Eye.Blinding.Systems;

public sealed class BlindfoldSystem : EntitySystem
{
    [Dependency] private readonly BlindableSystem _blindableSystem = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<BlindfoldComponent, GotEquippedEvent>(OnEquipped);
        SubscribeLocalEvent<BlindfoldComponent, GotUnequippedEvent>(OnUnequipped);
        SubscribeLocalEvent<BlindfoldComponent, InventoryRelayedEvent<CanSeeAttemptEvent>>(OnBlindfoldTrySee);
    }

    private void OnBlindfoldTrySee(Entity<BlindfoldComponent> blindfold, ref InventoryRelayedEvent<CanSeeAttemptEvent> args)
    {
        if (!blindfold.Comp.Override)
            args.Args.Cancel();
    }

    private void OnEquipped(Entity<BlindfoldComponent> blindfold, ref GotEquippedEvent args)
    {
        _blindableSystem.UpdateIsBlind(args.Equipee);
    }

    public void SetOverride(EntityUid uid, BlindfoldComponent blindfold, bool bit, EntityUid equipee)
    {
        Dirty(uid, blindfold);
        blindfold.Override = bit;
        if (equipee.IsValid())
        {
            _blindableSystem.UpdateIsBlind(equipee);
        }
    }

    private void OnUnequipped(Entity<BlindfoldComponent> blindfold, ref GotUnequippedEvent args)
    {
        _blindableSystem.UpdateIsBlind(args.Equipee);
    }
}
