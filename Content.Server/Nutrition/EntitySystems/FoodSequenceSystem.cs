using System.Numerics;
using System.Text;
using Content.Server.Nutrition.Components;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Interaction;
using Content.Shared.Mobs.Systems;
using Content.Shared.Nutrition;
using Content.Shared.Nutrition.Components;
using Content.Shared.Nutrition.EntitySystems;
using Content.Shared.Popups;
using Robust.Shared.Random;

namespace Content.Server.Nutrition.EntitySystems;

public sealed class FoodSequenceSystem : SharedFoodSequenceSystem
{
    [Dependency] private readonly SharedSolutionContainerSystem _solutionContainer = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly MetaDataSystem _metaData = default!;
    [Dependency] private readonly MobStateSystem _mobState = default!;
    [Dependency] private readonly IRobustRandom _random = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<FoodSequenceStartPointComponent, InteractUsingEvent>(OnInteractUsing);
        SubscribeLocalEvent<FoodMetamorphableByAddingComponent, FoodSequenceIngredientAddedEvent>(OnIngredientAdded);
    }

    private void OnInteractUsing(Entity<FoodSequenceStartPointComponent> ent, ref InteractUsingEvent args)
    {
        if (TryComp<FoodSequenceElementComponent>(args.Used, out var sequenceElement))
            TryAddFoodElement(ent, (args.Used, sequenceElement), args.User);
    }

    private void OnIngredientAdded(Entity<FoodMetamorphableByAddingComponent> ent, ref FoodSequenceIngredientAddedEvent args)
    {
        if (!TryComp<FoodSequenceStartPointComponent>(args.Start, out var start))
            return;

        if (!TryComp<FoodSequenceElementComponent>(args.Element, out var element))
            return;


    }

    private bool TryAddFoodElement(Entity<FoodSequenceStartPointComponent> start, Entity<FoodSequenceElementComponent> element, EntityUid? user = null)
    {
        if (TryComp<FoodComponent>(element, out var elementFood) && elementFood.RequireDead)
        {
            if (_mobState.IsAlive(element))
                return false;
        }

        //the first thing we do is collect our data. We need to use standard data + overwrite some fields described in specific keys
        var defaultData = element.Comp.Data;
        var elementData = FoodSequenceElementEntry.Clone(defaultData);

        foreach (var entry in element.Comp.Entries)
        {
            if (entry.Key == start.Comp.Key)
            {
                if (entry.Value.Name is not null) elementData.Name = entry.Value.Name;
                if (entry.Value.Sprite is not null) elementData.Sprite = entry.Value.Sprite;
                if (entry.Value.Scale != Vector2.One) elementData.Scale = entry.Value.Scale;
                break;
            }
        }

        //if we run out of space, we can still put in one last, final finishing element.
        if (start.Comp.FoodLayers.Count >= start.Comp.MaxLayers && !elementData.Final || start.Comp.Finished)
        {
            if (user is not null)
                _popup.PopupEntity(Loc.GetString("food-sequence-no-space"), start, user.Value);
            return false;
        }

        elementData.LocalOffset = new Vector2(
            _random.NextFloat(start.Comp.MinLayerOffset.X,start.Comp.MaxLayerOffset.X),
            _random.NextFloat(start.Comp.MinLayerOffset.Y,start.Comp.MaxLayerOffset.Y));

        start.Comp.FoodLayers.Add(elementData);
        Dirty(start);

        if (elementData.Final)
            start.Comp.Finished = true;

        UpdateFoodName(start);
        MergeFoodSolutions(start, element);
        MergeFlavorProfiles(start, element);
        MergeTrash(start, element);

        var ev = new FoodSequenceIngredientAddedEvent(start, element, user);
        RaiseLocalEvent(start, ev);

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

        HashSet<LocId> existedContentNames = new();
        foreach (var layer in start.Comp.FoodLayers)
        {
            if (layer.Name is not null && !existedContentNames.Contains(layer.Name.Value))
                existedContentNames.Add(layer.Name.Value);
        }

        var nameCounter = 1;
        foreach (var name in existedContentNames)
        {
            content.Append(Loc.GetString(name));

            if (nameCounter < existedContentNames.Count)
                content.Append(separator);
            nameCounter++;
        }

        var newName = Loc.GetString(start.Comp.NameGeneration.Value,
            ("prefix", start.Comp.NamePrefix is not null ? Loc.GetString(start.Comp.NamePrefix) : ""),
            ("content", content),
            ("suffix", start.Comp.NameSuffix is not null ? Loc.GetString(start.Comp.NameSuffix) : ""));

        _metaData.SetEntityName(start, newName);
    }

    private void MergeFoodSolutions(Entity<FoodSequenceStartPointComponent> start, Entity<FoodSequenceElementComponent> element)
    {
        if (!_solutionContainer.TryGetSolution(start.Owner, start.Comp.Solution, out var startSolutionEntity, out var startSolution))
            return;

        if (!_solutionContainer.TryGetSolution(element.Owner, element.Comp.Solution, out _, out var elementSolution))
            return;

        startSolution.MaxVolume += elementSolution.MaxVolume;
        _solutionContainer.TryAddSolution(startSolutionEntity.Value, elementSolution);
    }

    private void MergeFlavorProfiles(Entity<FoodSequenceStartPointComponent> start, Entity<FoodSequenceElementComponent> element)
    {
        if (!TryComp<FlavorProfileComponent>(start, out var startProfile))
            return;

        if (!TryComp<FlavorProfileComponent>(element, out var elementProfile))
            return;

        foreach (var flavor in elementProfile.Flavors)
        {
            if (startProfile != null && !startProfile.Flavors.Contains(flavor))
                startProfile.Flavors.Add(flavor);
        }
    }

    private void MergeTrash(Entity<FoodSequenceStartPointComponent> start, Entity<FoodSequenceElementComponent> element)
    {
        if (!TryComp<FoodComponent>(start, out var startFood))
            return;

        if (!TryComp<FoodComponent>(element, out var elementFood))
            return;

        foreach (var trash in elementFood.Trash)
        {
            startFood.Trash.Add(trash);
        }
    }
}
