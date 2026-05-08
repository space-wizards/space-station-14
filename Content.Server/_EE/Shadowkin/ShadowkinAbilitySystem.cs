using System.Threading;
using Content.Shared.Actions;
using Content.Shared.Flash;
using Content.Shared.Flash.Components;
using Content.Shared.Shadowkin;
using Robust.Shared.GameObjects;
using Robust.Shared.Timing;

namespace Content.Server.Shadowkin;

public sealed class ShadowkinAbilitySystem : EntitySystem
{
    [Dependency] private readonly SharedActionsSystem _actions = default!;
    [Dependency] private readonly EntityLookupSystem _lookup = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;

    private const string AnchorFlashDestroyEffect = "ShadowkinAnchorFlashDestroyEffect";
    private readonly HashSet<EntityUid> _flashRange = new();

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ShadowkinAbilityComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<ShadowkinAbilityComponent, ComponentShutdown>(OnShutdown);
        SubscribeLocalEvent<ShadowkinAbilityComponent, PlaceShadowAnchorEvent>(OnPlaceAnchor);
        SubscribeLocalEvent<ShadowkinAbilityComponent, RecallShadowAnchorEvent>(OnRecallAnchor);
        SubscribeLocalEvent<ShadowkinAnchorComponent, EntityTerminatingEvent>(OnAnchorTerminating);
        SubscribeLocalEvent<FlashComponent, AfterFlashActivatedEvent>(OnFlashActivated);
    }

    private void OnMapInit(Entity<ShadowkinAbilityComponent> ent, ref MapInitEvent args)
    {
        _actions.AddAction(ent.Owner, ref ent.Comp.PlaceActionEntity, ent.Comp.PlaceActionId);
        _actions.AddAction(ent.Owner, ref ent.Comp.RecallActionEntity, ent.Comp.RecallActionId);
        SetRecallEnabled(ent.Comp, false);
    }

    private void OnShutdown(Entity<ShadowkinAbilityComponent> ent, ref ComponentShutdown args)
    {
        _actions.RemoveAction(ent.Owner, ent.Comp.PlaceActionEntity);
        _actions.RemoveAction(ent.Owner, ent.Comp.RecallActionEntity);

        ent.Comp.RecallInProgress = false;

        if (ent.Comp.CurrentAnchor is { } anchor && !TerminatingOrDeleted(anchor))
            QueueDel(anchor);
    }

    private void OnPlaceAnchor(Entity<ShadowkinAbilityComponent> ent, ref PlaceShadowAnchorEvent args)
    {
        if (args.Handled)
            return;

        args.Handled = true;

        if (ent.Comp.RecallInProgress)
            return;

        if (ent.Comp.CurrentAnchor is { } oldAnchor && !TerminatingOrDeleted(oldAnchor))
            QueueDel(oldAnchor);

        var coordinates = Transform(ent.Owner).Coordinates;
        Spawn(ent.Comp.PlaceEffectPrototype, coordinates);

        var anchor = Spawn(ent.Comp.AnchorPrototype, coordinates);
        var anchorComp = EnsureComp<ShadowkinAnchorComponent>(anchor);
        anchorComp.AnchorOwner = ent.Owner;
        Dirty(anchor, anchorComp);

        ent.Comp.CurrentAnchor = anchor;
        SetRecallEnabled(ent.Comp, true);
        Dirty(ent.Owner, ent.Comp);
    }

    private void OnRecallAnchor(Entity<ShadowkinAbilityComponent> ent, ref RecallShadowAnchorEvent args)
    {
        if (args.Handled)
            return;

        args.Handled = true;

        if (ent.Comp.RecallInProgress)
            return;

        if (ent.Comp.CurrentAnchor is not { } anchor || TerminatingOrDeleted(anchor))
        {
            ClearAnchor(ent);
            return;
        }

        var userXform = Transform(ent.Owner);
        if (!TryComp(anchor, out TransformComponent? anchorXform))
        {
            ClearAnchor(ent);
            return;
        }

        if (userXform.GridUid is not { } userGrid || anchorXform.GridUid != userGrid)
            return;

        Spawn(ent.Comp.RecallDepartEffectPrototype, userXform.Coordinates);

        ent.Comp.RecallInProgress = true;
        SetRecallEnabled(ent.Comp, false);
        SetPlaceEnabled(ent.Comp, false);
        Dirty(ent.Owner, ent.Comp);

        Robust.Shared.Timing.Timer.Spawn(
            (int) (ent.Comp.RecallDelay * 1000),
            () => CompleteRecall(ent.Owner, anchor),
            CancellationToken.None);
    }

    private void CompleteRecall(EntityUid uid, EntityUid anchor)
    {
        if (TerminatingOrDeleted(uid) ||
            !TryComp<ShadowkinAbilityComponent>(uid, out var ability))
        {
            return;
        }

        ability.RecallInProgress = false;
        SetPlaceEnabled(ability, true);

        if (TerminatingOrDeleted(anchor))
        {
            ClearAnchor((uid, ability), false);
            return;
        }

        var userXform = Transform(uid);
        if (!TryComp(anchor, out TransformComponent? anchorXform))
        {
            ClearAnchor((uid, ability), false);
            return;
        }

        if (userXform.GridUid is not { } userGrid || anchorXform.GridUid != userGrid)
        {
            SetRecallEnabled(ability, true);
            Dirty(uid, ability);
            return;
        }

        Spawn(ability.RecallArriveEffectPrototype, anchorXform.Coordinates);
        _transform.SetCoordinates(uid, userXform, anchorXform.Coordinates);
        QueueDel(anchor);
        ClearAnchor((uid, ability), false);
    }

    private void OnAnchorTerminating(Entity<ShadowkinAnchorComponent> ent, ref EntityTerminatingEvent args)
    {
        if (ent.Comp.AnchorOwner is not { } owner ||
            !TryComp<ShadowkinAbilityComponent>(owner, out var ability) ||
            ability.CurrentAnchor != ent.Owner)
        {
            return;
        }

        ability.CurrentAnchor = null;
        SetRecallEnabled(ability, false);
        Dirty(owner, ability);
    }

    private void OnFlashActivated(Entity<FlashComponent> ent, ref AfterFlashActivatedEvent args)
    {
        _flashRange.Clear();
        _lookup.GetEntitiesInRange(ent.Owner, ent.Comp.Range, _flashRange, LookupFlags.Uncontained);

        foreach (var uid in _flashRange)
            TryDispelAnchor(uid);
    }

    private void TryDispelAnchor(EntityUid uid)
    {
        if (TerminatingOrDeleted(uid) ||
            !TryComp<ShadowkinAnchorComponent>(uid, out var anchor))
        {
            return;
        }

        var coordinates = Transform(uid).Coordinates;
        Spawn(AnchorFlashDestroyEffect, coordinates);

        if (anchor.AnchorOwner is { } owner &&
            TryComp<ShadowkinAbilityComponent>(owner, out var ability) &&
            ability.CurrentAnchor == uid)
        {
            ClearAnchor((owner, ability), false);
        }

        QueueDel(uid);
    }

    private void ClearAnchor(Entity<ShadowkinAbilityComponent> ent, bool deleteAnchor = true)
    {
        if (deleteAnchor && ent.Comp.CurrentAnchor is { } anchor && !TerminatingOrDeleted(anchor))
            QueueDel(anchor);

        ent.Comp.CurrentAnchor = null;
        SetRecallEnabled(ent.Comp, false);
        Dirty(ent.Owner, ent.Comp);
    }

    private void SetRecallEnabled(ShadowkinAbilityComponent component, bool enabled)
    {
        if (component.RecallActionEntity is { } action)
            _actions.SetEnabled((action, null), enabled);
    }

    private void SetPlaceEnabled(ShadowkinAbilityComponent component, bool enabled)
    {
        if (component.PlaceActionEntity is { } action)
            _actions.SetEnabled((action, null), enabled);
    }
}
