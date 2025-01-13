using Content.Shared.Hands.Components;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Projectiles;

namespace Content.Shared.ItemRecall;

/// <summary>
/// uwu
/// </summary>
public sealed partial class SharedItemRecallSystem : EntitySystem
{

    [Dependency] private readonly SharedHandsSystem _hands = default!;
    [Dependency] private readonly SharedProjectileSystem _proj = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ItemRecallComponent, OnItemRecallActionEvent>(OnItemRecallActionUse);

        SubscribeLocalEvent<RecallMarkerComponent, RecallItemEvent>(OnItemRecallEvent);
        SubscribeLocalEvent<RecallMarkerComponent, ComponentShutdown>(OnRecallMarkerShutdown);
    }

    private void OnItemRecallActionUse(Entity<ItemRecallComponent> ent, ref OnItemRecallActionEvent args)
    {

        if (ent.Comp.MarkedEntity == null)
        {
            if (!TryComp<HandsComponent>(args.Performer, out var hands))
                return;

            var markItem = _hands.GetActiveItem((args.Performer, hands));
            TryMarkItem(ent, markItem, args.Performer);
            return;
        }

        var ev = new RecallItemEvent(ent.Comp.MarkedEntity.Value);
        RaiseLocalEvent(ent.Comp.MarkedEntity.Value, ref ev);
        args.Handled = true;
    }

    private void OnRecallMarkerShutdown(Entity<RecallMarkerComponent> ent, ref ComponentShutdown args)
    {
        TryUnmarkItem(ent);
    }

    private void OnItemRecallEvent(Entity<RecallMarkerComponent> ent, ref RecallItemEvent args)
    {
        RecallItem(ent.Owner);
    }

    private void TryMarkItem(Entity<ItemRecallComponent> ent, EntityUid? item, EntityUid markedBy)
    {
        if (item == null)
            return;
        EnsureComp<RecallMarkerComponent>(item.Value, out var marker);
        ent.Comp.MarkedEntity = item;
        Dirty(ent);

        marker.MarkedByEntity = markedBy;
        marker.MarkedByAction = ent.Owner;
        Dirty(item.Value, marker);
    }

    private void TryUnmarkItem(EntityUid? item)
    {
        if (item == null)
            return;

        if (!TryComp<RecallMarkerComponent>(item.Value, out var marker))
            return;

        if (TryComp<ItemRecallComponent>(marker.MarkedByAction, out var action))
        {
            action.MarkedEntity = null;
            Dirty(marker.MarkedByAction, action);
        }

        RemCompDeferred<RecallMarkerComponent>(item.Value);
    }

    private void RecallItem(EntityUid? item)
    {
        if (!TryComp<RecallMarkerComponent>(item, out var marker))
            return;

        if (TryComp<EmbeddableProjectileComponent>(item, out var projectile))
            _proj.UnEmbed(item.Value, marker.MarkedByEntity, projectile);

        _hands.TryForcePickupAnyHand(marker.MarkedByEntity, item.Value);
    }
}
