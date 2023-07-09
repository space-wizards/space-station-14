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

    private void OnBlindfoldTrySee(EntityUid uid, BlindfoldComponent component, InventoryRelayedEvent<CanSeeAttemptEvent> args)
    {
        args.Args.Cancel();
    }

    private void OnEquipped(EntityUid uid, BlindfoldComponent component, GotEquippedEvent args)
    {
        _blindableSystem.UpdateIsBlind(args.Equipee);
    }

    private void OnUnequipped(EntityUid uid, BlindfoldComponent component, GotUnequippedEvent args)
    {
        _blindableSystem.UpdateIsBlind(args.Equipee);
    }
}
