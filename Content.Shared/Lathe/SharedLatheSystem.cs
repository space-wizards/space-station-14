using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Content.Server.Lathe.Components;
using Content.Shared.Administration.Logs;
using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.Database;
using Content.Shared.Emag.Systems;
using Content.Shared.Examine;
using Content.Shared.Fluids;
using Content.Shared.Lathe.Prototypes;
using Content.Shared.Localizations;
using Content.Shared.Materials;
using Content.Shared.Popups;
using Content.Shared.Power;
using Content.Shared.Power.EntitySystems;
using Content.Shared.ReagentSpeed;
using Content.Shared.Research.Components;
using Content.Shared.Research.Prototypes;
using Content.Shared.Stacks;
using Content.Shared.UserInterface;
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
public abstract class SharedLatheSystem : EntitySystem
{
    [Dependency] protected readonly IGameTiming Timing = default!;
    [Dependency] protected readonly IPrototypeManager Proto = default!;
    [Dependency] private readonly ISharedAdminLogManager _adminLogger = default!;
    [Dependency] private readonly EmagSystem _emag = default!;
    [Dependency] private readonly SharedPowerReceiverSystem _power = default!;
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedContainerSystem _container = default!;
    [Dependency] private readonly SharedMaterialStorageSystem _materialStorage = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly SharedPuddleSystem _puddle = default!;
    [Dependency] private readonly ReagentSpeedSystem _reagentSpeed = default!;
    [Dependency] private readonly SharedSolutionContainerSystem _solution = default!;
    [Dependency] private readonly SharedStackSystem _stack = default!;
    [Dependency] private readonly SharedUserInterfaceSystem _uiSys = default!;

    public readonly Dictionary<string, List<LatheRecipePrototype>> InverseRecipes = new();
    public const int MaxItemsPerRequest = 10_000;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<PrototypesReloadedEventArgs>(OnPrototypesReloaded);

        SubscribeLocalEvent<LatheComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<LatheComponent, ExaminedEvent>(OnExamined);
        SubscribeLocalEvent<LatheComponent, PowerChangedEvent>(OnPowerChanged);
        SubscribeLocalEvent<LatheComponent, TechnologyDatabaseModifiedEvent>(OnDatabaseModified);
        SubscribeLocalEvent<LatheComponent, ResearchRegistrationChangedEvent>(OnResearchRegistrationChanged);
        SubscribeLocalEvent<LatheComponent, BeforeActivatableUIOpenEvent>((u, c, _) => UpdateUserInterfaceState((u, c)));
        SubscribeLocalEvent<LatheComponent, MaterialAmountChangedEvent>(OnMaterialAmountChanged);
        SubscribeLocalEvent<LatheComponent, GetMaterialWhitelistEvent>(OnGetWhitelist);

        SubscribeLocalEvent<EmagLatheRecipesComponent, GotEmaggedEvent>(OnEmagged);
        SubscribeLocalEvent<TechnologyDatabaseComponent, LatheGetRecipesEvent>(OnGetRecipes);
        SubscribeLocalEvent<EmagLatheRecipesComponent, LatheGetRecipesEvent>(GetEmagLatheRecipes);

        SubscribeLocalEvent<LatheComponent, LatheQueueRecipeMessage>(OnLatheQueueRecipeMessage);
        SubscribeLocalEvent<LatheComponent, LatheSyncRequestMessage>(OnLatheSyncRequestMessage);
        SubscribeLocalEvent<LatheComponent, LatheDeleteRequestMessage>(OnLatheDeleteRequestMessage);
        SubscribeLocalEvent<LatheComponent, LatheMoveRequestMessage>(OnLatheMoveRequestMessage);
        SubscribeLocalEvent<LatheComponent, LatheAbortFabricationMessage>(OnLatheAbortFabricationMessage);

