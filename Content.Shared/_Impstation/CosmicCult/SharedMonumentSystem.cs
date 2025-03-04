using Content.Shared._Impstation.Cosmiccult;
using Content.Shared._Impstation.CosmicCult.Components;
using Content.Shared._Impstation.CosmicCult.Prototypes;
using Content.Shared.Actions;
using Content.Shared.Movement.Components;
using Content.Shared.Nutrition.Components;
using Content.Shared.UserInterface;
using Robust.Shared.Map.Components;
using Robust.Shared.Physics.Events;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Content.Shared._Impstation.CosmicCult;

public sealed class SharedMonumentSystem : EntitySystem
{
    [Dependency] private readonly SharedUserInterfaceSystem _ui = default!;
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
    [Dependency] private readonly SharedMapSystem _map = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly IPrototypeManager _prototype = default!;
    [Dependency] private readonly SharedActionsSystem _actions = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<MonumentComponent, BoundUIOpenedEvent>(OnUIOpened);
        SubscribeLocalEvent<MonumentComponent, GlyphSelectedMessage>(OnGlyphSelected);
        SubscribeLocalEvent<MonumentComponent, GlyphRemovedMessage>(OnGlyphRemove);
        SubscribeLocalEvent<MonumentComponent, InfluenceSelectedMessage>(OnInfluenceSelected);
        SubscribeLocalEvent<MonumentCollisionComponent, PreventCollideEvent>(OnPreventCollide);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);
        var query = EntityQueryEnumerator<MonumentTransformingComponent>();
        while (query.MoveNext(out var uid, out var comp))
        {
            if (_timing.CurTime < comp.EndTime)
                continue;
            _appearance.SetData(uid, MonumentVisuals.Transforming, false);
            RemComp<MonumentTransformingComponent>(uid);
        }
    }

    /// <summary>
    /// Ensures that Cultists can't walk through The Monument and allows non-cultists to walk through the space.
    /// </summary>
    private void OnPreventCollide(EntityUid uid, MonumentCollisionComponent comp, ref PreventCollideEvent args)
    {
        if (!HasComp<CosmicCultComponent>(args.OtherEntity) && !comp.HasCollision)
            args.Cancelled = true;
    }

    private void OnUIOpened(Entity<MonumentComponent> ent, ref BoundUIOpenedEvent args)
    {
        if (!_ui.IsUiOpen(ent.Owner, MonumentKey.Key))
            return;

        if (ent.Comp.Enabled && HasComp<CosmicCultComponent>(args.Actor))
        {
            _ui.SetUiState(ent.Owner, MonumentKey.Key, new MonumentBuiState(ent.Comp));
        }
        else
            _ui.CloseUi(ent.Owner, MonumentKey.Key); //close the UI if the monument isn't available
    }

    #region UI listeners
    private void OnGlyphSelected(Entity<MonumentComponent> ent, ref GlyphSelectedMessage args)
    {
        ent.Comp.SelectedGlyph = args.GlyphProtoId;

        if (!_prototype.TryIndex(args.GlyphProtoId, out var proto))
            return;

        var xform = Transform(ent);

        if (!TryComp<MapGridComponent>(xform.GridUid, out var grid))
            return;

        var localTile = _map.GetTileRef(xform.GridUid.Value, grid, xform.Coordinates);
        var targetIndices = localTile.GridIndices + new Vector2i(0, -1);

        if (ent.Comp.CurrentGlyph is not null)
            QueueDel(ent.Comp.CurrentGlyph);

        var glyphEnt = Spawn(proto.Entity, _map.ToCenterCoordinates(xform.GridUid.Value, targetIndices, grid));
        ent.Comp.CurrentGlyph = glyphEnt;

        _ui.SetUiState(ent.Owner, MonumentKey.Key, new MonumentBuiState(ent.Comp));
    }

    private void OnGlyphRemove(Entity<MonumentComponent> ent, ref GlyphRemovedMessage args)
    {
        if (ent.Comp.CurrentGlyph is not null)
            QueueDel(ent.Comp.CurrentGlyph);

        _ui.SetUiState(ent.Owner, MonumentKey.Key, new MonumentBuiState(ent.Comp));
    }

    private void OnInfluenceSelected(Entity<MonumentComponent> ent, ref InfluenceSelectedMessage args)
    {

        var senderEnt = GetEntity(args.Sender);

        if (!_prototype.TryIndex(args.InfluenceProtoId, out var proto) || !TryComp<ActivatableUIComponent>(ent, out var uiComp) || !TryComp<CosmicCultComponent>(senderEnt, out var cultComp))
            return;

        if (cultComp.EntropyBudget < proto.Cost || cultComp.OwnedInfluences.Contains(proto) || args.Sender == null)
            return;

        cultComp.OwnedInfluences.Add(proto);

        if (proto.InfluenceType == "influence-type-active")
        {
            var actionEnt = _actions.AddAction(senderEnt.Value, proto.Action);
            cultComp.ActionEntities.Add(actionEnt);
        }
        else if (proto.InfluenceType == "influence-type-passive")
        {
            UnlockPassive(senderEnt.Value, proto); //Not unlocking an action? call the helper function to add the influence's passive effects
        }

        cultComp.EntropyBudget -= proto.Cost;
        Dirty(senderEnt.Value, cultComp); //force an update to make sure that the client has the correct set of owned abilities

        _ui.SetUiState(ent.Owner, MonumentKey.Key, new MonumentBuiState(ent.Comp));
    }
    #endregion

    private void UnlockPassive(EntityUid cultist, InfluencePrototype proto)
    {
        switch (proto.PassiveName) // Yay, switch statements.
        {
            case "eschew":
                RemCompDeferred<HungerComponent>(cultist);
                RemCompDeferred<ThirstComponent>(cultist);
                break;
            case "step":
                EnsureComp<MovementIgnoreGravityComponent>(cultist);
                break;
            case "stride":
                EnsureComp<InfluenceStrideComponent>(cultist);
                break;
            case "vitality":
                EnsureComp<InfluenceVitalityComponent>(cultist);
                break;
        }
    }
}
