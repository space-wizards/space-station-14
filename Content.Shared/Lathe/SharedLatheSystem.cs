using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Content.Shared.Emag.Systems;
using Content.Shared.Examine;
using Content.Shared.Lathe.Prototypes;
using Content.Shared.Localizations;
using Content.Shared.Materials;
using Content.Shared.Research.Prototypes;
using Content.Shared.Research.Components;
using JetBrains.Annotations;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Shared.Lathe;

/// <summary>
/// This handles...
/// </summary>
public abstract partial class SharedLatheSystem : EntitySystem
{
    [Dependency] private SharedMaterialStorageSystem _materialStorage = default!;
    [Dependency] private EmagSystem _emag = default!;

    public readonly Dictionary<string, List<LatheRecipePrototype>> InverseRecipes = new();
    public const int MaxItemsPerRequest = 10_000;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<EmagLatheRecipesComponent, GotEmaggedEvent>(OnEmagged);
        SubscribeLocalEvent<LatheComponent, ExaminedEvent>(OnExamined);
        SubscribeLocalEvent<PrototypesReloadedEventArgs>(OnPrototypesReloaded);
        SubscribeLocalEvent<TechnologyDatabaseComponent, LatheGetRecipesEvent>(OnGetRecipes);
        SubscribeLocalEvent<EmagLatheRecipesComponent, LatheGetRecipesEvent>(GetEmagLatheRecipes);

        BuildInverseRecipeDictionary();
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

    /// <summary>
    /// Add every recipe in the list of recipe packs to a single hashset.
    /// </summary>
    public void AddRecipesFromPacks(HashSet<ProtoId<LatheRecipePrototype>> recipes, IEnumerable<ProtoId<LatheRecipePackPrototype>> packs)
    {
        foreach (var id in packs)
        {
            var pack = ProtoMan.Index(id);
            recipes.UnionWith(pack.Recipes);
        }
    }

    [PublicAPI]
    public bool TryGetAvailableRecipes(EntityUid uid, [NotNullWhen(true)] out List<ProtoId<LatheRecipePrototype>>? recipes, [NotNullWhen(true)] LatheComponent? component = null, bool getUnavailable = false)
    {
        recipes = null;
        if (!Resolve(uid, ref component))
            return false;
        recipes = GetAvailableRecipes(uid, component, getUnavailable);
        return true;
    }

    public List<ProtoId<LatheRecipePrototype>> GetAvailableRecipes(EntityUid uid, LatheComponent component, bool getUnavailable = false)
    {
        var ev = new LatheGetRecipesEvent((uid, component), getUnavailable);
        AddRecipesFromPacks(ev.Recipes, component.StaticPacks);
        RaiseLocalEvent(uid, ev);
        return ev.Recipes.ToList();
    }

    /// <summary>
    /// Adds every unlocked recipe from each pack to the recipes list.
    /// </summary>
    public void AddRecipesFromDynamicPacks(ref LatheGetRecipesEvent args, TechnologyDatabaseComponent database, IEnumerable<ProtoId<LatheRecipePackPrototype>> packs)
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

    private void OnGetRecipes(EntityUid uid, TechnologyDatabaseComponent component, LatheGetRecipesEvent args)
    {
        if (uid == args.Lathe)
            AddRecipesFromDynamicPacks(ref args, component, args.Comp.DynamicPacks);
    }

    private void GetEmagLatheRecipes(EntityUid uid, EmagLatheRecipesComponent component, LatheGetRecipesEvent args)
    {
        if (uid != args.Lathe)
            return;

        if (!args.GetUnavailable && !_emag.CheckFlag(uid, EmagType.Interaction))
            return;

        AddRecipesFromPacks(args.Recipes, component.EmagStaticPacks);

        if (TryComp<TechnologyDatabaseComponent>(uid, out var database))
            AddRecipesFromDynamicPacks(ref args, database, component.EmagDynamicPacks);
    }

    private void OnExamined(Entity<LatheComponent> ent, ref ExaminedEvent args)
    {
        if (!args.IsInDetailsRange)
            return;

        if (ent.Comp.ReagentOutputSlotId != null)
            args.PushMarkup(Loc.GetString("lathe-menu-reagent-slot-examine"));
    }

    [PublicAPI]
    public bool CanProduce(EntityUid uid, string recipe, int amount = 1, LatheComponent? component = null)
    {
        return ProtoMan.TryIndex<LatheRecipePrototype>(recipe, out var proto) && CanProduce(uid, proto, amount, component);
    }

    public bool CanProduce(EntityUid uid, LatheRecipePrototype recipe, int amount = 1, LatheComponent? component = null)
    {
        if (!Resolve(uid, ref component))
            return false;
        if (!HasRecipe(uid, recipe, component))
            return false;
        if (amount <= 0)
            return false;

        foreach (var (material, needed) in recipe.Materials)
        {
            var adjustedAmount = AdjustMaterial(needed, recipe.ApplyMaterialDiscount, component.MaterialUseMultiplier);

            if (_materialStorage.GetMaterialAmount(uid, material) < adjustedAmount * amount)
                return false;
        }
        return true;
    }

    private void OnEmagged(EntityUid uid, EmagLatheRecipesComponent component, ref GotEmaggedEvent args)
    {
        if (!_emag.CompareFlag(args.Type, EmagType.Interaction))
            return;

        if (_emag.CheckFlag(uid, EmagType.Interaction))
            return;

        args.Handled = true;
    }

    public static int AdjustMaterial(int original, bool reduce, float multiplier)
        => reduce ? (int) MathF.Ceiling(original * multiplier) : original;

    protected abstract bool HasRecipe(EntityUid uid, LatheRecipePrototype recipe, LatheComponent component);

    private void OnPrototypesReloaded(PrototypesReloadedEventArgs obj)
    {
        if (!obj.WasModified<LatheRecipePrototype>())
            return;
        BuildInverseRecipeDictionary();
    }

    private void BuildInverseRecipeDictionary()
    {
        InverseRecipes.Clear();
        foreach (var latheRecipe in ProtoMan.EnumeratePrototypes<LatheRecipePrototype>())
        {
            if (latheRecipe.Result is not {} result)
                continue;

            InverseRecipes.GetOrNew(result).Add(latheRecipe);
        }
    }

    public bool TryGetRecipesFromEntity(string prototype, [NotNullWhen(true)] out List<LatheRecipePrototype>? recipes)
    {
        recipes = new();
        if (InverseRecipes.TryGetValue(prototype, out var r))
            recipes.AddRange(r);
        return recipes.Count != 0;
    }

    public string GetRecipeName(ProtoId<LatheRecipePrototype> proto)
    {
        return GetRecipeName(ProtoMan.Index(proto));
    }

    public string GetRecipeName(LatheRecipePrototype proto)
    {
        if (!string.IsNullOrWhiteSpace(proto.Name))
            return Loc.GetString(proto.Name);

        if (proto.Result is {} result)
        {
            return ProtoMan.Index(result).Name;
        }

        if (proto.ResultReagents is { } resultReagents)
        {
            return ContentLocalizationManager.FormatList(resultReagents
                .Select(p => Loc.GetString("lathe-menu-result-reagent-display", ("reagent", ProtoMan.Index(p.Key).LocalizedName), ("amount", p.Value)))
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

        if (proto.Result is {} result)
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
}
