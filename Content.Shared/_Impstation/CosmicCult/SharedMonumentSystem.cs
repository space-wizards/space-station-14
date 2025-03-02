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
        SubscribeLocalEvent<MonumentComponent, PreventCollideEvent>(OnPreventCollide);
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
    private void OnPreventCollide(EntityUid uid, MonumentComponent comp, ref PreventCollideEvent args)
    {
        if (!HasComp<CosmicCultComponent>(args.OtherEntity) && !comp.HasCollision)
            args.Cancelled = true;
    }

    private void OnUIOpened(Entity<MonumentComponent> ent, ref BoundUIOpenedEvent args)
    {
        if (!_ui.IsUiOpen(ent.Owner, MonumentKey.Key) || !TryComp<ActivatableUIComponent>(ent, out var uiComp))
            return;
        if (ent.Comp.Enabled && TryComp<CosmicCultComponent>(uiComp.CurrentSingleUser, out var cultComp))
        {
            //todo remove these later when I get around to taking influences out of the state
            ent.Comp.AvailableEntropy = cultComp.EntropyBudget;
            ent.Comp.UnlockedInfluences = cultComp.UnlockedInfluences;

            var buiState = new MonumentBuiState(
                cultComp.EntropyBudget,
                ent.Comp.EntropyUntilNextStage,
                ent.Comp.CrewToConvertNextStage,
                ent.Comp.PercentageComplete,
                ent.Comp.SelectedGlyph,
                cultComp.UnlockedInfluences,
                ent.Comp.UnlockedGlyphs
                );

            _ui.SetUiState(ent.Owner, MonumentKey.Key, buiState);
        }
        else
            _ui.CloseUi(ent.Owner, MonumentKey.Key); // based on the prior IF, this effectively cancels the UI if the user is either not a cultist, or the Finale is ready to trigger.

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

        if (ent.Comp.CurrentGlyph is not null) QueueDel(ent.Comp.CurrentGlyph);
        var glyphEnt = Spawn(proto.Entity, _map.ToCenterCoordinates(xform.GridUid.Value, targetIndices, grid));
        ent.Comp.CurrentGlyph = glyphEnt;

        _ui.SetUiState(ent.Owner, MonumentKey.Key, new MonumentBuiState(ent.Comp));
        _ui.CloseUi(ent.Owner, MonumentKey.Key);
    }

    private void OnGlyphRemove(Entity<MonumentComponent> ent, ref GlyphRemovedMessage args)
    {
        if (ent.Comp.CurrentGlyph is not null) QueueDel(ent.Comp.CurrentGlyph);
        _ui.SetUiState(ent.Owner, MonumentKey.Key, new MonumentBuiState(ent.Comp));
        _ui.CloseUi(ent.Owner, MonumentKey.Key);
    }

    private void OnInfluenceSelected(Entity<MonumentComponent> ent, ref InfluenceSelectedMessage args)
    {
        if (!_prototype.TryIndex(args.InfluenceProtoId, out var proto) || !TryComp<ActivatableUIComponent>(ent, out var uiComp) || !TryComp<CosmicCultComponent>(uiComp.CurrentSingleUser, out var cultComp))
            return;
        if (ent.Comp.AvailableEntropy < proto.Cost || cultComp.OwnedInfluences.Contains(proto) || uiComp.CurrentSingleUser == null)
            return;
        cultComp.OwnedInfluences.Add(proto);

        if (proto.InfluenceType == "influence-type-active")
        {
            var actionEnt = _actions.AddAction(uiComp.CurrentSingleUser.Value, proto.Action);
            cultComp.ActionEntities.Add(actionEnt);
        }
        else if (proto.InfluenceType == "influence-type-passive")
        {
            UnlockPassive(uiComp.CurrentSingleUser.Value, proto); //Not unlocking an action? call the helper function to add the influence's passive effects
        }

        ent.Comp.AvailableEntropy -= proto.Cost;
        cultComp.EntropyBudget -= proto.Cost;
        ent.Comp.UnlockedInfluences.Remove(args.InfluenceProtoId); //todo fiddle with this? this seems like a wierd way of doing things, but makes sense after some reading through the code paths - ruddygreat
        cultComp.UnlockedInfluences.Remove(args.InfluenceProtoId); //either way, will leave this for the PR that cleans up influence tiering
        Dirty(uiComp.CurrentSingleUser.Value, cultComp); //force an update to make sure that the client has the correct set of owned abilities

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
