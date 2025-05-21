using System.Linq;
using Content.Server.Nutrition.Components;
using Content.Server.Popups;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.FixedPoint;
using Content.Shared.Interaction;
using Robust.Server.Audio;

namespace Content.Server._Impstation.FoodReagentExtractor;

// TODO the only thing keeping this system in server is food.
/// <summary>
///     System for extracting reagents from <see cref="FoodComponent">.
/// </summary>
/// <seealso cref="FoodReagentExtractorComponent"/>
public sealed class FoodReagentExtractorSystem : EntitySystem
{
    [Dependency] private readonly SharedSolutionContainerSystem _solutionContainer = default!;
    [Dependency] private readonly AudioSystem _audio = default!;
    [Dependency] private readonly PopupSystem _popup = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<FoodReagentExtractorComponent, AfterInteractUsingEvent>(OnInteractUsing);
    }

    private void OnInteractUsing(Entity<FoodReagentExtractorComponent> ent, ref AfterInteractUsingEvent args)
    {
        if (args.Handled)
            return;

        if (!TryComp<FoodComponent>(args.Used, out var food))
            return;

        if (!_solutionContainer.TryGetSolution(args.Used, food.Solution, out var foodSol) ||
            !_solutionContainer.TryGetSolution(ent.Owner, ent.Comp.SolutionId, out var entSol))
            return;

        // IEnumerable for reagents that match the extraction reagents
        var reagentMatch = foodSol.Value.Comp.Solution.Contents
                           .Where(r => ent.Comp.ExtractionReagents.Contains(r.Reagent.Prototype));

        if (!reagentMatch.Any())
        {
            _popup.PopupEntity(Loc.GetString(ent.Comp.MessageBadFood, ("extractor", ent), ("food", args.Used)), ent, args.User);
            return; // Food doesn't have anything we want
        }

        var total = FixedPoint2.Zero;
        foreach (var reagent in reagentMatch)
        {
            _solutionContainer.TryAddReagent(entSol.Value, reagent, out var transfer);
            total += transfer;
        }

        if (total <= 0)
        {
            _popup.PopupEntity(Loc.GetString(ent.Comp.MessageSolutionFull, ("extractor", ent)), ent, args.User);
            return; // Nothing was transfered
        }

        if (ent.Comp.ExtractSound != null)
            _audio.PlayPvs(ent.Comp.ExtractSound, ent);

        _popup.PopupEntity(Loc.GetString(ent.Comp.MessageFoodEaten, ("extractor", ent), ("food", args.Used)), ent, args.User);
        QueueDel(args.Used);

        args.Handled = true;
    }
}
