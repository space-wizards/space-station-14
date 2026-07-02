using Content.Shared.Fluids;
using Content.Shared.Nutrition.Components;
using Content.Shared.Popups;
using Content.Shared.Random.Helpers;
using Content.Shared.Tag;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Shared.Nutrition.EntitySystems;

public sealed partial class MessyDrinkerSystem : EntitySystem
{
    [Dependency] private IngestionSystem _ingestion = default!;
    [Dependency] private SharedPuddleSystem _puddle = default!;
    [Dependency] private SharedPopupSystem _popup = default!;
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private TagSystem _tag = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<MessyDrinkerComponent, IngestingEvent>(OnIngested);
    }

    private void OnIngested(Entity<MessyDrinkerComponent> ent, ref IngestingEvent ev)
    {
        if (ent.Comp.SpillImmuneTag != null && _tag.HasTag(ev.Food, ent.Comp.SpillImmuneTag.Value))
            return;

        // Cannot spill if you're being forced to drink.
        if (ev.ForceFed)
            return;

        var proto = _ingestion.GetEdibleType(ev.Food);

        if (proto == null || !ent.Comp.SpillableTypes.Contains(proto.Value))
            return;

        if (!SharedRandomExtensions.PredictedProb(_timing, ent.Comp.SpillChance, GetNetEntity(ent)))
            return;

        if (ent.Comp.SpillMessagePopup != null)
            _popup.PopupPredicted(Loc.GetString(ent.Comp.SpillMessagePopup), null, ent, ent, PopupType.MediumCaution);

        var split = ev.Split.SplitSolution(ent.Comp.SpillAmount);

        _puddle.TrySpillAt(ent, split, out _);
    }
}
