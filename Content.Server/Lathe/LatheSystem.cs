using System.Diagnostics.CodeAnalysis;
using Content.Server.Lathe.Components;
using Content.Shared.Lathe;
using Content.Shared.Materials;
using Content.Shared.Research.Prototypes;
using Content.Server.Research.Components;
using Content.Server.Power.EntitySystems;
using Content.Server.Research;
using Content.Shared.Research.Components;
using Robust.Server.GameObjects;
using Robust.Shared.Prototypes;
using JetBrains.Annotations;
using System.Linq;
using Content.Server.Materials;
using Content.Server.Power.Components;
using Robust.Server.Player;

namespace Content.Server.Lathe
{
    [UsedImplicitly]
    public sealed class LatheSystem : SharedLatheSystem
    {
        [Dependency] private readonly IPrototypeManager _proto = default!;
        [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
        [Dependency] private readonly SharedAudioSystem _audio = default!;
        [Dependency] private readonly UserInterfaceSystem _uiSys = default!;
        [Dependency] private readonly ResearchSystem _researchSys = default!;
        [Dependency] private readonly MaterialStorageSystem _materialStorage = default!;

        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<LatheComponent, MaterialEntityInsertedEvent>(OnMaterialEntityInserted);
            SubscribeLocalEvent<LatheComponent, GetMaterialWhitelistEvent>(OnGetWhitelist);
            SubscribeLocalEvent<LatheComponent, MapInitEvent>(OnMapInit);
            SubscribeLocalEvent<LatheComponent, LatheQueueRecipeMessage>(OnLatheQueueRecipeMessage);
            SubscribeLocalEvent<LatheComponent, LatheSyncRequestMessage>(OnLatheSyncRequestMessage);
            SubscribeLocalEvent<LatheComponent, LatheServerSelectionMessage>(OnLatheServerSelectionMessage);
            SubscribeLocalEvent<LatheComponent, PowerChangedEvent>(OnPowerChanged);
            SubscribeLocalEvent<TechnologyDatabaseComponent, LatheGetRecipesEvent>(OnGetRecipes);
            SubscribeLocalEvent<TechnologyDatabaseComponent, LatheServerSyncMessage>(OnLatheServerSyncMessage);
        }

        public override void Update(float frameTime)
        {
            foreach (var comp in EntityQuery<LatheInsertingComponent>())
            {
                comp.TimeRemaining -= frameTime;

                if (comp.TimeRemaining > 0)
                    continue;

                UpdateInsertingAppearance(comp.Owner, false);
                RemCompDeferred(comp.Owner, comp);
            }

            foreach (var comp in EntityQuery<LatheProducingComponent>())
            {
                comp.TimeRemaining -= frameTime;

                if (comp.TimeRemaining <= 0)
                    FinishProducing(comp.Owner, comp);
            }
        }

        private void OnGetWhitelist(EntityUid uid, LatheComponent component, GetMaterialWhitelistEvent args)
        {
            var materialWhitelist = new List<string>();
            var recipes =  GetAllBaseRecipes(component);
            foreach (var id in recipes)
            {
                if (!_proto.TryIndex<LatheRecipePrototype>(id, out var proto))
                    continue;
                foreach (var (mat, _) in proto.RequiredMaterials)
                {
                    if (!materialWhitelist.Contains(mat))
                        materialWhitelist.Add(mat);
                }
            }
            args.Whitelist = args.Whitelist.Union(materialWhitelist).ToList();
        }

        public bool TryGetAvailableRecipes(EntityUid uid, [NotNullWhen(true)] out List<string>? recipes, LatheComponent? component = null)
        {
            recipes = null;
            if (!Resolve(uid, ref component))
                return false;
            recipes = GetAvailableRecipes(component);
            return true;
        }

        public List<string> GetAvailableRecipes(LatheComponent component)
        {
            var ev = new LatheGetRecipesEvent(component.Owner)
            {
                Recipes = component.StaticRecipes
            };
            RaiseLocalEvent(component.Owner, ev, true);
            return ev.Recipes;
        }

