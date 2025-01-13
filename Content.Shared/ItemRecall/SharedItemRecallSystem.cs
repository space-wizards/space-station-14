using Content.Shared.Hands.Components;
using Content.Shared.Hands.EntitySystems;

namespace Content.Shared.ItemRecall;

/// <summary>
/// uwu
/// </summary>
public sealed partial class SharedItemRecallSystem : EntitySystem
{

    [Dependency] private readonly SharedHandsSystem _hands = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ItemRecallComponent, OnItemRecallActionEvent>(OnItemRecallUse);
    }

    public void OnItemRecallUse(Entity<ItemRecallComponent> ent, ref OnItemRecallActionEvent args)
    {

        if (ent.Comp.MarkedEntity == null)
        {
            Log.Debug("Trying for hands");
            if (!TryComp<HandsComponent>(args.Performer, out var hands))
                return;

            Log.Debug("Marking");
            var markItem = _hands.GetActiveItem((args.Performer, hands));
            TryMarkItem(ent, markItem, args.Performer);
            return;
        }

        RecallItem(ent.Comp.MarkedEntity);
    }

    private void TryMarkItem(Entity<ItemRecallComponent> ent, EntityUid? item, EntityUid markedBy)
    {
        if (item == null)
            return;
        Log.Debug("Adding component");
        EnsureComp<RecallMarkerComponent>(item.Value, out var marker);
        ent.Comp.MarkedEntity = item;
        marker.MarkedByEntity = markedBy;
    }

    private void RecallItem(EntityUid? item)
    {
        if (!TryComp<RecallMarkerComponent>(item, out var marker))
            return;

        _hands.TryForcePickupAnyHand(marker.MarkedByEntity, item.Value);
    }
}
