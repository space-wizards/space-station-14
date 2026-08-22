using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Content.Server.Administration.Logs;
using Content.Server.Atmos.EntitySystems;
using Content.Server.Fluids.EntitySystems;
using Content.Server.Lathe.Components;
using Content.Server.Materials;
using Content.Server.Popups;
using Content.Server.Power.EntitySystems;
using Content.Server.Radio.EntitySystems;
using Content.Server.Stack;
using Content.Shared.Atmos;
using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.Database;
using Content.Shared.Emag.Systems;
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
using Robust.Shared.Prototypes;

namespace Content.Server.Lathe
{
    [UsedImplicitly]
    public sealed partial class LatheSystem : SharedLatheSystem
    {
        [Dependency] private IAdminLogManager _adminLogger = default!;
        [Dependency] private AtmosphereSystem _atmosphere = default!;
        [Dependency] private EmagSystem _emag = default!;
        [Dependency] private UserInterfaceSystem _uiSys = default!;
        [Dependency] private MaterialStorageSystem _materialStorage = default!;
        [Dependency] private TransformSystem _transform = default!;
        [Dependency] private RadioSystem _radio = default!;

        /// <summary>
        /// Per-tick cache
        /// </summary>
        private readonly List<GasMixture> _environments = new();

        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<LatheComponent, GetMaterialWhitelistEvent>(OnGetWhitelist);
            SubscribeLocalEvent<LatheComponent, MapInitEvent>(OnMapInit);
            SubscribeLocalEvent<LatheComponent, PowerChangedEvent>(OnPowerChanged);
            SubscribeLocalEvent<LatheComponent, TechnologyDatabaseModifiedEvent>(OnDatabaseModified);
            SubscribeLocalEvent<LatheAnnouncingComponent, TechnologyDatabaseModifiedEvent>(OnTechnologyDatabaseModified);
            SubscribeLocalEvent<LatheComponent, ResearchRegistrationChangedEvent>(OnResearchRegistrationChanged);

            SubscribeLocalEvent<LatheComponent, LatheMoveRequestMessage>(OnLatheMoveRequestMessage);
            SubscribeLocalEvent<LatheComponent, LatheAbortFabricationMessage>(OnLatheAbortFabricationMessage);

