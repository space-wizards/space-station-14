using Content.Server.UserInterface;
using Content.Shared.Lathe;
using Content.Shared.Research.Prototypes;
using Content.Shared.Whitelist;
using Robust.Server.GameObjects;
using Content.Shared.Sound;

namespace Content.Server.Lathe.Components
{
    [RegisterComponent]
    public sealed class LatheComponent : SharedLatheComponent
    {
<<<<<<< HEAD
        [Dependency] private readonly IEntityManager _entMan = default!;

        [DataField("whitelist")] private EntityWhitelist? _whitelist = null;

=======
        /// <summary>
        /// How much volume in cm^3 each sheet of material adds
        /// </summary>
        public int VolumePerSheet = 100;

        /// <summary>
        /// The lathe's construction queue
        /// </summary>
>>>>>>> d0a3044edd0e367ec199daa1a7f88bb6b94d2525
        [ViewVariables]
        public Queue<LatheRecipePrototype> Queue { get; } = new();
        /// <summary>
        /// The recipe the lathe is currently producing
        /// </summary>
        [ViewVariables]
        public LatheRecipePrototype? ProducingRecipe;
        /// <summary>
        /// How long the inserting animation will play
        /// </summary>
        [ViewVariables]
        public float InsertionTime = 0.79f; // 0.01 off for animation timing
        /// <summary>
        /// Update accumulator for the insertion time
        /// </suummary>
        public float InsertionAccumulator = 0f;
        /// <summary>
        /// Production accumulator for the production time.
        /// </summary>
        [ViewVariables]
<<<<<<< HEAD
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
        async Task<bool> IInteractUsing.InteractUsing(InteractUsingEventArgs eventArgs)
        {
            if (!_entMan.TryGetComponent(Owner, out MaterialStorageComponent? storage)
                ||  !_entMan.TryGetComponent(eventArgs.Using, out MaterialComponent? material)
                || _whitelist != null && !_whitelist.IsValid(eventArgs.Using)) return false;

            var multiplier = 1;

            if (_entMan.TryGetComponent(eventArgs.Using, out StackComponent? stack)) multiplier = stack.Count;

            var totalAmount = 0;

            // Check if it can insert all materials.
            foreach (var mat in material.MaterialIds)
            {
                if (!storage.CanInsertMaterial(mat, material.volume * multiplier)) return false;
                totalAmount += material.volume * multiplier;
            }

            // Check if it can take ALL of the material's volume.
            if (storage.CanTakeAmount(totalAmount)) return false;

            foreach (var mat in material.MaterialIds)
            {
                storage.InsertMaterial(mat, material.volume * multiplier);
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

        private enum LatheState : byte
        {
            Base,
            Inserting,
            Producing
        }
=======
        public float ProducingAccumulator = 0f;

        /// <summary>
        /// The sound that plays when the lathe is producing an item, if any
        /// </summary>
        [DataField("producingSound")]
        public SoundSpecifier? ProducingSound;

        /// <summmary>
        /// The lathe's UI.
        /// </summary>
        [ViewVariables] public BoundUserInterface? UserInterface;
>>>>>>> d0a3044edd0e367ec199daa1a7f88bb6b94d2525
    }
}
