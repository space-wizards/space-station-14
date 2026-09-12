using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.Emag.Systems;
using Content.Shared.Examine;
using Content.Shared.Fluids;
using Content.Shared.Lathe.Prototypes;
using Content.Shared.Localizations;
using Content.Shared.Materials;
using Content.Shared.Popups;
using Content.Shared.ReagentSpeed;
using Content.Shared.Research.Prototypes;
using Content.Shared.Research.Components;
using Content.Shared.Stacks;
using JetBrains.Annotations;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Containers;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;
using Robust.Shared.Utility;

namespace Content.Shared.Lathe;

/// <summary>
/// This handles...
/// </summary>
public abstract partial class SharedLatheSystem : EntitySystem
{
    [Dependency] protected IGameTiming Timing = default!;
    [Dependency] protected SharedAppearanceSystem Appearance = default!;
    [Dependency] private SharedAudioSystem _audio = default!;
    [Dependency] private SharedContainerSystem _container = default!;
    [Dependency] private EmagSystem _emag = default!;
    [Dependency] private SharedMaterialStorageSystem _materialStorage = default!;
    [Dependency] private SharedPopupSystem _popup = default!;
    [Dependency] private SharedPuddleSystem _puddle = default!;
    [Dependency] private ReagentSpeedSystem _reagentSpeed = default!;
    [Dependency] private SharedSolutionContainerSystem _solution = default!;
    [Dependency] private SharedStackSystem _stack = default!;

    public readonly Dictionary<string, List<LatheRecipePrototype>> InverseRecipes = new();
    public const int MaxItemsPerRequest = 10_000;

    public override void Initialize()
    {
        base.Initialize();

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

        recipes = GetAvailableRecipes(uid, component, getUnavailable).ToList();
        return true;
    }

    [PublicAPI]
    public IEnumerable<ProtoId<LatheRecipePrototype>> GetAvailableRecipes(EntityUid uid, LatheComponent component, bool getUnavailable = false)
    {
        var ev = new LatheGetRecipesEvent((uid, component), getUnavailable);
        AddRecipesFromPacks(ev.Recipes, component.StaticPacks);
        RaiseLocalEvent(uid, ev);
        return ev.Recipes;
    }

    /// <summary>
    /// Adds every unlocked recipe from each pack to the recipes list.
    /// </summary>
    [PublicAPI]
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

    [SubscribeLocalEvent]
    private void OnGetRecipes(EntityUid uid, TechnologyDatabaseComponent component, LatheGetRecipesEvent args)
    {
        if (uid == args.Lathe)
            AddRecipesFromDynamicPacks(ref args, component, args.Comp.DynamicPacks);
    }

    [SubscribeLocalEvent]
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

    [SubscribeLocalEvent]
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

    [SubscribeLocalEvent]
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

    [SubscribeLocalEvent]
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

    public bool TryAddToQueue(Entity<LatheComponent> uid, LatheRecipePrototype recipe, int quantity)
    {
        if (quantity <= 0)
            return false;
        quantity = int.Min(quantity, MaxItemsPerRequest);

        if (!CanProduce(uid, recipe, quantity, uid.Comp))
            return false;

        foreach (var (mat, amount) in GetAdjustedAmount(uid.Comp, recipe))
            _materialStorage.TryChangeMaterialAmount(uid, mat, -amount * quantity);

        if (uid.Comp.Queue.Last is { } node && node.ValueRef.Recipe == recipe.ID)
            node.ValueRef.ItemsRequested += quantity;
        else
            uid.Comp.Queue.AddLast(new LatheRecipeBatch(recipe.ID, 0, quantity));
        DirtyField(uid, uid.Comp, nameof(LatheComponent.Queue));

        return true;
    }

    protected virtual void LogRecipeQueueAddition(Entity<LatheComponent> uid, ref LatheQueueRecipeMessage args, LatheRecipePrototype recipe)
    {
    }

    public bool TryStartProducing(Entity<LatheComponent> uid, EntityUid? actor)
    {
        if (uid.Comp.CurrentRecipe != null || uid.Comp.Queue.Count <= 0 || !IsPowered(uid))
            return false;

        var batch = uid.Comp.Queue.First();
        batch.ItemsPrinted++;
        if (batch.ItemsPrinted >= batch.ItemsRequested || batch.ItemsPrinted < 0) // Rollover sanity check
            uid.Comp.Queue.RemoveFirst();
        var recipe = ProtoMan.Index(batch.Recipe);

        var time = _reagentSpeed.ApplySpeed(uid.Owner, recipe.CompleteTime) * uid.Comp.TimeMultiplier;

        var lathe = EnsureComp<LatheProducingComponent>(uid);
        lathe.StartTime = Timing.CurTime;
        lathe.ProductionLength = time;
        uid.Comp.CurrentRecipe = recipe;

        var ev = new LatheStartPrintingEvent(recipe);
        RaiseLocalEvent(uid, ref ev);

        _audio.PlayPredicted(uid.Comp.ProducingSound, uid, actor);
        UpdateRunningAppearance(uid, true);
        DirtyField(uid, uid.Comp, nameof(LatheComponent.Queue));
        DirtyField(uid, uid.Comp, nameof(LatheComponent.CurrentRecipe));

        if (time == TimeSpan.Zero)
        {
            FinishProducing(uid);
        }
        return true;
    }

