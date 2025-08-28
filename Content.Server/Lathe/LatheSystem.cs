using System.Linq;
using Content.Server.Administration.Logs;
using Content.Server.Atmos.EntitySystems;
using Content.Server.Fluids.EntitySystems;
using Content.Server.Lathe.Components;
using Content.Server.Popups;
using Content.Server.Power.EntitySystems;
using Content.Server.Radio.EntitySystems;
using Content.Server.Stack;
using Content.Shared.Atmos;
using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.UserInterface;
using Content.Shared.Database;
using Content.Shared.Lathe;
using Content.Shared.Localizations;
using Content.Shared.Materials;
using Content.Shared.Power;
using Content.Shared.ReagentSpeed;
using Content.Shared.Research.Components;
using Content.Shared.Research.Prototypes;
using JetBrains.Annotations;
using Robust.Server.Containers;
using Robust.Server.GameObjects;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Content.Server.Lathe
{
    [UsedImplicitly]
    public sealed class LatheSystem : SharedLatheSystem
    {
        [Dependency] private readonly IGameTiming _timing = default!;
        [Dependency] private readonly IAdminLogManager _adminLogger = default!;
        [Dependency] private readonly AtmosphereSystem _atmosphere = default!;
        [Dependency] private readonly SharedAudioSystem _audio = default!;
        [Dependency] private readonly ContainerSystem _container = default!;
        [Dependency] private readonly UserInterfaceSystem _uiSys = default!;
        [Dependency] private readonly PopupSystem _popup = default!;
        [Dependency] private readonly PuddleSystem _puddle = default!;
        [Dependency] private readonly ReagentSpeedSystem _reagentSpeed = default!;
        [Dependency] private readonly SharedSolutionContainerSystem _solution = default!;
        [Dependency] private readonly StackSystem _stack = default!;
        [Dependency] private readonly TransformSystem _transform = default!;
        [Dependency] private readonly RadioSystem _radio = default!;

        /// <summary>
        /// Per-tick cache
        /// </summary>
        private readonly List<GasMixture> _environments = new();

        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<LatheComponent, GetMaterialWhitelistEvent>(OnGetWhitelist);
            SubscribeLocalEvent<LatheComponent, PowerChangedEvent>(OnPowerChanged);
            SubscribeLocalEvent<LatheComponent, TechnologyDatabaseModifiedEvent>(OnDatabaseModified);
            SubscribeLocalEvent<LatheAnnouncingComponent, TechnologyDatabaseModifiedEvent>(OnTechnologyDatabaseModified);
            SubscribeLocalEvent<LatheComponent, ResearchRegistrationChangedEvent>(OnResearchRegistrationChanged);

            SubscribeLocalEvent<LatheComponent, LatheQueueRecipeMessage>(OnLatheQueueRecipeMessage);
            SubscribeLocalEvent<LatheComponent, LatheSyncRequestMessage>(OnLatheSyncRequestMessage);
            SubscribeLocalEvent<LatheComponent, LatheDeleteRequestMessage>(OnLatheDeleteRequestMessage);
            SubscribeLocalEvent<LatheComponent, LatheMoveRequestMessage>(OnLatheMoveRequestMessage);
            SubscribeLocalEvent<LatheComponent, LatheAbortFabricationMessage>(OnLatheAbortFabricationMessage);

            SubscribeLocalEvent<LatheComponent, BeforeActivatableUIOpenEvent>((u, c, _) => UpdateUserInterfaceState((u, c)));
            SubscribeLocalEvent<LatheComponent, MaterialAmountChangedEvent>(OnMaterialAmountChanged);
            SubscribeLocalEvent<LatheHeatProducingComponent, LatheStartPrintingEvent>(OnHeatStartPrinting);
        }
        public override void Update(float frameTime)
        {
            var query = EntityQueryEnumerator<LatheProducingComponent, LatheComponent>();
            while (query.MoveNext(out var uid, out var comp, out var lathe))
            {
                if (lathe.CurrentRecipe == null)
                    continue;

                if (_timing.CurTime - comp.StartTime >= comp.ProductionLength)
                    FinishProducing((uid, lathe, comp));
            }

            var heatQuery = EntityQueryEnumerator<LatheHeatProducingComponent, LatheProducingComponent, TransformComponent>();
            while (heatQuery.MoveNext(out var uid, out var heatComp, out _, out var xform))
            {
                if (_timing.CurTime < heatComp.NextSecond)
                    continue;
                heatComp.NextSecond += TimeSpan.FromSeconds(1);

                var position = _transform.GetGridTilePositionOrDefault((uid, xform));
                _environments.Clear();

                if (_atmosphere.GetTileMixture(xform.GridUid, xform.MapUid, position, true) is { } tileMix)
                    _environments.Add(tileMix);

                if (xform.GridUid != null)
                {
                    var enumerator = _atmosphere.GetAdjacentTileMixtures(xform.GridUid.Value, position, false, true);
                    while (enumerator.MoveNext(out var mix))
                    {
                        _environments.Add(mix);
                    }
                }

                if (_environments.Count > 0)
                {
                    var heatPerTile = heatComp.EnergyPerSecond / _environments.Count;
                    foreach (var env in _environments)
                    {
                        _atmosphere.AddHeat(env, heatPerTile);
                    }
                }
            }
        }

        private void OnGetWhitelist(EntityUid uid, LatheComponent component, ref GetMaterialWhitelistEvent args)
        {
            if (args.Storage != uid)
                return;
            var materialWhitelist = new List<ProtoId<MaterialPrototype>>();
            var recipes = GetAvailableRecipes((uid, component), true);
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

        public bool TryStartProducing(Entity<LatheComponent?> entity)
        {
            if (!Resolve(entity, ref entity.Comp))
                return false;

            if (entity.Comp.CurrentRecipe != null || entity.Comp.Queue.Count <= 0 || !this.IsPowered(entity.Owner, EntityManager))
                return false;

            var batch = entity.Comp.Queue.First();
            batch.ItemsPrinted++;
            if (batch.ItemsPrinted >= batch.ItemsRequested || batch.ItemsPrinted < 0) // Rollover sanity check
                component.Queue.RemoveFirst();
            var recipe = _proto.Index(batch.Recipe);

            var time = _reagentSpeed.ApplySpeed(entity.Owner, recipe.CompleteTime) * entity.Comp.TimeMultiplier;

            var lathe = EnsureComp<LatheProducingComponent>(entity);
            lathe.StartTime = _timing.CurTime;
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

        private void FinishProducing(Entity<LatheComponent, LatheProducingComponent> entity)
        {
            if (entity.Comp1.CurrentRecipe != null)
            {
                var currentRecipe = Proto.Index(entity.Comp1.CurrentRecipe.Value);
                if (currentRecipe.Result is { } resultProto)
                {
                    var result = Spawn(resultProto, Transform(entity).Coordinates);
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
            entity.Comp2.StartTime = _timing.CurTime;

            if (!TryStartProducing(entity.AsNullable()))
            {
                RemCompDeferred(entity, entity.Comp2);
                UpdateUserInterfaceState(entity.AsNullable());
                UpdateRunningAppearance(entity, false);
            }
        }

        public void UpdateUserInterfaceState(Entity<LatheComponent?> entity)
        {
            if (!Resolve(entity, ref entity.Comp))
                return;

            var producing = component.CurrentRecipe;
            if (producing == null && component.Queue.First is { } node)
                producing = node.Value.Recipe;

            var state = new LatheUpdateState(GetAvailableRecipes((entity, entity.Comp)), entity.Comp.Queue.ToArray(), producing);
            _uiSys.SetUiState(entity.Owner, LatheUiKey.Key, state);
        }

        private void OnHeatStartPrinting(EntityUid uid, LatheHeatProducingComponent component, LatheStartPrintingEvent args)
        {
            component.NextSecond = _timing.CurTime;
        }

        private void OnMaterialAmountChanged(Entity<LatheComponent> entity, ref MaterialAmountChangedEvent args)
        {
            UpdateUserInterfaceState(entity.AsNullable());
        }

        /// <summary>
        /// Sets the machine sprite to either play the running animation
        /// or stop.
        /// </summary>
        private void UpdateRunningAppearance(EntityUid uid, bool isRunning)
        {
            Appearance.SetData(uid, LatheVisuals.IsRunning, isRunning);
        }

        private void OnPowerChanged(Entity<LatheComponent> entity, ref PowerChangedEvent args)
        {
            if (!args.Powered)
            {
                AbortProduction(uid);
            }
            else
            {
                TryStartProducing(uid, component);
            }
        }

        private void OnDatabaseModified(Entity<LatheComponent> entity, ref TechnologyDatabaseModifiedEvent args)
        {
            UpdateUserInterfaceState(entity.AsNullable());
        }

        private void OnTechnologyDatabaseModified(Entity<LatheAnnouncingComponent> ent, ref TechnologyDatabaseModifiedEvent args)
        {
            if (args.NewlyUnlockedRecipes is null)
                return;

            if (!TryGetAvailableRecipes(ent.Owner, out var potentialRecipes))
                return;

            var recipeNames = new List<string>();
            foreach (var recipeId in args.NewlyUnlockedRecipes)
            {
                if (!potentialRecipes.Contains(new(recipeId)))
                    continue;

                if (!Proto.TryIndex(recipeId, out LatheRecipePrototype? recipe))
                    continue;

                var itemName = GetRecipeName(recipe);
                recipeNames.Add(Loc.GetString("lathe-unlock-recipe-radio-broadcast-item", ("item", itemName)));
            }

            if (recipeNames.Count == 0)
                return;

            var message =
                recipeNames.Count > ent.Comp.MaximumItems
                    ? Loc.GetString(
                        "lathe-unlock-recipe-radio-broadcast-overflow",
                        ("items", ContentLocalizationManager.FormatList(recipeNames.GetRange(0, ent.Comp.MaximumItems))),
                        ("count", recipeNames.Count)
                    )
                    : Loc.GetString(
                        "lathe-unlock-recipe-radio-broadcast",
                        ("items", ContentLocalizationManager.FormatList(recipeNames))
                    );

            foreach (var channel in ent.Comp.Channels)
            {
                _radio.SendRadioMessage(ent.Owner, message, channel, ent.Owner, escapeMarkup: false);
            }
        }

        private void OnResearchRegistrationChanged(Entity<LatheComponent> entity, ref ResearchRegistrationChangedEvent args)
        {
            UpdateUserInterfaceState(entity.AsNullable());
        }

        public void AbortProduction(EntityUid uid, LatheComponent? component = null)
        {
            if (!Resolve(uid, ref component))
                return;

            if (component.CurrentRecipe != null)
            {
                if (component.Queue.Count > 0)
                {
                    // Batch abandoned while printing last item, need to create a one-item batch
                    var batch = component.Queue.First();
                    if (batch.Recipe != component.CurrentRecipe)
                    {
                        var newBatch = new LatheRecipeBatch(component.CurrentRecipe.Value, 0, 1);
                        component.Queue.AddFirst(newBatch);
                    }
                    else if (batch.ItemsPrinted > 0)
                    {
                        batch.ItemsPrinted--;
                    }
                }

                component.CurrentRecipe = null;
            }
            RemCompDeferred<LatheProducingComponent>(uid);
            UpdateUserInterfaceState(uid, component);
            UpdateRunningAppearance(uid, false);
        }

        #region UI Messages

        private void OnLatheQueueRecipeMessage(Entity<LatheComponent> entity, ref LatheQueueRecipeMessage args)
        {
            if (Proto.TryIndex(args.ID, out LatheRecipePrototype? recipe))
            {
                var count = 0;
                for (var i = 0; i < args.Quantity; i++)
                {
                    if (TryAddToQueue(entity.AsNullable(), recipe))
                        count++;
                    else
                        break;
                }
                if (count > 0)
                if (TryAddToQueue(uid, recipe, args.Quantity, component))
                {
                    _adminLogger.Add(LogType.Action,
                        LogImpact.Low,
                        $"{ToPrettyString(args.Actor):player} queued {count} {GetRecipeName(recipe)} at {ToPrettyString(entity):lathe}");
                        $"{ToPrettyString(args.Actor):player} queued {args.Quantity} {GetRecipeName(recipe)} at {ToPrettyString(uid):lathe}");
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
        /// <param name="uid">The lathe whose queue is being altered.</param>
        /// <param name="component"></param>
        /// <param name="args"></param>
        public void OnLatheDeleteRequestMessage(EntityUid uid, LatheComponent component, ref LatheDeleteRequestMessage args)
        {
            if (args.Index < 0 || args.Index >= component.Queue.Count)
                return;

            var node = component.Queue.First;
            for (int i = 0; i < args.Index; i++)
                node = node?.Next;

            if (node == null) // Shouldn't happen with checks above.
                return;

            var batch = node.Value;
            _adminLogger.Add(LogType.Action,
                LogImpact.Low,
                $"{ToPrettyString(args.Actor):player} deleted a lathe job for ({batch.ItemsPrinted}/{batch.ItemsRequested}) {GetRecipeName(batch.Recipe)} at {ToPrettyString(uid):lathe}");

            component.Queue.Remove(node);
            UpdateUserInterfaceState(uid, component);
        }

        public void OnLatheMoveRequestMessage(EntityUid uid, LatheComponent component, ref LatheMoveRequestMessage args)
        {
            if (args.Change == 0 || args.Index < 0 || args.Index >= component.Queue.Count)
                return;

            // New index must be within the bounds of the batch.
            var newIndex = args.Index + args.Change;
            if (newIndex < 0 || newIndex >= component.Queue.Count)
                return;

            var node = component.Queue.First;
            for (int i = 0; i < args.Index; i++)
                node = node?.Next;

            if (node == null) // Something went wrong.
                return;

            if (args.Change > 0)
            {
                var newRelativeNode = node.Next;
                for (int i = 1; i < args.Change; i++) // 1-indexed: starting from Next
                    newRelativeNode = newRelativeNode?.Next;

                if (newRelativeNode == null) // Something went wrong.
                    return;

                component.Queue.Remove(node);
                component.Queue.AddAfter(newRelativeNode, node);
            }
            else
            {
                var newRelativeNode = node.Previous;
                for (int i = 1; i < -args.Change; i++) // 1-indexed: starting from Previous
                    newRelativeNode = newRelativeNode?.Previous;

                if (newRelativeNode == null) // Something went wrong.
                    return;

                component.Queue.Remove(node);
                component.Queue.AddBefore(newRelativeNode, node);
            }

            UpdateUserInterfaceState(uid, component);
        }

        public void OnLatheAbortFabricationMessage(EntityUid uid, LatheComponent component, ref LatheAbortFabricationMessage args)
        {
            if (component.CurrentRecipe == null)
                return;

            _adminLogger.Add(LogType.Action,
                LogImpact.Low,
                $"{ToPrettyString(args.Actor):player} aborted printing {GetRecipeName(component.CurrentRecipe.Value)} at {ToPrettyString(uid):lathe}");

            component.CurrentRecipe = null;
            FinishProducing(uid, component);
        }
        #endregion
    }
}
