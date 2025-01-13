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
public abstract partial class SharedItemRecallSystem : EntitySystem
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

            if (markItem == null)
                return;

            _popups.PopupClient(Loc.GetString("item-recall-item-marked", ("item", markItem.Value)), args.Performer, args.Performer);
            TryMarkItem(ent, markItem, args.Performer);
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

        UpdateActionAppearance(ent);
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
            if(_net.IsServer) // This is the only way it worked, not even PopupClient
                _popups.PopupEntity(Loc.GetString("item-recall-item-unmark", ("item", item.Value)), marker.MarkedByEntity, marker.MarkedByEntity, PopupType.MediumCaution);

            action.MarkedEntity = null;
            UpdateActionAppearance((marker.MarkedByAction, action));
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

        _popups.PopupEntity(Loc.GetString("item-recall-item-summon", ("item", item.Value)), marker.MarkedByEntity, marker.MarkedByEntity);

        _hands.TryForcePickupAnyHand(marker.MarkedByEntity, item.Value);
    }

    private void UpdateActionAppearance(Entity<ItemRecallComponent> action)
    {
        if (!TryComp<InstantActionComponent>(action, out var instantAction))
            return;

        if (action.Comp.MarkedEntity == null)
        {
            _metaData.SetEntityName(action, Prototype(action)!.Name);
            _metaData.SetEntityDescription(action, Prototype(action)!.Description);
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
        Dirty(action);
    }
}
