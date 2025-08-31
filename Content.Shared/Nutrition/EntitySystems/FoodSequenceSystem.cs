using System.Numerics;
using System.Text;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Interaction;
using Content.Shared.Mobs.Systems;
using Content.Shared.Nutrition.Components;
using Content.Shared.Nutrition.Prototypes;
using Content.Shared.Popups;
using Content.Shared.Storage.Components;
using Content.Shared.Tag;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Shared.Nutrition.EntitySystems;

public sealed class FoodSequenceSystem : SharedFoodSequenceSystem
{
    [Dependency] private readonly SharedSolutionContainerSystem _solutionContainer = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly MetaDataSystem _metaData = default!;
    [Dependency] private readonly MobStateSystem _mobState = default!;
    [Dependency] private readonly TagSystem _tag = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly IPrototypeManager _proto = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<FoodSequenceStartPointComponent, InteractUsingEvent>(OnInteractUsing);

        SubscribeLocalEvent<FoodMetamorphableByAddingComponent, FoodSequenceIngredientAddedEvent>(OnIngredientAdded);
    }

    private void OnInteractUsing(Entity<FoodSequenceStartPointComponent> ent, ref InteractUsingEvent args)
    {
        if (TryComp<FoodSequenceElementComponent>(args.Used, out var sequenceElement))
            args.Handled = TryAddFoodElement(ent, (args.Used, sequenceElement), args.User);
    }

    private void OnIngredientAdded(Entity<FoodMetamorphableByAddingComponent> ent, ref FoodSequenceIngredientAddedEvent args)
    {
        if (!TryComp<FoodSequenceStartPointComponent>(args.Start, out var start))
            return;

        if (!_proto.TryIndex(args.Proto, out var elementProto))
            return;

        if (!ent.Comp.OnlyFinal || elementProto.Final || start.FoodLayers.Count == start.MaxLayers)
        {
            TryMetamorph((ent, start));
        }
    }

    private bool TryMetamorph(Entity<FoodSequenceStartPointComponent> start)
    {
        List<MetamorphRecipePrototype> availableRecipes = new();
        foreach (var recipe in _proto.EnumeratePrototypes<MetamorphRecipePrototype>())
        {
            if (recipe.Key != start.Comp.Key)
                continue;

            bool allowed = true;
            foreach (var rule in recipe.Rules)
            {
                if (!rule.Check(_proto, EntityManager, start, start.Comp.FoodLayers))
                {
                    allowed = false;
                    break;
                }
            }
            if (allowed)
                availableRecipes.Add(recipe);
        }

        if (availableRecipes.Count <= 0)
            return true;

        Metamorf(start, _random.Pick(availableRecipes)); //In general, if there's more than one recipe, the yml-guys screwed up. Maybe some kind of unit test is needed.
        QueueDel(start);
        return true;
    }

    private void Metamorf(Entity<FoodSequenceStartPointComponent> start, MetamorphRecipePrototype recipe)
    {
        var result = SpawnAtPosition(recipe.Result, Transform(start).Coordinates);

        //Try putting in container
        _transform.DropNextTo(result, (start, Transform(start)));

        if (!_solutionContainer.TryGetSolution(result, start.Comp.Solution, out var resultSoln, out var resultSolution))
            return;

        if (!_solutionContainer.TryGetSolution(start.Owner, start.Comp.Solution, out var startSoln, out var startSolution))
            return;

        _solutionContainer.RemoveAllSolution(resultSoln.Value); //Remove all YML reagents
        resultSoln.Value.Comp.Solution.MaxVolume = startSoln.Value.Comp.Solution.MaxVolume;
        _solutionContainer.TryAddSolution(resultSoln.Value, startSolution);

        MergeFlavorProfiles(start, result);
        MergeTrash(start, result);
        MergeTags(start, result);
    }

    private bool TryAddFoodElement(Entity<FoodSequenceStartPointComponent> start, Entity<FoodSequenceElementComponent> element, EntityUid? user = null)
    {
        // we can't add a live mouse to a burger.
        if (!TryComp<FoodComponent>(element, out var elementFood))
            return false;
        if (elementFood.RequireDead && _mobState.IsAlive(element))
            return false;

        //looking for a suitable FoodSequence prototype
        if (!element.Comp.Entries.TryGetValue(start.Comp.Key, out var elementProto))
            return false;
        if (!_proto.TryIndex(elementProto, out var elementIndexed))
            return false;

        //if we run out of space, we can still put in one last, final finishing element.
        if (start.Comp.FoodLayers.Count >= start.Comp.MaxLayers && !elementIndexed.Final || start.Comp.Finished)
        {
            if (user is not null)
                _popup.PopupClient(Loc.GetString("food-sequence-no-space"), start, user.Value);
            return false;
        }

        // Prevents plushies with items hidden in them from being added to prevent deletion of items
        // If more of these types of checks need to be added, this should be changed to an event or something.
        if (TryComp<SecretStashComponent>(element, out var stashComponent) && stashComponent.ItemContainer.Count != 0)
        {
            return false;
        }

        //Generate new visual layer
        var flip = start.Comp.AllowHorizontalFlip && _random.Prob(0.5f);
        var layer = new FoodSequenceVisualLayer(elementIndexed,
            _random.Pick(elementIndexed.Sprites),
            new Vector2(flip ? -elementIndexed.Scale.X : elementIndexed.Scale.X, elementIndexed.Scale.Y),
            new Vector2(
                _random.NextFloat(start.Comp.MinLayerOffset.X, start.Comp.MaxLayerOffset.X),
                _random.NextFloat(start.Comp.MinLayerOffset.Y, start.Comp.MaxLayerOffset.Y))
        );

        start.Comp.FoodLayers.Add(layer);
        Dirty(start);

        if (elementIndexed.Final)
            start.Comp.Finished = true;

        UpdateFoodName(start);
        MergeFoodSolutions(start, element);
        MergeFlavorProfiles(start, element);
        MergeTrash(start, element);
        MergeTags(start, element);

        var ev = new FoodSequenceIngredientAddedEvent(start, element, elementProto, user);
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

        HashSet<ProtoId<FoodSequenceElementPrototype>> existedContentNames = new();
        foreach (var layer in start.Comp.FoodLayers)
        {
            if (!existedContentNames.Contains(layer.Proto))
                existedContentNames.Add(layer.Proto);
        }

        var nameCounter = 1;
        foreach (var proto in existedContentNames)
        {
            if (!_proto.TryIndex(proto, out var protoIndexed))
                continue;

            if (protoIndexed.Name is null)
                continue;

            content.Append(Loc.GetString(protoIndexed.Name.Value));

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

    private void MergeFoodSolutions(EntityUid start, EntityUid element)
    {
        if (!TryComp<FoodComponent>(start, out var startFood))
            return;

        if (!TryComp<FoodComponent>(element, out var elementFood))
            return;

        if (!_solutionContainer.TryGetSolution(start, startFood.Solution, out var startSolutionEntity, out var startSolution))
            return;

        if (!_solutionContainer.TryGetSolution(element, elementFood.Solution, out _, out var elementSolution))
            return;

        startSolution.MaxVolume += elementSolution.MaxVolume;
        _solutionContainer.TryAddSolution(startSolutionEntity.Value, elementSolution);
    }

    private void MergeFlavorProfiles(EntityUid start, EntityUid element)
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

    private void MergeTrash(EntityUid start, EntityUid element)
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

    private void MergeTags(EntityUid start, EntityUid element)
    {
        if (!TryComp<TagComponent>(element, out var elementTags))
            return;

        EnsureComp<TagComponent>(start);

        _tag.TryAddTags(start, elementTags.Tags);
    }
}
