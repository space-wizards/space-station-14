using Content.Server.Fluids.EntitySystems;
using Content.Server.Nutrition.Components;
using Content.Server.Nutrition.Events;
using Content.Shared.Popups;
using Robust.Shared.Random;

namespace Content.Server.Nutrition.EntitySystems;

public sealed class MessyDrinkerSystem : EntitySystem
{
    [Dependency] private readonly PuddleSystem _puddle = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<MessyDrinkerComponent, BeforeIngestDrinkEvent>(OnBeforeIngestDrink);
    }

    private void OnBeforeIngestDrink(Entity<MessyDrinkerComponent> ent, ref BeforeIngestDrinkEvent ev)
    {
        if (ev.Solution.Volume <= ent.Comp.SpillAmount)
            return;

        // Cannot spill if you're being forced to drink.
        if (ev.Forced)
            return;

        if (!_random.Prob(ent.Comp.SpillChance))
            return;

        if (ent.Comp.SpillMessagePopup != null)
            _popup.PopupEntity(Loc.GetString(ent.Comp.SpillMessagePopup), ent, ent, PopupType.MediumCaution);

        var split = ev.Solution.SplitSolution(ent.Comp.SpillAmount);

        _puddle.TrySpillAt(ent, split, out _);
    }
}
