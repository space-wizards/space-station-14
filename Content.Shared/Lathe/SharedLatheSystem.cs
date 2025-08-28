using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Emag.Systems;
using Content.Shared.Examine;
using Content.Shared.FixedPoint;
using Content.Shared.Lathe.Prototypes;
using Content.Shared.Localizations;
using Content.Shared.Materials;
using Content.Shared.Research.Components;
using Content.Shared.Research.Prototypes;
using Content.Shared.Storage.Components;
using JetBrains.Annotations;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Shared.Lathe;

/// <summary>
/// This handles...
/// </summary>
public abstract class SharedLatheSystem : EntitySystem
{
    [Dependency] protected readonly IPrototypeManager Proto = default!;
    [Dependency] private readonly EmagSystem _emag = default!;
    [Dependency] protected readonly SharedAppearanceSystem Appearance = default!;
    [Dependency] protected readonly SharedMaterialStorageSystem MaterialStorage = default!;
    [Dependency] protected readonly SharedSolutionContainerSystem SolutionContainer = default!;

    public readonly Dictionary<string, List<LatheRecipePrototype>> InverseRecipes = new();
    public const int MaxItemsPerRequest = 10_000;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<LatheComponent, MapInitEvent>(OnMapInit);

        SubscribeLocalEvent<EmagLatheRecipesComponent, GotEmaggedEvent>(OnEmagged);
        SubscribeLocalEvent<LatheComponent, ExaminedEvent>(OnExamined);
        SubscribeLocalEvent<PrototypesReloadedEventArgs>(OnPrototypesReloaded);

        SubscribeLocalEvent<TechnologyDatabaseComponent, LatheGetRecipesEvent>(OnGetRecipes);
        SubscribeLocalEvent<EmagLatheRecipesComponent, LatheGetRecipesEvent>(GetEmagLatheRecipes);

