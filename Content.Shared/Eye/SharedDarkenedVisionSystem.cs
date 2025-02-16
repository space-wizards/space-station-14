using Content.Shared.Clothing;
using Content.Shared.Clothing.Components;
using Content.Shared.Eye.Blinding.Components;
using Content.Shared.Eye.Blinding.Systems;
using Content.Shared.Inventory;
using Content.Shared.Tag;

namespace Content.Shared.Eye;

public abstract class SharedDarkenedVisionSystem : EntitySystem
{
    [Dependency] private readonly InventorySystem _inventory = default!;
    [Dependency] private readonly BlindableSystem _blinding = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<DarkenedVisionComponent, CanSeeAttemptEvent>(OnTrySee);
    }

    public void UpdateVisionDarkening(Entity<DarkenedVisionComponent?> ent)
    {
        if (!Resolve(ent, ref ent.Comp, false))
            return;

        var ev = new GetVisionDarkeningEvent();
        RaiseLocalEvent(ent, ev);
        if (TryComp<InventoryComponent>(ent, out var inventory))
            _inventory.RelayEvent((ent.Owner, inventory), ref ev);

        ent.Comp.Strength = ev.Strength;

        Dirty(ent);
        _blinding.UpdateIsBlind(ent.Owner);
    }

    private void OnTrySee(Entity<DarkenedVisionComponent> ent, ref CanSeeAttemptEvent args)
    {
        if (ent.Comp.Strength >= ent.Comp.BlindTreshold) args.Cancel();
    }
}

/// <summary>
/// Event to get total vision darkening
/// </summary>
public sealed class GetVisionDarkeningEvent : EntityEventArgs, IInventoryRelayEvent
{
    public SlotFlags TargetSlots => SlotFlags.EYES | SlotFlags.MASK | SlotFlags.HEAD;
    public float Strength = 0f;
}
