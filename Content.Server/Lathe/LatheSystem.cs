using Content.Server.Lathe.Components;
using Content.Shared.Lathe;
using Content.Shared.Materials;
using Content.Shared.Research.Prototypes;
using Content.Server.Research.Components;
using Content.Shared.Interaction;
using Content.Server.Materials;
using Content.Server.Popups;
using Content.Server.Power.EntitySystems;
using Content.Server.Research;
using Content.Server.Stack;
using Content.Shared.Research.Components;
using Robust.Server.GameObjects;
using Robust.Shared.Prototypes;
using Robust.Shared.Player;
using JetBrains.Annotations;
using System.Linq;
using Content.Server.Power.Components;
using Robust.Server.Player;

namespace Content.Server.Lathe
{
    [UsedImplicitly]
    public sealed class LatheSystem : EntitySystem
    {
        [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
        [Dependency] private readonly PopupSystem _popupSystem = default!;
        [Dependency] private readonly SharedAudioSystem _audioSys = default!;
        [Dependency] private readonly UserInterfaceSystem _uiSys = default!;
        [Dependency] private readonly ResearchSystem _researchSys = default!;

        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<LatheComponent, InteractUsingEvent>(OnInteractUsing);
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
            if (TryComp<AppearanceComponent>(uid, out var appearance))
            {
                appearance.SetData(LatheVisuals.IsInserting, false);
                appearance.SetData(LatheVisuals.IsRunning, false);
            }

            //Fix this awful shit once Lathes get ECS'd.
            List<LatheRecipePrototype>? recipes = null;
            if (TryComp<ProtolatheDatabaseComponent>(uid, out var database))
                recipes = database.ProtolatheRecipes.ToList();
            else if (TryComp<LatheDatabaseComponent>(uid, out var database2))
                recipes = database2._recipes;

            if (recipes == null)
                return;

            foreach (var recipe in recipes)
            {
                foreach (var mat in recipe.RequiredMaterials)
                {
                    if (!component.MaterialWhiteList.Contains(mat.Key))
                        component.MaterialWhiteList.Add(mat.Key);
                }
            }
        }

        /// <summary>
        /// When someone tries to use an item on the lathe,
        /// insert it if it's a stack and fits inside
        /// </summary>
        private void OnInteractUsing(EntityUid uid, LatheComponent component, InteractUsingEvent args)
        {
            if (args.Handled)
                return;

            if (!TryComp<MaterialStorageComponent>(uid, out var storage)
                || !TryComp<MaterialComponent>(args.Used, out var material)
                || component.LatheWhitelist?.IsValid(args.Used) == false)
                return;

            args.Handled = true;

            var matUsed = false;
            foreach (var mat in material.Materials)
                if (component.MaterialWhiteList.Contains(mat.ID))
                    matUsed = true;

            if (!matUsed)
            {
                _popupSystem.PopupEntity(Loc.GetString("lathe-popup-material-not-used"), uid, Filter.Pvs(uid));
                return;
            }

            var multiplier = 1;

            if (TryComp<StackComponent>(args.Used, out var stack))
                multiplier = stack.Count;

            var totalAmount = 0;

            // Check if it can insert all materials.
            foreach (var (mat, vol) in material._materials)
            {
                if (!storage.CanInsertMaterial(mat,
                        vol * multiplier)) return;
                totalAmount += vol * multiplier;
            }

            // Check if it can take ALL of the material's volume.
            if (storage.StorageLimit > 0 && !storage.CanTakeAmount(totalAmount))
                return;

            var lastMat = string.Empty;
            foreach (var (mat, vol) in material._materials)
            {
                storage.InsertMaterial(mat, vol * multiplier);
                lastMat = mat;
            }

            // Play a sound when inserting, if any
            if (component.InsertingSound != null)
                _audioSys.PlayPvs(component.InsertingSound, uid);

            // We need the prototype to get the color
            _prototypeManager.TryIndex(lastMat, out MaterialPrototype? matProto);

            EntityManager.QueueDeleteEntity(args.Used);

            EnsureComp<LatheInsertingComponent>(uid).TimeRemaining = component.InsertionTime;

            _popupSystem.PopupEntity(Loc.GetString("machine-insert-item", ("machine", uid),
                ("item", args.Used)), uid, Filter.Entities(args.User));

            if (matProto != null)
            {
                UpdateInsertingAppearance(uid, true, matProto.Color);
            }
            UpdateInsertingAppearance(uid, true);
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
                storage.RemoveMaterial(material, amount);
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
            if (!TryComp<AppearanceComponent>(uid, out var appearance))
                return;

            appearance.SetData(LatheVisuals.IsRunning, isRunning);
        }

        /// <summary>
        /// Sets the machine sprite to play the inserting animation
        /// and sets the color of the inserted mat if applicable
        /// </summary>
        private void UpdateInsertingAppearance(EntityUid uid, bool isInserting, Color? color = null)
        {
            if (!TryComp<AppearanceComponent>(uid, out var appearance))
                return;

            appearance.SetData(LatheVisuals.IsInserting, isInserting);
            if (color != null)
                appearance.SetData(LatheVisuals.InsertingColor, color);
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
            if (!HasComp<MaterialStorageComponent>(uid)) return;

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
