using Content.Shared.Actions.Components;
using Content.Shared.Hands.EntitySystems;
using Robust.Shared.Prototypes;

namespace Content.Shared.Actions;

/// <summary>
/// Creates an entity in the hands of the entity using the action.
/// </summary>
public sealed class ItemCreateActionSystem : EntitySystem
{
    [Dependency] private readonly SharedHandsSystem _hands = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<ActionsContainerComponent, CreateItemEvent>(OnCreateItem);
    }

    private void OnCreateItem(Entity<ActionsContainerComponent> ent, ref CreateItemEvent args)
    {
        var ev = new CreateItemAttemptEvent(args.Performer);
        RaiseLocalEvent(ent, ref ev);
        if (ev.Cancelled)
            return;

        // try to put item in hand, otherwise it goes on the ground
        var item = PredictedSpawnAtPosition(args.ProtoId, Transform(args.Performer).Coordinates);
        _hands.TryPickupAnyHand(args.Performer, item);

        args.Handled = true;
    }
}

/// <summary>
/// Raised on the entity action container for the action, which can be used to cancel the spawning.
/// </summary>
[ByRefEvent]
public record struct CreateItemAttemptEvent(EntityUid User, bool Cancelled = false);

public sealed partial class CreateItemEvent : InstantActionEvent
{
    /// <summary>
    /// The entity prototype that will be spawned via this action.
    /// </summary>
    [DataField]
    public EntProtoId ProtoId;

    public CreateItemEvent(EntProtoId protoId)
    {
        ProtoId = protoId;
    }
}
