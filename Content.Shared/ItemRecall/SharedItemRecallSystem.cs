using Content.Shared.Actions;
using Content.Shared.Hands.Components;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Popups;
using Content.Shared.Projectiles;
using Robust.Shared.Network;

namespace Content.Shared.ItemRecall;

/// <summary>
/// System for handling the ItemRecall ability for wizards.
/// </summary>
public sealed partial class SharedItemRecallSystem : EntitySystem
{
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly SharedActionsSystem _actions = default!;
    [Dependency] private readonly SharedHandsSystem _hands = default!;
    [Dependency] private readonly SharedProjectileSystem _proj = default!;
    [Dependency] private readonly MetaDataSystem _metaData = default!;
    [Dependency] private readonly SharedPopupSystem _popups = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ItemRecallComponent, OnItemRecallActionEvent>(OnItemRecallActionUse);

        SubscribeLocalEvent<RecallMarkerComponent, RecallItemEvent>(OnItemRecall);
        SubscribeLocalEvent<RecallMarkerComponent, ComponentShutdown>(OnRecallMarkerShutdown);
    }

    private void OnItemRecallActionUse(Entity<ItemRecallComponent> ent, ref OnItemRecallActionEvent args)
    {
        if (ent.Comp.MarkedEntity == null)
        {
            if (!TryComp<HandsComponent>(args.Performer, out var hands))
                return;

            var markItem = _hands.GetActiveItem((args.Performer, hands));

            if (markItem == null)
                return;

            _popups.PopupClient(Loc.GetString("item-recall-item-marked", ("item", markItem.Value)), args.Performer, args.Performer);
            TryMarkItem(ent, markItem.Value, args.Performer);
            return;
        }

        var ev = new RecallItemEvent();
        RaiseLocalEvent(ent.Comp.MarkedEntity.Value, ref ev);
        args.Handled = true;
    }

    private void OnRecallMarkerShutdown(Entity<RecallMarkerComponent> ent, ref ComponentShutdown args)
    {
        TryUnmarkItem(ent);
    }

    private void OnItemRecall(Entity<RecallMarkerComponent> ent, ref RecallItemEvent args)
    {
        RecallItem(ent);
    }

    private void TryMarkItem(Entity<ItemRecallComponent> ent, EntityUid item, EntityUid markedBy)
    {
        EnsureComp<RecallMarkerComponent>(item, out var marker);
        ent.Comp.MarkedEntity = item;
        Dirty(ent);

        marker.MarkedByEntity = markedBy;
        marker.MarkedByAction = ent.Owner;

        UpdateActionAppearance(ent);
        Dirty(item, marker);
    }

    private void TryUnmarkItem(EntityUid item)
    {
        if (!TryComp<RecallMarkerComponent>(item, out var marker))
            return;

        if (marker.MarkedByEntity == null)
            return;

        if (TryComp<ItemRecallComponent>(marker.MarkedByAction, out var action))
        {
            if(_net.IsServer)
                _popups.PopupEntity(Loc.GetString("item-recall-item-unmark", ("item", item)), marker.MarkedByEntity.Value, marker.MarkedByEntity.Value, PopupType.MediumCaution);

            action.MarkedEntity = null;
            UpdateActionAppearance((marker.MarkedByAction.Value, action));
            Dirty(marker.MarkedByAction.Value, action);
        }

        RemCompDeferred<RecallMarkerComponent>(item);
    }

    private void RecallItem(Entity<RecallMarkerComponent> ent)
    {
        if (ent.Comp.MarkedByEntity == null)
            return;

        if (TryComp<EmbeddableProjectileComponent>(ent, out var projectile))
            _proj.UnEmbed(ent, projectile, ent.Comp.MarkedByEntity.Value);

        _popups.PopupPredicted(Loc.GetString("item-recall-item-summon", ("item", ent)), ent.Comp.MarkedByEntity.Value, ent.Comp.MarkedByEntity.Value);

        _hands.TryForcePickupAnyHand(ent.Comp.MarkedByEntity.Value, ent);
    }

    private void UpdateActionAppearance(Entity<ItemRecallComponent> action)
    {
        if (!TryComp<InstantActionComponent>(action, out var instantAction))
            return;

        var proto = Prototype(action);

        if (proto == null)
            return;

        if (action.Comp.MarkedEntity == null)
        {
            _metaData.SetEntityName(action, proto.Name);
            _metaData.SetEntityDescription(action, proto.Description);
            _actions.SetEntityIcon(action, null, instantAction);
        }
        else
        {
            if (action.Comp.WhileMarkedName != null)
                _metaData.SetEntityName(action, Loc.GetString(action.Comp.WhileMarkedName,
                    ("item", action.Comp.MarkedEntity.Value)));

            if (action.Comp.WhileMarkedDescription != null)
                _metaData.SetEntityDescription(action, Loc.GetString(action.Comp.WhileMarkedDescription,
                    ("item", action.Comp.MarkedEntity.Value)));

            _actions.SetEntityIcon(action, action.Comp.MarkedEntity, instantAction);
        }
    }
}