        public List<string> GetAllBaseRecipes(LatheComponent component)
        {
            return component.DynamicRecipes == null
                ? component.StaticRecipes
                : component.StaticRecipes.Union(component.DynamicRecipes).ToList();
        }

        private void OnGetRecipes(EntityUid uid, TechnologyDatabaseComponent component, LatheGetRecipesEvent args)
        {
            if (uid != args.Lathe || !TryComp<LatheComponent>(uid, out var latheComponent) || latheComponent.DynamicRecipes == null)
                return;

            //gets all of the techs that are unlocked and also in the DynamicRecipes list
            var allTechs = (from tech in component.Technologies
                from recipe in tech.UnlockedRecipes
                where latheComponent.DynamicRecipes.Contains(recipe)
                select recipe).ToList();

            args.Recipes = args.Recipes.Union(allTechs).ToList();
        }

        /// <summary>
        /// Initialize the UI and appearance.
        /// Appearance requires initialization or the layers break
        /// </summary>
        private void OnMapInit(EntityUid uid, LatheComponent component, MapInitEvent args)
        {
            _appearance.SetData(uid, LatheVisuals.IsInserting, false);
            _appearance.SetData(uid, LatheVisuals.IsRunning, false);

            _materialStorage.UpdateMaterialWhitelist(uid);
        }

        private void OnMaterialEntityInserted(EntityUid uid, LatheComponent component, MaterialEntityInsertedEvent args)
        {
            var lastMat = args.Materials.Keys.Last();
            // We need the prototype to get the color
            _proto.TryIndex(lastMat, out MaterialPrototype? matProto);
            EnsureComp<LatheInsertingComponent>(uid).TimeRemaining = component.InsertionTime;
            UpdateInsertingAppearance(uid, true, matProto?.Color);
        }

        private void OnPowerChanged(EntityUid uid, LatheComponent component, PowerChangedEvent args)
        {
            //if the power state changes, try to produce.
            //aka, if you went from unpowered --> powered, resume lathe queue.
            TryStartProducing(uid, component: component);
        }

        /// <summary>
        /// This handles the checks to start producing an item, and
        /// starts up the sound and visuals
        /// </summary>
        private bool TryStartProducing(EntityUid uid, LatheProducingComponent? prodComp = null, LatheComponent? component = null)
        {
            if (!Resolve(uid, ref component) || component.Queue.Count == 0)
                return false;

            if (!this.IsPowered(uid, EntityManager))
                return false;

            var recipeId = component.Queue[0];

            if (!_proto.TryIndex<LatheRecipePrototype>(recipeId, out var recipe))
            {
                // recipie does not exist. Remove and try produce the next item.
                component.Queue.RemoveAt(0);
                return TryStartProducing(uid, prodComp, component);
            }

            if (!CanProduce(uid, recipe, component: component) || !TryComp(uid, out MaterialStorageComponent? storage))
            {
                component.Queue.RemoveAt(0);
                return false;
            }

            prodComp ??= EnsureComp<LatheProducingComponent>(uid);

            // Do nothing if the lathe is already producing something.
            if (prodComp.Recipe != null)
                return false;

            component.Queue.RemoveAt(0);
            prodComp.Recipe = recipeId;
            prodComp.TimeRemaining = (float)recipe.CompleteTime.TotalSeconds;

            foreach (var (material, amount) in recipe.RequiredMaterials)
            {
                // This should always return true, otherwise CanProduce fucked up.
                // TODO just remove materials when first queuing, to avoid queuing more items than can actually be produced.
                _materialStorage.CanChangeMaterialAmount(uid, material, -amount, storage);
            }

            // Again, this should really just be a bui state instead of two separate messages.
            _uiSys.TrySendUiMessage(uid, LatheUiKey.Key, new LatheProducingRecipeMessage(recipe.ID));
            _uiSys.TrySendUiMessage(uid, LatheUiKey.Key, new LatheFullQueueMessage(component.Queue));

            _audio.PlayPvs(component.ProducingSound, component.Owner);

            UpdateRunningAppearance(uid, true);
            return true;
        }

