using Content.Shared.Inventory;

namespace Content.Shared.Contraband;

public sealed class ShowContrabandSystem : EntitySystem
{
    public override void Initialize()
    {
        SubscribeLocalEvent<ShowContrabandDetailsComponent, InventoryRelayedEvent<GetContrabandDetailsEvent>>(GetContrabandDetailsEventHandler);
    }

    private void GetContrabandDetailsEventHandler(Entity<ShowContrabandDetailsComponent> ent, ref InventoryRelayedEvent<GetContrabandDetailsEvent> args)
    {
        args.Args.CanShowContraband = true;
    }
}

/// <summary>
///     Raised to the entity to determine if it can see contraband.
/// </summary>
public sealed class GetContrabandDetailsEvent : EntityEventArgs, IInventoryRelayEvent
{
    public bool CanShowContraband;

    public SlotFlags TargetSlots { get; } = SlotFlags.EYES;
}
