using Content.Server.Lathe.Components;
using Content.Shared.Lathe;
using Content.Shared.Research.Prototypes;
using Content.Server.Research.Components;
using JetBrains.Annotations;
using Content.Shared.Interaction;
using Content.Server.Materials;
using Content.Server.Power.Components;
using Content.Server.Stack;
using Robust.Server.GameObjects;
using Robust.Shared.Prototypes;

namespace Content.Server.Lathe
{
    [UsedImplicitly]
    public sealed class LatheSystem : EntitySystem
    {
        [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<LatheComponent, InteractUsingEvent>(OnInteractUsing);
            SubscribeLocalEvent<LatheComponent, ComponentInit>(OnComponentInit);
        }

        // These queues are to add/remove COMPONENTS to the lathes
        private Queue<EntityUid> ProducingAddQueue = new();
        private Queue<EntityUid> ProducingRemoveQueue = new();
        private Queue<EntityUid> InsertingAddQueue = new();
        private Queue<EntityUid> InsertingRemoveQueue = new();

        public override void Update(float frameTime)
        {
            foreach (var uid in ProducingAddQueue)
                EnsureComp<LatheProducingComponent>(uid);
            ProducingAddQueue.Clear();
            foreach (var uid in ProducingRemoveQueue)
                RemComp<LatheProducingComponent>(uid);
            ProducingRemoveQueue.Clear();
            foreach (var uid in InsertingAddQueue)
                EnsureComp<LatheInsertingComponent>(uid);
            InsertingAddQueue.Clear();
            foreach (var uid in InsertingRemoveQueue)
                RemComp<LatheInsertingComponent>(uid);
            InsertingRemoveQueue.Clear();

            foreach (var comp in EntityManager.EntityQuery<LatheComponent>())
            {
                if (!HasComp<LatheProducingComponent>(comp.Owner) && comp.Queue.Count > 0)
                {
                    Produce(comp, comp.Queue.Dequeue());
                }
            }

            foreach (var (insertingComp, lathe) in EntityQuery<LatheInsertingComponent, LatheComponent>(false))
            {
                if (lathe.InsertionAccumulator < lathe.InsertionTime)
                {
                    lathe.InsertionAccumulator += frameTime;
                    continue;
                }
                lathe.InsertionAccumulator = 0;
                UpdateInsertingAppearance(lathe.Owner, false);
                InsertingRemoveQueue.Enqueue(lathe.Owner);
            }

            foreach (var (producingComp, lathe) in EntityQuery<LatheProducingComponent, LatheComponent>(false))
            {
                if (lathe.ProducingRecipe == null)
                    continue;
                if (lathe.ProducingAccumulator < lathe.ProductionTime)
                {
                    lathe.ProducingAccumulator += frameTime;
                    continue;
                }
                lathe.ProducingAccumulator = 0;

                FinishProducing(lathe.ProducingRecipe, lathe);
            }
        }

        private void OnComponentInit(EntityUid uid, LatheComponent component, ComponentInit args)
        {
            if (component.UserInterface != null)
            {
                component.UserInterface.OnReceiveMessage += msg => UserInterfaceOnOnReceiveMessage(uid, component, msg);
            }

            if (!TryComp<AppearanceComponent>(uid, out var appearance))
                return;
            appearance.SetData(LatheVisuals.IsInserting, false);
            appearance.SetData(LatheVisuals.IsRunning, false);
        }

        private void OnInteractUsing(EntityUid uid, LatheComponent component, InteractUsingEvent args)
        {
            if (!TryComp<MaterialStorageComponent>(uid, out var storage) || !TryComp<MaterialComponent>(args.Used, out var material))
                return;

            var multiplier = 1;

            if (TryComp<StackComponent>(args.Used, out var stack))
                multiplier = stack.Count;

            var totalAmount = 0;

            // Check if it can insert all materials.
            foreach (var mat in material.MaterialIds)
            {
                // TODO: Change how MaterialComponent works so this is not hard-coded.
                if (!storage.CanInsertMaterial(mat, component.VolumePerSheet * multiplier))
                    return;
                totalAmount += component.VolumePerSheet * multiplier;
            }

            // Check if it can take ALL of the material's volume.
            if (storage.StorageLimit != -1 && !storage.CanTakeAmount(totalAmount))
                return;

            foreach (var mat in material.MaterialIds)
            {
                storage.InsertMaterial(mat, component.VolumePerSheet * multiplier);
            }

            EntityManager.QueueDeleteEntity(args.Used);
            InsertingAddQueue.Enqueue(uid);
            UpdateInsertingAppearance(uid, true);

            args.Handled = true;
        }

