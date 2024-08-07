using System.Text;
using Content.Server.Nutrition.Components;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Interaction;
using Content.Shared.Nutrition.Components;
using Content.Shared.Nutrition.EntitySystems;
using Content.Shared.Popups;

namespace Content.Server.Nutrition.EntitySystems;

public sealed class FoodSequenceSystem : SharedFoodSequenceSystem
{
    [Dependency] private readonly SharedSolutionContainerSystem _solutionContainer = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly MetaDataSystem _metaData = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<FoodSequenceStartPointComponent, InteractUsingEvent>(OnInteractUsing);
    }

    private void OnInteractUsing(Entity<FoodSequenceStartPointComponent> ent, ref InteractUsingEvent args)
    {
        if (TryComp<FoodSequenceElementComponent>(args.Used, out var sequenceElement))
            TryAddFoodElement(ent, (args.Used, sequenceElement), args.User);
    }

    private bool TryAddFoodElement(Entity<FoodSequenceStartPointComponent> start, Entity<FoodSequenceElementComponent> element, EntityUid? user = null)
    {
        FoodSequenceElementEntry? elementData = null;
        foreach (var entry in element.Comp.Entries)
        {
            if (entry.Key == start.Comp.Key)
            {
                elementData = entry.Value;
                break;
            }
        }

        if (elementData is null)
            return false;

        //if we run out of space, we can still put in one last, final finishing element.
        if (start.Comp.FoodLayers.Count >= start.Comp.MaxLayers && !elementData.Value.Final || start.Comp.Finished)
        {
            if (user is not null)
                _popup.PopupEntity(Loc.GetString("food-sequence-no-space"), start, user.Value);
            return false;
        }

        if (elementData.Value.Sprite is not null)
        {
            start.Comp.FoodLayers.Add(elementData.Value);
            Dirty(start);
        }

        if (elementData.Value.Final)
            start.Comp.Finished = true;

        UpdateFoodName(start);
        MergeFoodSolutions(start, element);
        QueueDel(element);
        return true;
    }

    private void UpdateFoodName(Entity<FoodSequenceStartPointComponent> start)
    {
        if (start.Comp.NameGeneration is null)
            return;

        var content = new StringBuilder();
        var separator = "";
        if (start.Comp.ContentSeparator is not null)
            separator = Loc.GetString(start.Comp.ContentSeparator);

        foreach (var layer in start.Comp.FoodLayers)
        {
            if (layer.Name is not null)
                content.Append(Loc.GetString(layer.Name.Value));

            content.Append(separator);
        }

        var newName = Loc.GetString(start.Comp.NameGeneration.Value,
            ("prefix", start.Comp.NamePrefix is not null ? Loc.GetString(start.Comp.NamePrefix) : ""),
            ("content", content),
            ("suffix", start.Comp.NameSuffix is not null ? Loc.GetString(start.Comp.NameSuffix) : ""));

        _metaData.SetEntityName(start, newName);
    }

    public void MergeFoodSolutions(Entity<FoodSequenceStartPointComponent> start, Entity<FoodSequenceElementComponent> element)
    {
        if (!TryComp<FoodComponent>(start, out var startFood))
            return;

        if (!TryComp<FoodComponent>(element, out var elementFood))
            return;

        if (!_solutionContainer.TryGetSolution(start.Owner, startFood.Solution, out var startSolutionEntity, out var startSolution))
            return;

        if (!_solutionContainer.TryGetSolution(element.Owner, elementFood.Solution, out var elementSolutionEntity, out var elementSolution))
            return;

        startSolution.MaxVolume += elementSolution.MaxVolume;
        _solutionContainer.TryAddSolution(startSolutionEntity.Value, elementSolution);
    }
}
