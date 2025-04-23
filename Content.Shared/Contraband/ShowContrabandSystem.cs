using Content.Shared.Inventory;

namespace Content.Shared.Contraband;

public sealed partial class ShowContrabandSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();
        Subs.SubscribeWithRelay<ShowContrabandDetailsComponent, GetContrabandDetailsEvent>(OnGetContrabandDetails);
    }

    private void OnGetContrabandDetails(Entity<ShowContrabandDetailsComponent> ent, ref GetContrabandDetailsEvent args)
    {
        args.CanShowContraband = true;
    }
}

/// <summary>
/// Raised on an entity and its inventory to determine if it can see contraband information in the examination window.
/// </summary>
[ByRefEvent]
public sealed class GetContrabandDetailsEvent : EntityEventArgs, IInventoryRelayEvent
{
    public bool CanShowContraband;

    public SlotFlags TargetSlots { get; } = SlotFlags.EYES;
}
