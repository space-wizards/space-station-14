using Content.Shared.Fluids;
using Content.Shared.Nutrition.Components;
using Content.Shared.Popups;
using Content.Shared.Random.Helpers;
using Content.Shared.Tag;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Shared.Nutrition.EntitySystems;

public sealed class MessyDrinkerSystem : EntitySystem
{
    [Dependency] private readonly IngestionSystem _ingestion = default!;
    [Dependency] private readonly SharedPuddleSystem _puddle = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly TagSystem _tag = default!;

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

        // TODO: Replace with RandomPredicted once the engine PR is merged
        var seed = SharedRandomExtensions.HashCodeCombine((int)_timing.CurTick.Value, GetNetEntity(ent).Id);
        var rand = new System.Random(seed);
        if (!rand.Prob(ent.Comp.SpillChance))
            return;

        if (ent.Comp.SpillMessagePopup != null)
            _popup.PopupPredicted(Loc.GetString(ent.Comp.SpillMessagePopup), null, ent, ent, PopupType.MediumCaution);

        var split = ev.Split.SplitSolution(ent.Comp.SpillAmount);

        _puddle.TrySpillAt(ent, split, out _);
    }
}