        /// <summary>
        /// If we were able to produce the recipe,
        /// spawn it and cleanup. If we weren't, just do cleanup.
        /// </summary>
        private void FinishProducing(EntityUid uid, LatheProducingComponent prodComp)
        {
            if (prodComp.Recipe == null || !_proto.TryIndex<LatheRecipePrototype>(prodComp.Recipe, out var recipe))
            {
                RemCompDeferred(prodComp.Owner, prodComp);
                UpdateRunningAppearance(uid, false);
                return;
            }

            Spawn(recipe.Result, Transform(uid).Coordinates);
            prodComp.Recipe = null;

            // TODO this should probably just be a BUI state, not a special message.
            _uiSys.TrySendUiMessage(uid, LatheUiKey.Key, new LatheStoppedProducingRecipeMessage());

            // Continue to next in queue if there are items left
            if (TryStartProducing(uid, prodComp))
                return;

            RemComp(prodComp.Owner, prodComp);
            UpdateRunningAppearance(uid, false);
        }

        /// <summary>
        /// Sets the machine sprite to either play the running animation
        /// or stop.
        /// </summary>
        private void UpdateRunningAppearance(EntityUid uid, bool isRunning)
        {
            _appearance.SetData(uid, LatheVisuals.IsRunning, isRunning);
        }

        /// <summary>
        /// Sets the machine sprite to play the inserting animation
        /// and sets the color of the inserted mat if applicable
        /// </summary>
        private void UpdateInsertingAppearance(EntityUid uid, bool isInserting, Color? color = null)
        {
            _appearance.SetData(uid, LatheVisuals.IsInserting, isInserting);
            if (color != null)
                _appearance.SetData(uid, LatheVisuals.InsertingColor, color);
        }

        #region UI Messages

        private void OnLatheQueueRecipeMessage(EntityUid uid, LatheComponent component, LatheQueueRecipeMessage args)
        {
            if (_proto.TryIndex(args.ID, out LatheRecipePrototype? recipe))
            {
                for (var i = 0; i < args.Quantity; i++)
                {
                    // TODO check required materials exist and make materials unavailable.
                    component.Queue.Add(recipe.ID);
                }

                // Again: TODO this should be handled by BUI states
                _uiSys.TrySendUiMessage(uid, LatheUiKey.Key, new LatheFullQueueMessage(component.Queue));
            }

            TryStartProducing(component.Owner, null, component);
        }

        private void OnLatheSyncRequestMessage(EntityUid uid, LatheComponent component, LatheSyncRequestMessage args)
        {
            // Again: TODO BUI states. Why TF was this was this ever two separate messages!?!?
            _uiSys.TrySendUiMessage(uid, LatheUiKey.Key, new LatheFullQueueMessage(component.Queue));
            if (TryComp(uid, out LatheProducingComponent? prodComp) && prodComp.Recipe != null)
                _uiSys.TrySendUiMessage(uid, LatheUiKey.Key, new LatheProducingRecipeMessage(prodComp.Recipe));
        }

        private void OnLatheServerSelectionMessage(EntityUid uid, LatheComponent component, LatheServerSelectionMessage args)
        {
            // TODO W.. b.. why?
            // the client can just open the ui itself. why tf is it asking the server to open it for it.
            _uiSys.TryOpen(uid, ResearchClientUiKey.Key, (IPlayerSession) args.Session);
        }

        private void OnLatheServerSyncMessage(EntityUid uid, TechnologyDatabaseComponent component, LatheServerSyncMessage args)
        {
            _researchSys.SyncWithServer(component);

            //TODO: make the lathe ui update on sync if nothing has changed
        }

        #endregion
    }
}
