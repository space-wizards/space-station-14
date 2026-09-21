using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Content.Shared.Emag.Systems;
using Content.Shared.Lathe.Components;
using Content.Shared.Lathe.Prototypes;
using Content.Shared.Localizations;
using Content.Shared.Research.Components;
using Content.Shared.Research.Prototypes;
using JetBrains.Annotations;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Shared.Lathe;

public abstract partial class LatheSystem
{
    /// <summary>
    /// A dictionary of lathe entity prototypes and their associated lathe recipes.
    /// </summary>
    public readonly Dictionary<EntProtoId, List<LatheRecipePrototype>> InverseRecipes = new();

    [SubscribeLocalEvent]
    private void OnPrototypesReloaded(PrototypesReloadedEventArgs args)
    {
        if (!args.WasModified<LatheRecipePrototype>())
            return;

        BuildInverseRecipeDictionary();
    }

    private void BuildInverseRecipeDictionary()
    {
        InverseRecipes.Clear();
        foreach (var latheRecipe in ProtoMan.EnumeratePrototypes<LatheRecipePrototype>())
        {
            if (latheRecipe.Result is not { } result)
                continue;

            InverseRecipes.GetOrNew(result).Add(latheRecipe);
        }
    }

    [SubscribeLocalEvent]
    private void OnEmagged(Entity<EmagLatheRecipesComponent> entity, ref GotEmaggedEvent args)
    {
        if (!_emag.CompareFlag(args.Type, EmagType.Interaction))
            return;

        if (_emag.CheckFlag(entity, EmagType.Interaction))
            return;

        args.Handled = true;
    }

    [SubscribeLocalEvent]
    private void OnGetRecipes(Entity<TechnologyDatabaseComponent> entity, ref LatheGetRecipesEvent args)
    {
        if (entity.Owner == args.Lathe.Owner)
            AddRecipesFromDynamicPacks(ref args, entity.Comp, args.Lathe.Comp.DynamicPacks);
    }

    [SubscribeLocalEvent]
    private void GetEmagLatheRecipes(Entity<EmagLatheRecipesComponent> entity, ref LatheGetRecipesEvent args)
    {
        if (entity.Owner != args.Lathe.Owner)
            return;

        if (!args.GetUnavailable && !_emag.CheckFlag(entity, EmagType.Interaction))
            return;

        AddRecipesFromPacks(args.Recipes, entity.Comp.EmagStaticPacks);

        if (TryComp<TechnologyDatabaseComponent>(entity, out var database))
            AddRecipesFromDynamicPacks(ref args, database, entity.Comp.EmagDynamicPacks);
    }

    # region API

    public bool TryGetRecipesFromEntity(string prototype, [NotNullWhen(true)] out List<LatheRecipePrototype>? recipes)
    {
        recipes = new();
        if (InverseRecipes.TryGetValue(prototype, out var r))
            recipes.AddRange(r);
        return recipes.Count != 0;
    }

    /// <summary>
    /// Get the set of all recipes that a lathe could possibly ever create (e.g., if all techs were unlocked).
    /// </summary>
    public HashSet<ProtoId<LatheRecipePrototype>> GetAllPossibleRecipes(LatheComponent component)
    {
        var recipes = new HashSet<ProtoId<LatheRecipePrototype>>();
        foreach (var pack in component.StaticPacks)
        {
            recipes.UnionWith(ProtoMan.Index(pack).Recipes);
        }

        foreach (var pack in component.DynamicPacks)
        {
            recipes.UnionWith(ProtoMan.Index(pack).Recipes);
        }

        return recipes;
    }

    public void UpdateRecipies(Entity<LatheComponent> entity)
    {
        entity.Comp.Recipes = GetAvailableRecipes(entity);
        Dirty(entity.AsNullable());
    }

    /// <summary>
    /// Add every recipe in the list of recipe packs to a single hashset.
    /// </summary>
    public void AddRecipesFromPacks(HashSet<ProtoId<LatheRecipePrototype>> recipes,
        IEnumerable<ProtoId<LatheRecipePackPrototype>> packs)
    {
        foreach (var id in packs)
        {
            var pack = ProtoMan.Index(id);
            recipes.UnionWith(pack.Recipes);
        }
    }

