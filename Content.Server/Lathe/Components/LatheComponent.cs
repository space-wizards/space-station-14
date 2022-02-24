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
using Robust.Shared.IoC;
using Robust.Shared.ViewVariables;

namespace Content.Server.Lathe.Components
{
    [RegisterComponent]
    [ComponentReference(typeof(IActivate))]
    public sealed class LatheComponent : SharedLatheComponent, IInteractUsing, IActivate
    {
        [Dependency] private readonly IEntityManager _entMan = default!;

        public const int VolumePerSheet = 100;

        [ViewVariables]
        public Queue<LatheRecipePrototype> Queue { get; } = new();

        [ViewVariables]
        public bool Producing { get; private set; }

        private LatheState _state = LatheState.Base;

        protected LatheState State
        {
            get => _state;
            set => _state = value;
        }

        [ViewVariables]
        private LatheRecipePrototype? _producingRecipe;
        [ViewVariables]
        private bool Powered => !_entMan.TryGetComponent(Owner, out ApcPowerReceiverComponent? receiver) || receiver.Powered;

        private static readonly TimeSpan InsertionTime = TimeSpan.FromSeconds(0.9f);

        [ViewVariables] private BoundUserInterface? UserInterface => Owner.GetUIOrNull(LatheUiKey.Key);

        protected override void Initialize()
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
                    if (!_entMan.HasComponent<MaterialStorageComponent>(Owner)) return;
                    UserInterface?.SendMessage(new LatheFullQueueMessage(GetIdQueue()));
                    if (_producingRecipe != null)
                        UserInterface?.SendMessage(new LatheProducingRecipeMessage(_producingRecipe.ID));
                    break;

                case LatheServerSelectionMessage _:
                    if (!_entMan.TryGetComponent(Owner, out ResearchClientComponent? researchClient)) return;
                    researchClient.OpenUserInterface(message.Session);
                    break;

                case LatheServerSyncMessage _:
                    if (!_entMan.TryGetComponent(Owner, out TechnologyDatabaseComponent? database)
                    || !_entMan.TryGetComponent(Owner, out ProtolatheDatabaseComponent? protoDatabase)) return;

                    if (database.SyncWithServer())
                        protoDatabase.Sync();

                    break;
            }


        }

        internal bool Produce(LatheRecipePrototype recipe)
        {
            if (Producing || !Powered || !CanProduce(recipe) || !_entMan.TryGetComponent(Owner, out MaterialStorageComponent? storage)) return false;

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
                _entMan.SpawnEntity(recipe.Result, _entMan.GetComponent<TransformComponent>(Owner).Coordinates);
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
            if (!_entMan.TryGetComponent(eventArgs.User, out ActorComponent? actor))
                return;
            if (!Powered)
            {
                return;
            }

            OpenUserInterface(actor.PlayerSession);
        }

        async Task<bool> IInteractUsing.InteractUsing(InteractUsingEventArgs eventArgs)
        {
            if (!_entMan.TryGetComponent(Owner, out MaterialStorageComponent? storage)
                ||  !_entMan.TryGetComponent(eventArgs.Using, out MaterialComponent? material)) return false;

            var multiplier = 1;

            if (_entMan.TryGetComponent(eventArgs.Using, out StackComponent? stack)) multiplier = stack.Count;

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

            _entMan.DeleteEntity(eventArgs.Using);

            return true;
        }

        private void SetAppearance(LatheVisualState state)
        {
            if (_entMan.TryGetComponent(Owner, out AppearanceComponent? appearance))
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