        BuildInverseRecipeDictionary();
    }

    /// <summary>
    /// Initialize the UI and appearance.
    /// Appearance requires initialization or the layers break
    /// </summary>
    private void OnMapInit(Entity<LatheComponent> entity, ref MapInitEvent args)
    {
        _appearance.SetData(entity, LatheVisuals.IsInserting, false);
        _appearance.SetData(entity, LatheVisuals.IsRunning, false);

        _materialStorage.UpdateMaterialWhitelist(entity);
    }

    private void OnExamined(Entity<LatheComponent> ent, ref ExaminedEvent args)
    {
        if (!args.IsInDetailsRange)
            return;

        if (ent.Comp.ReagentOutputSlotId != null)
            args.PushMarkup(Loc.GetString("lathe-menu-reagent-slot-examine"));
    }

    private void OnEmagged(Entity<EmagLatheRecipesComponent> entity, ref GotEmaggedEvent args)
    {
        if (!_emag.CompareFlag(args.Type, EmagType.Interaction))
            return;

        if (_emag.CheckFlag(entity, EmagType.Interaction))
            return;

        args.Handled = true;
    }

    private void OnPowerChanged(Entity<LatheComponent> entity, ref PowerChangedEvent args)
    {
        if (!args.Powered)
        {
            AbortProduction(entity.AsNullable());
        }
        else
        {
            TryStartProducing(entity.AsNullable());
        }
    }

    private void OnGetWhitelist(Entity<LatheComponent> entity, ref GetMaterialWhitelistEvent args)
    {
        if (args.Storage != entity.Owner)
            return;
        var materialWhitelist = new List<ProtoId<MaterialPrototype>>();
        var recipes = GetAvailableRecipes(entity, true);
        foreach (var id in recipes)
        {
            if (!Proto.TryIndex(id, out var proto))
                continue;
            foreach (var (mat, _) in proto.Materials)
            {
                if (!materialWhitelist.Contains(mat))
                {
                    materialWhitelist.Add(mat);
                }
            }
        }

        var combined = args.Whitelist.Union(materialWhitelist).ToList();
        args.Whitelist = combined;
    }

    /// <summary>
    /// Sets the machine sprite to either play the running animation
    /// or stop.
    /// </summary>
    private void UpdateRunningAppearance(EntityUid uid, bool isRunning)
    {
        _appearance.SetData(uid, LatheVisuals.IsRunning, isRunning);
    }

    public static int AdjustMaterial(int original, bool reduce, float multiplier)
        => reduce ? (int) MathF.Ceiling(original * multiplier) : original;

    #region Production

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
            var adjustedAmount = AdjustMaterial(needed, recipe.ApplyMaterialDiscount, entity.Comp.MaterialUseMultiplier);

            if (_materialStorage.GetMaterialAmount(entity.Owner, material) < adjustedAmount * amount)
                return false;
        }
        return true;
    }

    public bool TryAddToQueue(Entity<LatheComponent?> entity, LatheRecipePrototype recipe, int quantity)
    {
        if (!Resolve(entity, ref entity.Comp))
            return false;

        if (quantity <= 0)
            return false;

        quantity = int.Min(quantity, MaxItemsPerRequest);

        if (!CanProduce(entity, recipe, quantity))
            return false;

        foreach (var (mat, amount) in recipe.Materials)
        {
            var adjustedAmount =
                AdjustMaterial(amount, recipe.ApplyMaterialDiscount, entity.Comp.MaterialUseMultiplier);
            adjustedAmount *= quantity;

            _materialStorage.TryChangeMaterialAmount(entity.Owner, mat, -adjustedAmount);
        }

        if (entity.Comp.Queue.Last is { } node && node.ValueRef.Recipe == recipe.ID)
            node.ValueRef.ItemsRequested += quantity;
        else
            entity.Comp.Queue.AddLast(new LatheRecipeBatch(recipe.ID, 0, quantity));

        return true;
    }

    public bool TryStartProducing(Entity<LatheComponent?> entity)
    {
        if (!Resolve(entity, ref entity.Comp))
            return false;

        if (entity.Comp.CurrentRecipe != null || entity.Comp.Queue.Count <= 0 || !_power.IsPowered(entity.Owner))
            return false;

        var batch = entity.Comp.Queue.First();
        batch.ItemsPrinted++;
        if (batch.ItemsPrinted >= batch.ItemsRequested || batch.ItemsPrinted < 0) // Rollover sanity check
            entity.Comp.Queue.RemoveFirst();

        var recipe = Proto.Index(batch.Recipe);

        var time = _reagentSpeed.ApplySpeed(entity.Owner, recipe.CompleteTime) * entity.Comp.TimeMultiplier;

        var lathe = EnsureComp<LatheProducingComponent>(entity);
        lathe.StartTime = Timing.CurTime;
        lathe.ProductionLength = time;
        entity.Comp.CurrentRecipe = recipe;

        var ev = new LatheStartPrintingEvent(recipe);
        RaiseLocalEvent(entity, ref ev);

        _audio.PlayPvs(entity.Comp.ProducingSound, entity);
        UpdateRunningAppearance(entity, true);
        UpdateUserInterfaceState(entity);

        if (time == TimeSpan.Zero)
        {
            FinishProducing((entity, entity.Comp, lathe));
        }
        return true;
    }

    public void AbortProduction(Entity<LatheComponent?> entity)
    {
        if (!Resolve(entity, ref entity.Comp))
            return;

        if (entity.Comp.CurrentRecipe != null)
        {
            if (entity.Comp.Queue.Count > 0)
            {
                // Batch abandoned while printing last item, need to create a one-item batch
                var batch = entity.Comp.Queue.First();
                if (batch.Recipe != entity.Comp.CurrentRecipe)
                {
                    var newBatch = new LatheRecipeBatch(entity.Comp.CurrentRecipe.Value, 0, 1);
                    entity.Comp.Queue.AddFirst(newBatch);
                }
                else if (batch.ItemsPrinted > 0)
                {
                    batch.ItemsPrinted--;
                }
            }

            entity.Comp.CurrentRecipe = null;
        }
        RemCompDeferred<LatheProducingComponent>(entity);
        UpdateUserInterfaceState(entity);
        UpdateRunningAppearance(entity, false);
    }

    protected void FinishProducing(Entity<LatheComponent, LatheProducingComponent?> entity)
    {
        if (!Resolve(entity, ref entity.Comp2, false))
            return;

        if (entity.Comp1.CurrentRecipe != null)
        {
            var currentRecipe = Proto.Index(entity.Comp1.CurrentRecipe.Value);
            if (currentRecipe.Result is { } resultProto)
            {
                var result = PredictedSpawnNextToOrDrop(resultProto, entity);
                _stack.TryMergeToContacts(result);
            }

            if (currentRecipe.ResultReagents is { } resultReagents &&
                entity.Comp1.ReagentOutputSlotId is { } slotId)
            {
                var toAdd = new Solution(
                    resultReagents.Select(p => new ReagentQuantity(p.Key.Id, p.Value)));

                // dispense it in the container if we have it and dump it if we don't
                if (_container.TryGetContainer(entity, slotId, out var container) &&
                    container.ContainedEntities.Count == 1 &&
                    _solution.TryGetFitsInDispenser(container.ContainedEntities.First(), out var solution, out _))
                {
                    _solution.AddSolution(solution.Value, toAdd);
                }
                else
                {
                    _popup.PopupEntity(Loc.GetString("lathe-reagent-dispense-no-container", ("name", entity)), entity);
                    _puddle.TrySpillAt(entity, toAdd, out _);
                }
            }
        }

        entity.Comp1.CurrentRecipe = null;
        entity.Comp2.StartTime = Timing.CurTime;

        if (!TryStartProducing(entity.AsNullable()))
        {
            RemCompDeferred(entity, entity.Comp2);
            UpdateUserInterfaceState(entity.AsNullable());
            UpdateRunningAppearance(entity, false);
        }
    }

    #endregion

    #region Recipies

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

    private void OnGetRecipes(Entity<TechnologyDatabaseComponent> entity, ref LatheGetRecipesEvent args)
    {
        if (entity.Owner == args.Lathe)
            AddRecipesFromDynamicPacks(ref args, entity.Comp, args.Comp.DynamicPacks);
    }

    private void GetEmagLatheRecipes(Entity<EmagLatheRecipesComponent> entity, ref LatheGetRecipesEvent args)
    {
        if (entity.Owner != args.Lathe)
            return;

        if (!args.GetUnavailable && !_emag.CheckFlag(entity, EmagType.Interaction))
            return;

        AddRecipesFromPacks(args.Recipes, entity.Comp.EmagStaticPacks);

        if (TryComp<TechnologyDatabaseComponent>(entity, out var database))
            AddRecipesFromDynamicPacks(ref args, database, entity.Comp.EmagDynamicPacks);
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

    #endregion

    #region UI

    public void UpdateUserInterfaceState(Entity<LatheComponent?> entity)
    {
        if (!Resolve(entity, ref entity.Comp))
            return;

        var producing = entity.Comp.CurrentRecipe;
        if (producing == null && entity.Comp.Queue.First is { } node)
            producing = node.Value.Recipe;

        var state = new LatheUpdateState(GetAvailableRecipes((entity, entity.Comp)), entity.Comp.Queue.ToArray(), producing);
        _uiSys.SetUiState(entity.Owner, LatheUiKey.Key, state);
    }

    private void OnLatheQueueRecipeMessage(Entity<LatheComponent> entity, ref LatheQueueRecipeMessage args)
    {
        if (Proto.TryIndex(args.ID, out LatheRecipePrototype? recipe))
        {
            if (TryAddToQueue(entity.AsNullable(), recipe, args.Quantity))
            {
                _adminLogger.Add(LogType.Action,
                    LogImpact.Low,
                    $"{ToPrettyString(args.Actor):player} queued {args.Quantity} {GetRecipeName(recipe)} at {ToPrettyString(entity):lathe}");
            }
        }
        TryStartProducing(entity.AsNullable());
        UpdateUserInterfaceState(entity.AsNullable());
    }

    private void OnLatheSyncRequestMessage(Entity<LatheComponent> entity, ref LatheSyncRequestMessage args)
    {
        UpdateUserInterfaceState(entity.AsNullable());
    }

    /// <summary>
    /// Removes a batch from the batch queue by index.
    /// If the index given does not exist or is outside of the bounds of the lathe's batch queue, nothing happens.
    /// </summary>
    /// <param name="entity">The lathe whose queue is being altered.</param>
    /// <param name="args"></param>
    public void OnLatheDeleteRequestMessage(Entity<LatheComponent> entity, ref LatheDeleteRequestMessage args)
    {
        if (args.Index < 0 || args.Index >= entity.Comp.Queue.Count)
            return;

        var node = entity.Comp.Queue.First;
        for (int i = 0; i < args.Index; i++)
        {
            node = node?.Next;
        }

        if (node == null) // Shouldn't happen with checks above.
            return;

        var batch = node.Value;
        _adminLogger.Add(LogType.Action,
            LogImpact.Low,
            $"{ToPrettyString(args.Actor):player} deleted a lathe job for ({batch.ItemsPrinted}/{batch.ItemsRequested}) {GetRecipeName(batch.Recipe)} at {ToPrettyString(entity):lathe}");

        entity.Comp.Queue.Remove(node);
        UpdateUserInterfaceState(entity.AsNullable());
    }

    public void OnLatheMoveRequestMessage(Entity<LatheComponent> entity, ref LatheMoveRequestMessage args)
    {
        if (args.Change == 0 || args.Index < 0 || args.Index >= entity.Comp.Queue.Count)
            return;

        // New index must be within the bounds of the batch.
        var newIndex = args.Index + args.Change;
        if (newIndex < 0 || newIndex >= entity.Comp.Queue.Count)
            return;

        var node = entity.Comp.Queue.First;
        for (int i = 0; i < args.Index; i++)
        {
            node = node?.Next;
        }

        if (node == null) // Something went wrong.
            return;

        if (args.Change > 0)
        {
            var newRelativeNode = node.Next;
            for (int i = 1; i < args.Change; i++) // 1-indexed: starting from Next
            {
                newRelativeNode = newRelativeNode?.Next;
            }

            if (newRelativeNode == null) // Something went wrong.
                return;

            entity.Comp.Queue.Remove(node);
            entity.Comp.Queue.AddAfter(newRelativeNode, node);
        }
        else
        {
            var newRelativeNode = node.Previous;
            for (int i = 1; i < -args.Change; i++) // 1-indexed: starting from Previous
            {
                newRelativeNode = newRelativeNode?.Previous;
            }

            if (newRelativeNode == null) // Something went wrong.
                return;

            entity.Comp.Queue.Remove(node);
            entity.Comp.Queue.AddBefore(newRelativeNode, node);
        }

        UpdateUserInterfaceState(entity.AsNullable());
    }

    public void OnLatheAbortFabricationMessage(Entity<LatheComponent> entity, ref LatheAbortFabricationMessage args)
    {
        if (entity.Comp.CurrentRecipe == null)
            return;

        _adminLogger.Add(LogType.Action,
            LogImpact.Low,
            $"{ToPrettyString(args.Actor):player} aborted printing {GetRecipeName(entity.Comp.CurrentRecipe.Value)} at {ToPrettyString(entity):lathe}");

        entity.Comp.CurrentRecipe = null;
        FinishProducing(entity);
    }
    #endregion

    private void OnMaterialAmountChanged(Entity<LatheComponent> entity, ref MaterialAmountChangedEvent args)
    {
        UpdateUserInterfaceState(entity.AsNullable());
    }

    private void OnDatabaseModified(Entity<LatheComponent> entity, ref TechnologyDatabaseModifiedEvent args)
    {
        UpdateUserInterfaceState(entity.AsNullable());
    }

    private void OnResearchRegistrationChanged(Entity<LatheComponent> entity, ref ResearchRegistrationChangedEvent args)
    {
        UpdateUserInterfaceState(entity.AsNullable());
    }
}
