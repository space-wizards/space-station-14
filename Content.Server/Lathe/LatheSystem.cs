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
    public sealed class LatheSystem : EntitySystem
    {
        [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
        [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
        [Dependency] private readonly SharedAudioSystem _audioSys = default!;
        [Dependency] private readonly UserInterfaceSystem _uiSys = default!;
        [Dependency] private readonly ResearchSystem _researchSys = default!;
        [Dependency] private readonly MaterialStorageSystem _materialStorage = default!;

        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<LatheComponent, MaterialEntityInsertedEvent>(OnMaterialEntityInserted);
            SubscribeLocalEvent<LatheComponent, ComponentInit>(OnComponentInit);
            SubscribeLocalEvent<LatheComponent, LatheQueueRecipeMessage>(OnLatheQueueRecipeMessage);
            SubscribeLocalEvent<LatheComponent, LatheSyncRequestMessage>(OnLatheSyncRequestMessage);
            SubscribeLocalEvent<LatheComponent, LatheServerSelectionMessage>(OnLatheServerSelectionMessage);
            SubscribeLocalEvent<LatheComponent, PowerChangedEvent>(OnPowerChanged);
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

        /// <summary>
        /// Initialize the UI and appearance.
        /// Appearance requires initialization or the layers break
        /// </summary>
        private void OnComponentInit(EntityUid uid, LatheComponent component, ComponentInit args)
        {
            _appearance.SetData(uid, LatheVisuals.IsInserting, false);
            _appearance.SetData(uid, LatheVisuals.IsRunning, false);

            //Fix this awful shit once Lathes get ECS'd.
            List<LatheRecipePrototype>? recipes = null;
            if (TryComp<ProtolatheDatabaseComponent>(uid, out var database))
                recipes = database.ProtolatheRecipes.ToList();
            else if (TryComp<LatheDatabaseComponent>(uid, out var database2))
                recipes = database2._recipes;

            if (recipes == null)
                return;

            if (!TryComp<MaterialStorageComponent>(uid, out var storage))
                return;

            //TODO: fuck
            storage.MaterialWhiteList = new();
            foreach (var recipe in recipes)
            {
                foreach (var mat in recipe.RequiredMaterials)
                {
                    if (!storage.MaterialWhiteList.Contains(mat.Key))
                        storage.MaterialWhiteList.Add(mat.Key);
                }
            }
        }

        private void OnMaterialEntityInserted(EntityUid uid, LatheComponent component, MaterialEntityInsertedEvent args)
        {
            var lastMat = args.Materials.Keys.Last();
            // We need the prototype to get the color
            _prototypeManager.TryIndex(lastMat, out MaterialPrototype? matProto);
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

            if (!_prototypeManager.TryIndex<LatheRecipePrototype>(recipeId, out var recipe))
            {
                // recipie does not exist. Remove and try produce the next item.
                component.Queue.RemoveAt(0);
                return TryStartProducing(uid, prodComp, component);
            }

            if (!component.CanProduce(recipe) || !TryComp(uid, out MaterialStorageComponent? storage))
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
                _materialStorage.CanChangeMaterialAmount(storage, material, -amount);
            }

            // Again, this should really just be a bui state instead of two separate messages.
            _uiSys.TrySendUiMessage(uid, LatheUiKey.Key, new LatheProducingRecipeMessage(recipe.ID));
            _uiSys.TrySendUiMessage(uid, LatheUiKey.Key, new LatheFullQueueMessage(component.Queue));

            if (component.ProducingSound != null)
                _audioSys.PlayPvs(component.ProducingSound, component.Owner);

            UpdateRunningAppearance(uid, true);
            return true;
        }

        /// <summary>
        /// If we were able to produce the recipe,
        /// spawn it and cleanup. If we weren't, just do cleanup.
        /// </summary>
        private void FinishProducing(EntityUid uid, LatheProducingComponent prodComp)
        {
            if (prodComp.Recipe == null || !_prototypeManager.TryIndex<LatheRecipePrototype>(prodComp.Recipe, out var recipe))
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
            if (_prototypeManager.TryIndex(args.ID, out LatheRecipePrototype? recipe))
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

            if (TryComp(uid, out ProtolatheDatabaseComponent? protoDatabase))
                protoDatabase.Sync();
        }

        #endregion
    }
}
