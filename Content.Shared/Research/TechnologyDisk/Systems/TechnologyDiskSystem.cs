using Content.Shared.Cargo;
using Content.Shared.Examine;
using Content.Shared.Interaction;
using Content.Shared.Lathe;
using Content.Shared.NameModifier.EntitySystems;
using Content.Shared.Popups;
using Content.Shared.Random.Helpers;
using Content.Shared.Research.Components;
using Content.Shared.Research.Prototypes;
using Content.Shared.Research.Systems;
using Content.Shared.Research.TechnologyDisk.Components;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Serialization;

namespace Content.Shared.Research.TechnologyDisk.Systems;

public sealed class TechnologyDiskSystem : EntitySystem
{
    [Dependency] private readonly IPrototypeManager _protoMan = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly ResearchSystem _research = default!;
    [Dependency] private readonly SharedLatheSystem _lathe = default!;
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
    [Dependency] private readonly NameModifierSystem _nameModifier = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<TechnologyDiskComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<TechnologyDiskComponent, AfterInteractEvent>(OnAfterInteract);
        SubscribeLocalEvent<TechnologyDiskComponent, ExaminedEvent>(OnExamine);
        SubscribeLocalEvent<TechnologyDiskComponent, PriceCalculationEvent>(OnPriceCalculation);
        SubscribeLocalEvent<TechnologyDiskComponent, RefreshNameModifiersEvent>(OnRefreshNameModifiers);
    }

    private void OnMapInit(Entity<TechnologyDiskComponent> ent, ref MapInitEvent args)
    {
        TryPickAndSetRecipe(ent);
        TrySetVisuals(ent);
    }

    /// <summary>
    /// Attempts to pick and set a random recipe as the chosen one.
    /// If the disk already has recipes, does nothing.
    /// </summary>
    private void TryPickAndSetRecipe(Entity<TechnologyDiskComponent> ent)
    {
        if (ent.Comp.Recipes != null)
            return;

        int tier;
        if (ent.Comp.Tier.HasValue)
        {
            tier = ent.Comp.Tier.Value;
        }
        else
        {
            var weightedRandom = _protoMan.Index(ent.Comp.TierWeightPrototype);
            tier = int.Parse(weightedRandom.Pick(_random));
            ent.Comp.Tier = tier;
        }

        // get a list of every distinct recipe in all the technologies.
        var bundles = new HashSet<(ProtoId<LatheRecipePrototype> recipe, ProtoId<TechDisciplinePrototype> discipline)>();
        foreach (var tech in _protoMan.EnumeratePrototypes<TechnologyPrototype>())
        {
            if (tech.Tier != tier)
                continue;
            if (ent.Comp.Discipline != null && tech.Discipline != ent.Comp.Discipline.Value)
                continue;

            foreach (var recipe in tech.RecipeUnlocks)
            {
                bundles.Add((recipe, tech.Discipline));
            }
        }

        if (bundles.Count == 0)
        {
            Log.Error($"Failed to pick recipe for a tech disk: no suitable recipes were found");
            return;
        }

        // pick one
        var bundle = _random.Pick(bundles);
        ent.Comp.Discipline = bundle.discipline;
        ent.Comp.Recipes = [];
        ent.Comp.Recipes.Add(bundle.recipe);
        Dirty(ent);
        _nameModifier.RefreshNameModifiers(ent.Owner);
    }

    /// <summary>
    /// Attempts to set tier and discipline visuals based on chosen tier and discipline.
    /// </summary>
    private void TrySetVisuals(Entity<TechnologyDiskComponent> ent)
    {
        TrySetTierVisuals(ent);
        TrySetDisciplineVisuals(ent);
    }

    /// <summary>
    /// Attempts to set tier visuals based on chosen tier.
    /// </summary>
    private void TrySetTierVisuals(Entity<TechnologyDiskComponent> ent)
    {
        if (ent.Comp.Tier is not { } tier)
            return;

        _appearance.SetData(ent.Owner, TechDiskVisuals.Tier, tier);
    }

    /// <summary>
    /// Attempts to set discipline visuals based on chosen discipline.
    /// </summary>
    private void TrySetDisciplineVisuals(Entity<TechnologyDiskComponent> ent)
    {
        if (!_protoMan.Resolve(ent.Comp.Discipline, out var discipline))
            return;

        _appearance.SetData(ent.Owner, TechDiskVisuals.Discipline, discipline.ID);
    }

    private void OnAfterInteract(Entity<TechnologyDiskComponent> ent, ref AfterInteractEvent args)
    {
        if (args.Handled || !args.CanReach || args.Target is not { } target)
            return;

        if (!HasComp<ResearchServerComponent>(target) || !HasComp<TechnologyDatabaseComponent>(target))
            return;

        if (ent.Comp.Recipes != null)
        {
            foreach (var recipe in ent.Comp.Recipes)
            {
                _research.AddLatheRecipe(target, recipe);
            }
        }
        _popup.PopupClient(Loc.GetString("tech-disk-inserted"), target, args.User);
        PredictedQueueDel(ent.Owner);
        args.Handled = true;
    }

    private void OnExamine(Entity<TechnologyDiskComponent> ent, ref ExaminedEvent args)
    {
        if (ent.Comp is { Tier: not null, Discipline: not null }
            && _protoMan.Resolve(ent.Comp.Discipline, out var disciplineProto))
        {
            var desc = Loc.GetString("tech-disk-examine-desc",
                ("tier", ent.Comp.Tier),
                ("branch", Loc.GetString(disciplineProto.Name))
            );

            args.PushMarkup(desc);
        }
        else
        {
            args.PushMarkup(Loc.GetString("tech-disk-examine-desc-unknown"));
        }

        var message = Loc.GetString("tech-disk-examine-none");
        if (ent.Comp.Recipes != null && ent.Comp.Recipes.Count > 0)
        {
            var prototype = _protoMan.Index(ent.Comp.Recipes[0]);
            message = Loc.GetString("tech-disk-examine", ("result", _lathe.GetRecipeName(prototype)));

            if (ent.Comp.Recipes.Count > 1) //idk how to do this well. sue me.
                message += " " + Loc.GetString("tech-disk-examine-more");
        }
        args.PushMarkup(message);
    }

    private void OnPriceCalculation(Entity<TechnologyDiskComponent> ent, ref PriceCalculationEvent args)
    {
        if (ent.Comp.Tier is not { } tier)
            return;

        if (!ent.Comp.DiskPricePerTier.TryGetValue(tier, out var price))
            return;

        args.Price = price;
        args.Handled = true;
    }

    private void OnRefreshNameModifiers(Entity<TechnologyDiskComponent> entity, ref RefreshNameModifiersEvent args)
    {
        if (entity.Comp.Recipes != null)
        {
            foreach (var recipe in entity.Comp.Recipes)
            {
                var proto = _protoMan.Index(recipe);
                args.AddModifier("tech-disk-name-format", extraArgs: ("technology", _lathe.GetRecipeName(proto)));
            }
        }
    }
}

[Serializable, NetSerializable]
public enum TechDiskVisuals : byte
{
    Tier,
    Discipline
}