            SubscribeLocalEvent<LatheHeatProducingComponent, LatheStartPrintingEvent>(OnHeatStartPrinting);
        }
        public override void Update(float frameTime)
        {
            var query = EntityQueryEnumerator<LatheProducingComponent, LatheComponent>();
            while (query.MoveNext(out var uid, out var comp, out var lathe))
            {
                if (lathe.CurrentRecipe == null)
                    continue;

                if (Timing.CurTime - comp.StartTime >= comp.ProductionLength)
                    FinishProducing((uid, lathe));
            }

            var heatQuery = EntityQueryEnumerator<LatheHeatProducingComponent, LatheProducingComponent, TransformComponent>();
            while (heatQuery.MoveNext(out var uid, out var heatComp, out _, out var xform))
            {
                if (Timing.CurTime < heatComp.NextSecond)
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
            var recipes = GetAvailableRecipes(uid, component, true);
            foreach (var id in recipes)
            {
                if (!ProtoMan.Resolve(id, out var proto))
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

        private void OnHeatStartPrinting(EntityUid uid, LatheHeatProducingComponent component, LatheStartPrintingEvent args)
        {
            component.NextSecond = Timing.CurTime;
        }

        /// <summary>
        /// Initialize the UI and appearance.
        /// Appearance requires initialization or the layers break
        /// </summary>
        private void OnMapInit(EntityUid uid, LatheComponent component, MapInitEvent args)
        {
            Appearance.SetData(uid, LatheVisuals.IsInserting, false);
            Appearance.SetData(uid, LatheVisuals.IsRunning, false);

            _materialStorage.UpdateMaterialWhitelist(uid);
        }

        private void OnPowerChanged(EntityUid uid, LatheComponent component, ref PowerChangedEvent args)
        {
            if (!args.Powered)
            {
                AbortProduction(uid);
            }
            else
            {
                TryStartProducing((uid, component), null);
            }
        }

        private void OnDatabaseModified(EntityUid uid, LatheComponent component, ref TechnologyDatabaseModifiedEvent args)
        {
            _uiSys.ServerSendUiMessage((uid, null), LatheUiKey.Key, new LatheRefreshRecipesMessage());
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

                if (!ProtoMan.TryIndex(recipeId, out LatheRecipePrototype? recipe))
                    continue;

                var itemName = GetRecipeName(recipe!);
                recipeNames.Add(Loc.GetString("lathe-unlock-recipe-radio-broadcast-item", ("item", itemName)));
            }

            if (recipeNames.Count == 0)
                return;

            var message =
                recipeNames.Count > ent.Comp.MaximumItems ?
                    Loc.GetString(
                        "lathe-unlock-recipe-radio-broadcast-overflow",
                        ("items", ContentLocalizationManager.FormatList(recipeNames.GetRange(0, ent.Comp.MaximumItems))),
                        ("count", recipeNames.Count)
                    ) :
                    Loc.GetString(
                        "lathe-unlock-recipe-radio-broadcast",
                        ("items", ContentLocalizationManager.FormatList(recipeNames))
                    );

            foreach (var channel in ent.Comp.Channels)
            {
                _radio.SendRadioMessage(ent.Owner, message, channel, ent.Owner, escapeMarkup: false);
            }
        }

        private void OnResearchRegistrationChanged(EntityUid uid, LatheComponent component, ref ResearchRegistrationChangedEvent args)
        {
            _uiSys.ServerSendUiMessage((uid, null), LatheUiKey.Key, new LatheRefreshRecipesMessage());
        }

        protected override bool HasRecipe(EntityUid uid, LatheRecipePrototype recipe, LatheComponent component)
        {
            return GetAvailableRecipes(uid, component).Contains(recipe.ID);
        }

        /// <summary>
        /// Refunds the material cost of the currently running recipe,
        /// without cancelling production
        /// </summary>
        private void RefundCurrentRecipe(EntityUid uid, LatheComponent lathe)
        {
            ProtoMan.Resolve(lathe.CurrentRecipe, out var recipe);

            foreach (var (mat, amount) in GetAdjustedAmount(lathe, recipe!))
                _materialStorage.TryChangeMaterialAmount(uid, mat, amount);
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
                        DirtyField(uid, component, nameof(LatheComponent.Queue));
                    }
                    else if (batch.ItemsPrinted > 0)
                    {
                        batch.ItemsPrinted--;
                    }
                }

                RefundCurrentRecipe(uid, component);
                component.CurrentRecipe = null;
                DirtyField(uid, component, nameof(LatheComponent.CurrentRecipe));
            }
            RemCompDeferred<LatheProducingComponent>(uid);
            UpdateRunningAppearance(uid, false);
        }

        #region UI Messages


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

            DirtyField(uid, component, nameof(component.Queue));
        }

        public void OnLatheAbortFabricationMessage(EntityUid uid, LatheComponent component, ref LatheAbortFabricationMessage args)
        {
            if (component.CurrentRecipe == null)
                return;

            _adminLogger.Add(LogType.Action,
                LogImpact.Low,
                $"{ToPrettyString(args.Actor):player} aborted printing {GetRecipeName(component.CurrentRecipe.Value)} at {ToPrettyString(uid):lathe}");

            RefundCurrentRecipe(uid, component);
            component.CurrentRecipe = null;
            FinishProducing((uid, component));
            DirtyField(uid, component, nameof(LatheComponent.CurrentRecipe));
        }
        #endregion

        protected override bool IsPowered(EntityUid ent)
        {
            return this.IsPowered(ent, EntityManager);
        }

        protected override void DeleteQueueEntryImpl(Entity<LatheComponent> uid, LatheDeleteRequestMessage args, LinkedListNode<LatheRecipeBatch> entry)
        {
            var batch = entry.Value;
            _adminLogger.Add(LogType.Action,
                    LogImpact.Low,
                    $"{ToPrettyString(args.Actor):player} deleted a lathe job for ({batch.ItemsPrinted}/{batch.ItemsRequested}) {GetRecipeName(batch.Recipe)} at {ToPrettyString(uid):lathe}");

            base.DeleteQueueEntryImpl(uid, args, entry);
        }

        protected override void LogRecipeQueueAddition(Entity<LatheComponent> uid, ref LatheQueueRecipeMessage args, LatheRecipePrototype recipe)
        {
            _adminLogger.Add(LogType.Action,
                    LogImpact.Low,
                    $"{ToPrettyString(args.Actor):player} queued {args.Quantity} {GetRecipeName(recipe)} at {ToPrettyString(uid):lathe}");
        }
    }
}