    protected abstract bool IsPowered(EntityUid ent);

    public void FinishProducing(Entity<LatheComponent> uid, LatheProducingComponent? prodComp = null)
    {
        if (!Resolve(uid, ref prodComp, false))
            return;

        if (uid.Comp.CurrentRecipe != null)
        {
            var currentRecipe = ProtoMan.Index(uid.Comp.CurrentRecipe.Value);
            if (currentRecipe.Result is { } resultProto)
            {
                var result = Spawn(resultProto, Transform(uid).Coordinates);
                _stack.TryMergeToContacts(result);
            }

            if (currentRecipe.ResultReagents is { } resultReagents &&
                    uid.Comp.ReagentOutputSlotId is { } slotId)
            {
                var toAdd = new Solution(
                        resultReagents.Select(p => new ReagentQuantity(p.Key.Id, p.Value, null)));

                // dispense it in the container if we have it and dump it if we don't
                if (_container.TryGetContainer(uid, slotId, out var container) &&
                        container.ContainedEntities.Count == 1 &&
                        _solution.TryGetFitsInDispenser(container.ContainedEntities.First(), out var solution, out _))
                {
                    _solution.AddSolution(solution.Value, toAdd);
                }
                else
                {
                    _popup.PopupEntity(Loc.GetString("lathe-reagent-dispense-no-container", ("name", uid)), uid);
                    _puddle.TrySpillAt(uid, toAdd, out _);
                }
            }
        }

        uid.Comp.CurrentRecipe = null;
        prodComp.StartTime = Timing.CurTime;
        DirtyField(uid, uid.Comp, nameof(LatheComponent.CurrentRecipe));

        if (!TryStartProducing(uid, null))
        {
            RemCompDeferred(uid, prodComp);
            UpdateRunningAppearance(uid, false);
        }
    }

    /// <summary>
    /// Sets the machine sprite to either play the running animation
    /// or stop.
    /// </summary>
    protected void UpdateRunningAppearance(EntityUid uid, bool isRunning)
    {
        Appearance.SetData(uid, LatheVisuals.IsRunning, isRunning);
    }

    /// <summary>
    /// Iterator returning adjusted amount of material needed to
    /// produce a given recipe
    /// </summary>
    protected static IEnumerable<(ProtoId<MaterialPrototype> mat, int amount)> GetAdjustedAmount(LatheComponent lathe, LatheRecipePrototype recipe)
    {
        foreach (var (mat, amount) in recipe.Materials)
        {
            var adjustedAmount = recipe.ApplyMaterialDiscount
                ? (int)(amount * lathe.MaterialUseMultiplier)
                : amount;

            yield return (mat, adjustedAmount);
        }
    }

    /// <summary>
    /// Refunds the material cost of a given batch,
    /// without deleting it
    /// </summary>
    private void RefundBatch(Entity<LatheComponent> uid, LatheRecipeBatch batch)
    {
        var delta = batch.ItemsRequested - batch.ItemsPrinted;

        ProtoMan.Resolve(batch.Recipe, out var recipe);

        foreach (var (mat, amount) in GetAdjustedAmount(uid.Comp, recipe!))
            _materialStorage.TryChangeMaterialAmount(uid, mat, amount * delta);
    }

    protected virtual void DeleteQueueEntryImpl(Entity<LatheComponent> uid, LatheDeleteRequestMessage args, LinkedListNode<LatheRecipeBatch> entry)
    {
        RefundBatch(uid, entry.Value);
        uid.Comp.Queue.Remove(entry);
        DirtyField(uid, uid.Comp, nameof(LatheComponent.Queue));
    }

    #region UI Messages

    [SubscribeLocalEvent]
    private void OnLatheQueueRecipeMessage(Entity<LatheComponent> uid, ref LatheQueueRecipeMessage args)
    {
        if (ProtoMan.TryIndex(args.ID, out LatheRecipePrototype? recipe))
        {
            if (TryAddToQueue(uid, recipe, args.Quantity))
            {
                LogRecipeQueueAddition(uid, ref args, recipe);
            }
        }
        TryStartProducing(uid, args.Actor);
    }

    /// <summary>
    /// Removes a batch from the batch queue by index.
    /// If the index given does not exist or is outside of the bounds of the lathe's batch queue, nothing happens.
    /// </summary>
    /// <param name="uid">The lathe whose queue is being altered.</param>
    /// <param name="component"></param>
    /// <param name="args"></param>
    [SubscribeLocalEvent]
    public void OnLatheDeleteRequestMessage(Entity<LatheComponent> uid, ref LatheDeleteRequestMessage args)
    {
        if (args.Index < 0 || args.Index >= uid.Comp.Queue.Count)
            return;

        var node = uid.Comp.Queue.First;
        for (var i = 0; i < args.Index; i++)
            node = node?.Next;

        if (node == null) // Shouldn't happen with checks above.
            return;

        DeleteQueueEntryImpl(uid, args, node);
    }

    #endregion
}
