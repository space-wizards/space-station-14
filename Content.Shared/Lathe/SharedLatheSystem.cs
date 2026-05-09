using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Content.Shared.Administration.Logs;
using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.Database;
using Content.Shared.Emag.Systems;
using Content.Shared.Examine;
using Content.Shared.Fluids;
using Content.Shared.Lathe.Components;
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
using Robust.Shared.GameStates;
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
    [Dependency] protected IPrototypeManager Proto = default!;
    [Dependency] private ISharedAdminLogManager _adminLogger = default!;
    [Dependency] private EmagSystem _emag = default!;
    [Dependency] private SharedPowerReceiverSystem _power = default!;
    [Dependency] private SharedAppearanceSystem _appearance = default!;
    [Dependency] private SharedAudioSystem _audio = default!;
    [Dependency] private SharedContainerSystem _container = default!;
    [Dependency] private SharedMaterialStorageSystem _materialStorage = default!;
    [Dependency] private SharedPopupSystem _popup = default!;
    [Dependency] private SharedPuddleSystem _puddle = default!;
    [Dependency] private ReagentSpeedSystem _reagentSpeed = default!;
    [Dependency] private SharedPowerStateSystem _powerState = default!;
    [Dependency] private SharedSolutionContainerSystem _solution = default!;
    [Dependency] private SharedStackSystem _stack = default!;
    [Dependency] protected SharedUserInterfaceSystem UISys = default!;

    [Dependency] protected EntityQuery<LatheComponent> LatheQuery = default!;
    [Dependency] protected EntityQuery<LatheProducingComponent> ProducingQuery = default!;

    public readonly Dictionary<string, List<LatheRecipePrototype>> InverseRecipes = new();
    public const int MaxItemsPerRequest = 10_000;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<PrototypesReloadedEventArgs>(OnPrototypesReloaded);

        SubscribeLocalEvent<LatheComponent, ComponentGetState>(OnGetState);

        SubscribeLocalEvent<LatheComponent, ComponentInit>(OnComponentInit);
        SubscribeLocalEvent<LatheComponent, ExaminedEvent>(OnExamined);
        SubscribeLocalEvent<LatheComponent, PowerChangedEvent>(OnPowerChanged);
        SubscribeLocalEvent<LatheComponent, TechnologyDatabaseModifiedEvent>(OnDatabaseModified);
        SubscribeLocalEvent<LatheComponent, ResearchRegistrationChangedEvent>(OnResearchRegistrationChanged);
        SubscribeLocalEvent<LatheComponent, BeforeActivatableUIOpenEvent>((u, c, _) => UpdateUI((u, c)));
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

        SubscribeLocalEvent<LatheProducingComponent, MapInitEvent>(OnProductionStartup);
        SubscribeLocalEvent<LatheProducingComponent, ComponentShutdown>(OnProductionShutdown);

        BuildInverseRecipeDictionary();
    }

    private void OnGetState(Entity<LatheComponent> entity, ref ComponentGetState args)
    {
        args.State = new LatheComponentState(entity.Comp.Recipes, entity.Comp.Queue, entity.Comp.CurrentRecipe);
    }

    public override void Update(float frameTime)
    {
        var query = EntityQueryEnumerator<LatheProducingComponent, LatheComponent>();
        while (query.MoveNext(out var uid, out var comp, out var lathe))
        {
            if (Timing.CurTime - comp.StartTime >= comp.ProductionLength)
                FinishProducing((uid, lathe));
        }
    }

    /// <summary>
    /// Initialize the UI and appearance.
    /// Appearance requires initialization or the layers break
    /// </summary>
    private void OnComponentInit(Entity<LatheComponent> entity, ref ComponentInit args)
    {
        _appearance.SetData(entity, LatheVisuals.IsInserting, false);
        _appearance.SetData(entity, LatheVisuals.IsRunning, false);

        _materialStorage.UpdateMaterialWhitelist(entity);
        UpdateRecipies(entity);
    }

    private void OnProductionStartup(Entity<LatheProducingComponent> ent, ref MapInitEvent args)
    {
        _powerState.TrySetWorkingState(ent.Owner, true);
        UpdateRunningAppearance(ent, true);

        if (!LatheQuery.TryComp(ent, out var lathe))
            return;

        UpdateUI((ent, lathe));
    }

    private void OnProductionShutdown(Entity<LatheProducingComponent> ent, ref ComponentShutdown args)
    {
        // use the Try variant of this here
        // or else you get trolled by AllComponentsOneToOneDeleteTest
        _powerState.TrySetWorkingState(ent.Owner, false);
        UpdateRunningAppearance(ent, false);

        if (!LatheQuery.TryComp(ent, out var lathe))
            return;

        lathe.CurrentRecipe = null;
        UpdateUI((ent, lathe));
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
            if (!Proto.Resolve(id, out var proto))
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
        => reduce ? (int)MathF.Ceiling(original * multiplier) : original;

    #region Production

    [PublicAPI]
    public bool CanProduce(Entity<LatheComponent?> entity, string recipe, int amount = 1)
    {
        return Proto.Resolve<LatheRecipePrototype>(recipe, out var proto) && CanProduce(entity, proto, amount);
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
            var adjustedAmount =
                AdjustMaterial(needed, recipe.ApplyMaterialDiscount, entity.Comp.MaterialUseMultiplier);

            if (_materialStorage.GetMaterialAmount(entity.Owner, material) < adjustedAmount * amount)
                return false;
        }

        return true;
    }

    /// <summary>
    /// Iterator returning adjusted amount of material needed to
    /// produce a given recipe
    /// </summary>
    private static IEnumerable<(ProtoId<MaterialPrototype> mat, int amount)> GetAdjustedAmount(Entity<LatheComponent> lathe, LatheRecipePrototype recipe)
    {
        foreach (var (mat, amount) in recipe.Materials)
        {
            var adjustedAmount = recipe.ApplyMaterialDiscount
                ? (int)(amount * lathe.Comp.MaterialUseMultiplier)
                : amount;

            yield return (mat, adjustedAmount);
        }
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

        foreach (var (mat, amount) in GetAdjustedAmount((entity, entity.Comp), recipe))
        {
            _materialStorage.TryChangeMaterialAmount(entity.Owner, mat, -amount * quantity);
        }

        if (entity.Comp.Queue.Last is { } node && node.ValueRef.Recipe == recipe.ID)
            node.ValueRef.ItemsRequested += quantity;
        else
            entity.Comp.Queue.AddLast(new LatheRecipeBatch(recipe.ID, 0, quantity));

        Dirty(entity);
        return true;
    }

    public bool TryStartProducing(Entity<LatheComponent?> entity)
    {
        if (!Resolve(entity, ref entity.Comp))
            return false;

        return entity.Comp.CurrentRecipe == null && TryProcessQueue((entity, entity.Comp));
    }

    private bool TryProcessQueue(Entity<LatheComponent> entity)
    {
        if (!_power.IsPowered(entity.Owner) || !entity.Comp.Queue.TryFirstOrDefault(out var batch))
            return false;

        batch.ItemsPrinted++;
        if (batch.ItemsPrinted >= batch.ItemsRequested || batch.ItemsPrinted < 0) // Rollover sanity check
            entity.Comp.Queue.RemoveFirst();

        var recipe = Proto.Index(batch.Recipe);

        var time = _reagentSpeed.ApplySpeed(entity.Owner, recipe.CompleteTime) * entity.Comp.TimeMultiplier;

        var lathe = EnsureComp<LatheProducingComponent>(entity);
        lathe.StartTime = Timing.CurTime;
        lathe.ProductionLength = time;
        entity.Comp.CurrentRecipe = recipe;
        Dirty(entity, lathe);
        Dirty(entity);

        var ev = new LatheStartPrintingEvent(recipe);
        RaiseLocalEvent(entity, ref ev);

        _audio.PlayPvs(entity.Comp.ProducingSound, entity);

        if (time == TimeSpan.Zero)
        {
            FinishProducing((entity, entity.Comp));
        }

        UpdateUI((entity, entity.Comp));
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

            RefundCurrentRecipe((entity, entity.Comp), entity.Comp.CurrentRecipe.Value);
            Dirty(entity);
        }

        RemComp<LatheProducingComponent>(entity);
    }

    /// <summary>
    /// Refunds the material cost of the currently running recipe,
    /// without cancelling production
    /// </summary>
    private void RefundCurrentRecipe(Entity<LatheComponent> lathe, ProtoId<LatheRecipePrototype> currentRecipe)
    {
        var recipe = Proto.Index(currentRecipe);

        foreach (var (mat, amount) in GetAdjustedAmount(lathe, recipe))
        {
            _materialStorage.TryChangeMaterialAmount(lathe, mat, amount);
        }
    }

    /// <summary>
    /// Refunds the material cost of a given batch,
    /// without deleting it
    /// </summary>
    private void RefundBatch(Entity<LatheComponent> lathe, LatheRecipeBatch batch)
    {
        var delta = batch.ItemsRequested - batch.ItemsPrinted;

        var recipe = Proto.Index(batch.Recipe);

        foreach (var (mat, amount) in GetAdjustedAmount(lathe, recipe))
        {
            _materialStorage.TryChangeMaterialAmount(lathe, mat, amount * delta);
        }
    }

    protected void FinishProducing(Entity<LatheComponent> entity)
    {
        if (!Proto.Resolve(entity.Comp.CurrentRecipe, out var currentRecipe))
            return;

        if (currentRecipe.Result is { } resultProto)
        {
            var result = PredictedSpawnNextToOrDrop(resultProto, entity);
            _stack.TryMergeToContacts(result);
        }

        if (currentRecipe.ResultReagents is { } resultReagents &&
            entity.Comp.ReagentOutputSlotId is { } slotId)
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

        // This will dirty the component if it succeeds
        // Attempt to continue along the queue
        if (TryProcessQueue(entity))
            return;

        RemCompDeferred<LatheProducingComponent>(entity);
        Dirty(entity);
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
            var pack = Proto.Index(id);
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
        RaiseLocalEvent(entity, ev);
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
            if (latheRecipe.Result is not { } result)
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

        if (proto.Result is { } result)
        {
            return Proto.Index(result).Name;
        }

        if (proto.ResultReagents is { } resultReagents)
        {
            return ContentLocalizationManager.FormatList(resultReagents
                .Select(p => Loc.GetString("lathe-menu-result-reagent-display",
                    ("reagent", Proto.Index(p.Key).LocalizedName),
                    ("amount", p.Value)))
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

        if (proto.Result is { } result)
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

    protected virtual void UpdateUI(Entity<LatheComponent> entity) { }

    private void OnLatheQueueRecipeMessage(Entity<LatheComponent> entity, ref LatheQueueRecipeMessage args)
    {
        if (Proto.Resolve(args.ID, out LatheRecipePrototype? recipe))
        {
            if (TryAddToQueue(entity.AsNullable(), recipe, args.Quantity))
            {
                _adminLogger.Add(LogType.Action,
                    LogImpact.Low,
                    $"{ToPrettyString(args.Actor):player} queued {args.Quantity} {GetRecipeName(recipe)} at {ToPrettyString(entity):lathe}");
            }
        }

        TryStartProducing(entity.AsNullable());
        UpdateUI(entity);
    }

    private void OnLatheSyncRequestMessage(Entity<LatheComponent> entity, ref LatheSyncRequestMessage args)
    {
        UpdateUI(entity);
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

        RefundBatch(entity, batch);
        entity.Comp.Queue.Remove(node);
        Dirty(entity.AsNullable());
        UpdateUI(entity);
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

        Dirty(entity.AsNullable());
        UpdateUI(entity);
    }

    public void OnLatheAbortFabricationMessage(Entity<LatheComponent> entity, ref LatheAbortFabricationMessage args)
    {
        if (entity.Comp.CurrentRecipe is not { } recipe)
            return;

        _adminLogger.Add(LogType.Action,
            LogImpact.Low,
            $"{ToPrettyString(args.Actor):player} aborted printing {GetRecipeName(recipe)} at {ToPrettyString(entity):lathe}");

        RefundCurrentRecipe(entity, recipe);
        FinishProducing(entity);
    }

    #endregion

    private void OnMaterialAmountChanged(Entity<LatheComponent> entity, ref MaterialAmountChangedEvent args)
    {
        UpdateUI(entity);
    }

    private void OnDatabaseModified(Entity<LatheComponent> entity, ref TechnologyDatabaseModifiedEvent args)
    {
        UpdateUI(entity);
    }

    private void OnResearchRegistrationChanged(Entity<LatheComponent> entity, ref ResearchRegistrationChangedEvent args)
    {
        UpdateUI(entity);
    }
}