        internal bool Produce(LatheComponent component, LatheRecipePrototype recipe)
        {
            if (HasComp<LatheProducingComponent>(component.Owner) || !component.CanProduce(recipe)
                || !TryComp(component.Owner, out MaterialStorageComponent? storage))
                return false;

            if (TryComp<ApcPowerReceiverComponent>(component.Owner, out var receiver) && !receiver.Powered)
                return false;

            component.UserInterface?.SendMessage(new LatheFullQueueMessage(GetIdQueue(component)));

            component.ProducingRecipe = recipe;

            foreach (var (material, amount) in recipe.RequiredMaterials)
            {
                // This should always return true, otherwise CanProduce fucked up.
                storage.RemoveMaterial(material, amount);
            }

            component.UserInterface?.SendMessage(new LatheProducingRecipeMessage(recipe.ID));
            UpdateRunningAppearance(component.Owner, true);
            ProducingAddQueue.Enqueue(component.Owner);
            return true;
        }

        private void FinishProducing(LatheRecipePrototype recipe, LatheComponent component)
        {
            component.ProducingRecipe = null;
            EntityManager.SpawnEntity(recipe.Result, Comp<TransformComponent>(component.Owner).Coordinates);
            component.UserInterface?.SendMessage(new LatheStoppedProducingRecipeMessage());
            ProducingRemoveQueue.Enqueue(component.Owner);
            UpdateRunningAppearance(component.Owner, false);
        }

        private void UpdateRunningAppearance(EntityUid uid, bool isRunning)
        {
            if (!TryComp<AppearanceComponent>(uid, out var appearance))
                return;

            appearance.SetData(LatheVisuals.IsRunning, isRunning);
        }

        private void UpdateInsertingAppearance(EntityUid uid, bool isInserting)
        {
            if (!TryComp<AppearanceComponent>(uid, out var appearance))
                return;

            appearance.SetData(LatheVisuals.IsInserting, isInserting);
        }
        private void UserInterfaceOnOnReceiveMessage(EntityUid uid, LatheComponent component, ServerBoundUserInterfaceMessage message)
        {
            if (TryComp<ApcPowerReceiverComponent>(uid, out var receiver) && !receiver.Powered)
                return;

            switch (message.Message)
            {
                case LatheQueueRecipeMessage msg:
                    _prototypeManager.TryIndex(msg.ID, out LatheRecipePrototype? recipe);
                    if (recipe != null!)
                        for (var i = 0; i < msg.Quantity; i++)
                        {
                            component.Queue.Enqueue(recipe);
                            component.UserInterface?.SendMessage(new LatheFullQueueMessage(GetIdQueue(component)));
                        }
                    break;
                case LatheSyncRequestMessage _:
                    if (!HasComp<MaterialStorageComponent>(uid)) return;
                    component.UserInterface?.SendMessage(new LatheFullQueueMessage(GetIdQueue(component)));
                    if (component.ProducingRecipe != null)
                        component.UserInterface?.SendMessage(new LatheProducingRecipeMessage(component.ProducingRecipe.ID));
                    break;

                case LatheServerSelectionMessage _:
                    if (!TryComp(uid, out ResearchClientComponent? researchClient)) return;
                    researchClient.OpenUserInterface(message.Session);
                    break;

                case LatheServerSyncMessage _:
                    if (!TryComp(uid, out TechnologyDatabaseComponent? database)
                    || !TryComp(uid, out ProtolatheDatabaseComponent? protoDatabase)) return;

                    if (database.SyncWithServer())
                        protoDatabase.Sync();

                    break;
            }
        }
        private Queue<string> GetIdQueue(LatheComponent lathe)
        {
            var queue = new Queue<string>();
            foreach (var recipePrototype in lathe.Queue)
            {
                queue.Enqueue(recipePrototype.ID);
            }
            return queue;
        }
    }
}