        BuildInverseRecipeDictionary();
    }

    /// <summary>
    /// Initialize the UI and appearance.
    /// Appearance requires initialization or the layers break
    /// </summary>
    private void OnMapInit(Entity<LatheComponent> entity, ref MapInitEvent args)
    {
        Appearance.SetData(entity, LatheVisuals.IsInserting, false);
        Appearance.SetData(entity, LatheVisuals.IsRunning, false);

        // This is here to cause test fails if you forget to define these in yaml.
        if (entity.Comp.InternalSolution != null)
        {
            SolutionContainer.EnsureSolution(entity.Owner, entity.Comp.InternalSolution, out _);
            EnsureComp<DumpableSolutionComponent>(entity, out var dump);
            dump.Solution = entity.Comp.InternalSolution;
        }

        MaterialStorage.UpdateMaterialWhitelist(entity);
    }

    /// <summary>
    /// Get the set of all recipes that a lathe could possibly ever create (e.g., if all techs were unlocked).
    /// </summary>
    public HashSet<ProtoId<LatheRecipePrototype>> GetAllPossibleRecipes(LatheComponent component)
    {
        var recipes = new HashSet<ProtoId<LatheRecipePrototype>>();
        foreach (var pack in component.StaticPacks)
        {
            recipes.UnionWith(Proto.Index(pack).Recipes);
        }

        foreach (var pack in component.DynamicPacks)
        {
            recipes.UnionWith(Proto.Index(pack).Recipes);
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
            var pack = Proto.Index(id);
            recipes.UnionWith(pack.Recipes);
        }
    }

    private void OnExamined(Entity<LatheComponent> ent, ref ExaminedEvent args)
    {
        if (!args.IsInDetailsRange)
            return;

        if (ent.Comp.ReagentOutputSlotId != null)
            args.PushMarkup(Loc.GetString("lathe-menu-reagent-slot-examine"));
    }

    [PublicAPI]
    public bool CanProduce(Entity<LatheComponent?> entity, string recipe, int amount = 1)
    {
        return Proto.TryIndex<LatheRecipePrototype>(recipe, out var proto) && CanProduce(entity, proto, amount);
    }

    public bool CanProduce(Entity<LatheComponent?> entity, LatheRecipePrototype recipe, int amount = 1)
    {
        if (!Resolve(entity, ref entity.Comp))
            return false;
        if (!HasRecipe((entity, entity.Comp), recipe))
            return false;
        if (amount <= 0)
            return false;

        foreach (var (material, needed) in recipe.Materials)
        {
            var adjustedAmount = recipe.ApplyMaterialDiscount ? AdjustMaterial(needed, entity.Comp.MaterialUseMultiplier) : needed;

            if (MaterialStorage.GetMaterialAmount(entity.Owner, material) < adjustedAmount * amount)
                return false;
        }

        // Exit early if we don't need reagents.
        if (recipe.Reagents.Count == 0)
            return true;

        // If we need reagents and can't find any, return.
        if (!SolutionContainer.TryGetSolution(entity.Owner, entity.Comp.InternalSolution, out _, out var solution))
            return false;

        foreach (var (reagent, quantity) in recipe.Reagents)
        {
            var adjustedVolume = recipe.ApplyReagentDiscount ? AdjustReagent(quantity, entity.Comp.ReagentUseMultiplier) : quantity;

            if (solution.GetTotalPrototypeQuantity(reagent.Id) < adjustedVolume)
                return false;
        }

        return true;
    }

    public bool TryAddToQueue(Entity<LatheComponent?> entity, LatheRecipePrototype recipe)
    {
        if (!Resolve(entity, ref entity.Comp))
            return false;

        if (!CanProduce(entity, recipe))
            return false;

        if (recipe.Reagents.Count > 0)
        {
            // If we need reagents and can't find any, return.
            if (!SolutionContainer.TryGetSolution(entity.Owner, entity.Comp.InternalSolution, out var solution))
                return false;

            // Do reagents first since the solution container might not exist if something goes horribly wrong.
            foreach (var (reagent, quantity) in recipe.Reagents)
            {
                var adjustedVolume = recipe.ApplyReagentDiscount ? AdjustReagent(quantity, entity.Comp.ReagentUseMultiplier) : quantity;

                SolutionContainer.RemoveReagent(solution.Value, reagent.Id, adjustedVolume);
            }
        }

        foreach (var (mat, amount) in recipe.Materials)
        {
            var adjustedAmount = recipe.ApplyMaterialDiscount
                ? AdjustMaterial(amount, entity.Comp.MaterialUseMultiplier)
                : amount;

            MaterialStorage.TryChangeMaterialAmount(entity.Owner, mat, -adjustedAmount);
        }

        entity.Comp.Queue.Enqueue(recipe);

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

    public static int AdjustMaterial(int original, float multiplier) => (int) MathF.Ceiling(original * multiplier);

    public static FixedPoint2 AdjustReagent(FixedPoint2 original, float multiplier) => original * multiplier;

    [PublicAPI]
    public bool TryGetAvailableRecipes(Entity<LatheComponent?> entity, [NotNullWhen(true)] out List<ProtoId<LatheRecipePrototype>>? recipes, bool getUnavailable = false)
    {
        recipes = null;
        if (!Resolve(entity, ref entity.Comp))
            return false;

        recipes = GetAvailableRecipes((entity, entity.Comp), getUnavailable);
        return true;
    }

    public List<ProtoId<LatheRecipePrototype>> GetAvailableRecipes(Entity<LatheComponent> entity, bool getUnavailable = false)
    {
        var ev = new LatheGetRecipesEvent(entity, getUnavailable);
        AddRecipesFromPacks(ev.Recipes, entity.Comp.StaticPacks);
        RaiseLocalEvent(entity, ev);
        return ev.Recipes.ToList();
    }

    /// <summary>
    /// Adds every unlocked recipe from each pack to the recipes list.
    /// </summary>
    public void AddRecipesFromDynamicPacks(ref LatheGetRecipesEvent args, TechnologyDatabaseComponent database, IEnumerable<ProtoId<LatheRecipePackPrototype>> packs)
    {
        foreach (var id in packs)
        {
            var pack = Proto.Index(id);
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

    protected bool HasRecipe(Entity<LatheComponent> entity, LatheRecipePrototype recipe)
    {
        return GetAvailableRecipes(entity).Contains(recipe.ID);
    }

    private void OnPrototypesReloaded(PrototypesReloadedEventArgs obj)
    {
        if (!obj.WasModified<LatheRecipePrototype>())
            return;
        BuildInverseRecipeDictionary();
    }

    private void BuildInverseRecipeDictionary()
    {
        InverseRecipes.Clear();
        foreach (var latheRecipe in Proto.EnumeratePrototypes<LatheRecipePrototype>())
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
        return GetRecipeName(Proto.Index(proto));
    }

    public string GetRecipeName(LatheRecipePrototype proto)
    {
        if (!string.IsNullOrWhiteSpace(proto.Name))
            return Loc.GetString(proto.Name);

        if (proto.Result is {} result)
        {
            return Proto.Index(result).Name;
        }

        if (proto.ResultReagents is { } resultReagents)
        {
            return ContentLocalizationManager.FormatList(resultReagents
                .Select(p => Loc.GetString("lathe-menu-result-reagent-display", ("reagent", Proto.Index(p.Key).LocalizedName), ("amount", p.Value)))
                .ToList());
        }

        return string.Empty;
    }

    [PublicAPI]
    public string GetRecipeDescription(ProtoId<LatheRecipePrototype> proto)
    {
        return GetRecipeDescription(Proto.Index(proto));
    }

    public string GetRecipeDescription(LatheRecipePrototype proto)
    {
        if (!string.IsNullOrWhiteSpace(proto.Description))
            return Loc.GetString(proto.Description);

        if (proto.Result is {} result)
        {
            return Proto.Index(result).Description;
        }

        if (proto.ResultReagents is { } resultReagents)
        {
            // We only use the first one for the description since these descriptions don't combine very well.
            var reagent = resultReagents.First().Key;
            return Proto.Index(reagent).LocalizedDescription;
        }

        return string.Empty;
    }
}
