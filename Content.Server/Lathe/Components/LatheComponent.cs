#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Content.Server.Materials;
using Content.Server.Power.Components;
using Content.Server.Research.Components;
using Content.Server.Stack;
using Content.Server.UserInterface;
using Content.Shared.Interaction;
using Content.Shared.Lathe;
using Content.Shared.Power;
using Content.Shared.Research.Prototypes;
using Robust.Server.GameObjects;
using Robust.Server.Player;
using Robust.Shared.GameObjects;
using Robust.Shared.ViewVariables;

namespace Content.Server.Lathe.Components
{
    [RegisterComponent]
    [ComponentReference(typeof(IActivate))]
    public class LatheComponent : SharedLatheComponent, IInteractUsing, IActivate
    {
        public const int VolumePerSheet = 100;

        [ViewVariables]
        public Queue<LatheRecipePrototype> Queue { get; } = new();

        [ViewVariables]
        public bool Producing { get; private set; }

        private LatheState _state = LatheState.Base;

        protected virtual LatheState State
        {
            get => _state;
            set => _state = value;
        }

        [ViewVariables]
        private LatheRecipePrototype? _producingRecipe;
        [ViewVariables]
        private bool Powered => !Owner.TryGetComponent(out PowerReceiverComponent? receiver) || receiver.Powered;

        private static readonly TimeSpan InsertionTime = TimeSpan.FromSeconds(0.9f);

        [ViewVariables] private BoundUserInterface? UserInterface => Owner.GetUIOrNull(LatheUiKey.Key);

        public override void Initialize()
        {
            base.Initialize();

            if (UserInterface != null)
            {
                UserInterface.OnReceiveMessage += UserInterfaceOnOnReceiveMessage;
            }
        }

        private void UserInterfaceOnOnReceiveMessage(ServerBoundUserInterfaceMessage message)
        {
            if (!Powered)
                return;

            switch (message.Message)
            {
                case LatheQueueRecipeMessage msg:
                    PrototypeManager.TryIndex(msg.ID, out LatheRecipePrototype? recipe);
                    if (recipe != null!)
                        for (var i = 0; i < msg.Quantity; i++)
                        {
                            Queue.Enqueue(recipe);
                            UserInterface?.SendMessage(new LatheFullQueueMessage(GetIdQueue()));
                        }
                    break;
                case LatheSyncRequestMessage _:
                    if (!Owner.HasComponent<MaterialStorageComponent>()) return;
                    UserInterface?.SendMessage(new LatheFullQueueMessage(GetIdQueue()));
                    if (_producingRecipe != null)
                        UserInterface?.SendMessage(new LatheProducingRecipeMessage(_producingRecipe.ID));
                    break;

                case LatheServerSelectionMessage _:
                    if (!Owner.TryGetComponent(out ResearchClientComponent? researchClient)) return;
                    researchClient.OpenUserInterface(message.Session);
                    break;

                case LatheServerSyncMessage _:
                    if (!Owner.TryGetComponent(out TechnologyDatabaseComponent? database)
                    || !Owner.TryGetComponent(out ProtolatheDatabaseComponent? protoDatabase)) return;

                    if (database.SyncWithServer())
                        protoDatabase.Sync();

                    break;
            }


        }

        internal bool Produce(LatheRecipePrototype recipe)
        {
            if (Producing || !Powered || !CanProduce(recipe) || !Owner.TryGetComponent(out MaterialStorageComponent? storage)) return false;

            UserInterface?.SendMessage(new LatheFullQueueMessage(GetIdQueue()));

            Producing = true;
            _producingRecipe = recipe;

            foreach (var (material, amount) in recipe.RequiredMaterials)
            {
                // This should always return true, otherwise CanProduce fucked up.
                storage.RemoveMaterial(material, amount);
            }

            UserInterface?.SendMessage(new LatheProducingRecipeMessage(recipe.ID));

            State = LatheState.Producing;
            SetAppearance(LatheVisualState.Producing);

            Owner.SpawnTimer(recipe.CompleteTime, () =>
            {
                Producing = false;
                _producingRecipe = null;
                Owner.EntityManager.SpawnEntity(recipe.Result, Owner.Transform.Coordinates);
                UserInterface?.SendMessage(new LatheStoppedProducingRecipeMessage());
                State = LatheState.Base;
                SetAppearance(LatheVisualState.Idle);
            });

            return true;
        }

        public void OpenUserInterface(IPlayerSession session)
        {
            UserInterface?.Open(session);
        }

        void IActivate.Activate(ActivateEventArgs eventArgs)
        {
            if (!eventArgs.User.TryGetComponent(out ActorComponent? actor))
                return;
            if (!Powered)
            {
                return;
            }

            OpenUserInterface(actor.PlayerSession);
        }

        async Task<bool> IInteractUsing.InteractUsing(InteractUsingEventArgs eventArgs)
        {
            if (!Owner.TryGetComponent(out MaterialStorageComponent? storage)
                ||  !eventArgs.Using.TryGetComponent(out MaterialComponent? material)) return false;

            var multiplier = 1;

            if (eventArgs.Using.TryGetComponent(out StackComponent? stack)) multiplier = stack.Count;

            var totalAmount = 0;

            // Check if it can insert all materials.
            foreach (var mat in material.MaterialIds)
            {
                // TODO: Change how MaterialComponent works so this is not hard-coded.
                if (!storage.CanInsertMaterial(mat, VolumePerSheet * multiplier)) return false;
                totalAmount += VolumePerSheet * multiplier;
            }

            // Check if it can take ALL of the material's volume.
            if (storage.CanTakeAmount(totalAmount)) return false;

            foreach (var mat in material.MaterialIds)
            {
                storage.InsertMaterial(mat, VolumePerSheet * multiplier);
            }

            State = LatheState.Inserting;
            switch (material.Materials.FirstOrDefault()?.ID)
            {
                case "Steel":
                    SetAppearance(LatheVisualState.InsertingMetal);
                    break;
                case "Glass":
                    SetAppearance(LatheVisualState.InsertingGlass);
                    break;
                case "Gold":
                    SetAppearance(LatheVisualState.InsertingGold);
                    break;
                case "Plastic":
                    SetAppearance(LatheVisualState.InsertingPlastic);
                    break;
                case "Plasma":
                    SetAppearance(LatheVisualState.InsertingPlasma);
                    break;
            }

            Owner.SpawnTimer(InsertionTime, () =>
            {
                State = LatheState.Base;
                SetAppearance(LatheVisualState.Idle);
            });

            eventArgs.Using.Delete();

            return true;
        }

        private void SetAppearance(LatheVisualState state)
        {
            if (Owner.TryGetComponent(out AppearanceComponent? appearance))
            {
                appearance.SetData(PowerDeviceVisuals.VisualState, state);
            }
        }

        private Queue<string> GetIdQueue()
        {
            var queue = new Queue<string>();
            foreach (var recipePrototype in Queue)
            {
                queue.Enqueue(recipePrototype.ID);
            }

            return queue;
        }

        protected enum LatheState
        {
            Base,
            Inserting,
            Producing
        }
    }
}
