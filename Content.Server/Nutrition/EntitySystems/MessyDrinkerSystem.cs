using Content.Server.Fluids.EntitySystems;
using Content.Server.Nutrition.Components;
using Content.Shared.Nutrition;
using Content.Shared.Nutrition.EntitySystems;
using Content.Shared.Popups;
using Robust.Shared.Random;

namespace Content.Server.Nutrition.EntitySystems;

public sealed class MessyDrinkerSystem : EntitySystem
{
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly IngestionSystem _ingestion = default!;
    [Dependency] private readonly PuddleSystem _puddle = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<MessyDrinkerComponent, IngestingEvent>(OnIngested);
    }

    private void OnIngested(Entity<MessyDrinkerComponent> ent, ref IngestingEvent ev)
    {
        if (ev.Split.Volume <= ent.Comp.SpillAmount)
            return;

        var proto = _ingestion.GetEdibleType(ev.Food);

        if (proto == null || !ent.Comp.SpillableTypes.Contains(proto.Value))
            return;

        // Cannot spill if you're being forced to drink.
        if (ev.ForceFed)
            return;

        if (!_random.Prob(ent.Comp.SpillChance))
            return;

        if (ent.Comp.SpillMessagePopup != null)
            _popup.PopupEntity(Loc.GetString(ent.Comp.SpillMessagePopup), ent, ent, PopupType.MediumCaution);

        var split = ev.Split.SplitSolution(ent.Comp.SpillAmount);

        _puddle.TrySpillAt(ent, split, out _);
    }
}