    [PublicAPI]
    public bool TryGetAvailableRecipes(Entity<LatheComponent?> entity,
        [NotNullWhen(true)] out List<ProtoId<LatheRecipePrototype>>? recipes,
        bool getUnavailable = false)
    {
        recipes = null;
        if (!Resolve(entity, ref entity.Comp))
            return false;

        recipes = GetAvailableRecipes((entity, entity.Comp), getUnavailable);
        return true;
    }

    public List<ProtoId<LatheRecipePrototype>> GetAvailableRecipes(Entity<LatheComponent> entity,
        bool getUnavailable = false)
    {
        var ev = new LatheGetRecipesEvent(entity, getUnavailable);
        AddRecipesFromPacks(ev.Recipes, entity.Comp.StaticPacks);
        RaiseLocalEvent(entity, ref ev);
        return ev.Recipes.ToList();
    }

    /// <summary>
    /// Adds every unlocked recipe from each pack to the recipes list.
    /// </summary>
    public void AddRecipesFromDynamicPacks(ref LatheGetRecipesEvent args,
        TechnologyDatabaseComponent database,
        IEnumerable<ProtoId<LatheRecipePackPrototype>> packs)
    {
        foreach (var id in packs)
        {
            var pack = ProtoMan.Index(id);
            foreach (var recipe in pack.Recipes)
            {
                if (args.GetUnavailable || database.UnlockedRecipes.Contains(recipe))
                    args.Recipes.Add(recipe);
            }
        }
    }

    protected bool HasRecipe(Entity<LatheComponent> entity, LatheRecipePrototype recipe)
    {
        return GetAvailableRecipes(entity).Contains(recipe.ID);
    }

    public string GetRecipeName(ProtoId<LatheRecipePrototype> proto)
    {
        return GetRecipeName(ProtoMan.Index(proto));
    }

    public string GetRecipeName(LatheRecipePrototype proto)
    {
        if (!string.IsNullOrWhiteSpace(proto.Name))
            return Loc.GetString(proto.Name);

        if (proto.Result is { } result)
        {
            return ProtoMan.Index(result).Name;
        }

        if (proto.ResultReagents is { } resultReagents)
        {
            return ContentLocalizationManager.FormatList(resultReagents
                .Select(p => Loc.GetString("lathe-menu-result-reagent-display",
                    ("reagent", ProtoMan.Index(p.Key).LocalizedName),
                    ("amount", p.Value)))
                .ToList());
        }

        return string.Empty;
    }

    [PublicAPI]
    public string GetRecipeDescription(ProtoId<LatheRecipePrototype> proto)
    {
        return GetRecipeDescription(ProtoMan.Index(proto));
    }

    public string GetRecipeDescription(LatheRecipePrototype proto)
    {
        if (!string.IsNullOrWhiteSpace(proto.Description))
            return Loc.GetString(proto.Description);

        if (proto.Result is { } result)
        {
            return ProtoMan.Index(result).Description;
        }

        if (proto.ResultReagents is { } resultReagents)
        {
            // We only use the first one for the description since these descriptions don't combine very well.
            var reagent = resultReagents.First().Key;
            return ProtoMan.Index(reagent).LocalizedDescription;
        }

        return string.Empty;
    }

    #endregion
}

/// <summary>
/// Raises an event on the lathe which acquires a hashset of all recipes available to this lathe.
/// </summary>
/// <param name="Lathe">Lathe that desires its recipes</param>
/// <param name="GetUnavailable">Whether we should also add unavailable recipes to the set</param>
[ByRefEvent]
public record struct LatheGetRecipesEvent(Entity<LatheComponent> Lathe, bool GetUnavailable)
{
    public readonly Entity<LatheComponent> Lathe = Lathe;

    public readonly bool GetUnavailable = GetUnavailable;

    public HashSet<ProtoId<LatheRecipePrototype>> Recipes = new();
}
