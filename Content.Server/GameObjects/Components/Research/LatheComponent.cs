#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Content.Server.GameObjects.Components.Power.ApcNetComponents;
using Content.Server.GameObjects.Components.Stack;
using Content.Server.Utility;
using Content.Shared.GameObjects.Components.Materials;
using Content.Shared.GameObjects.Components.Power;
using Content.Shared.GameObjects.Components.Research;
using Content.Shared.Interfaces.GameObjects.Components;
using Content.Shared.Research;
using Robust.Server.GameObjects;
using Robust.Server.GameObjects.Components.UserInterface;
using Robust.Server.Interfaces.GameObjects;
using Robust.Server.Interfaces.Player;
using Robust.Shared.GameObjects;
using Robust.Shared.Timers;
using Robust.Shared.ViewVariables;

namespace Content.Server.GameObjects.Components.Research
{
    [RegisterComponent]
    [ComponentReference(typeof(IActivate))]
    public class LatheComponent : SharedLatheComponent, IInteractUsing, IActivate
    {
        public const int VolumePerSheet = 3750;

        [ViewVariables]
        public Queue<LatheRecipePrototype> Queue { get; } = new Queue<LatheRecipePrototype>();

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
                    PrototypeManager.TryIndex(msg.ID, out LatheRecipePrototype recipe);
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
        {   if(!Powered)
            {
                return false;
            }
            if (Producing || !CanProduce(recipe) || !Owner.TryGetComponent(out MaterialStorageComponent? storage)) return false;

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

            Timer.Spawn(recipe.CompleteTime, () =>
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
            if (!eventArgs.User.TryGetComponent(out IActorComponent? actor))
                return;
            if (!Powered)
            {
                return;
            }

            OpenUserInterface(actor.playerSession);
        }

        async Task<bool> IInteractUsing.InteractUsing(InteractUsingEventArgs eventArgs)
        {
            if (!Owner.TryGetComponent(out MaterialStorageComponent? storage)
                ||  !eventArgs.Using.TryGetComponent(out MaterialComponent? material)) return false;

            var multiplier = 1;

            if (eventArgs.Using.TryGetComponent(out StackComponent? stack)) multiplier = stack.Count;

            var totalAmount = 0;

            // Check if it can insert all materials.
            foreach (var mat in material.MaterialTypes.Values)
            {
                // TODO: Change how MaterialComponent works so this is not hard-coded.
                if (!storage.CanInsertMaterial(mat.ID, VolumePerSheet * multiplier)) return false;
                totalAmount += VolumePerSheet * multiplier;
            }

            // Check if it can take ALL of the material's volume.
            if (storage.CanTakeAmount(totalAmount)) return false;

            foreach (var mat in material.MaterialTypes.Values)
            {
                storage.InsertMaterial(mat.ID, VolumePerSheet * multiplier);
            }

            State = LatheState.Inserting;
            switch (material.MaterialTypes.Values.First().Name)
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
            }

            Timer.Spawn(InsertionTime, () =>
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
