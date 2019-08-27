using System.Collections.Generic;
using Content.Server.GameObjects.Components.Stack;
using Content.Server.GameObjects.EntitySystems;
using Content.Shared.GameObjects.Components.Materials;
using Content.Shared.GameObjects.Components.Research;
using Content.Shared.Research;
using Robust.Server.GameObjects.Components.UserInterface;
using Robust.Server.Interfaces.GameObjects;
using Robust.Shared.GameObjects;
using Robust.Shared.GameObjects.Components.UserInterface;
using Robust.Shared.Timers;
using Robust.Shared.Utility;
using Robust.Shared.ViewVariables;

namespace Content.Server.GameObjects.Components.Research
{
    [RegisterComponent]
    [ComponentReference(typeof(IActivate))]
    public class LatheComponent : SharedLatheComponent, IAttackBy, IActivate
    {
        public const int VolumePerSheet = 3750;

        private BoundUserInterface _userInterface;

        [ViewVariables]
        public Queue<LatheRecipePrototype> Queue { get; } = new Queue<LatheRecipePrototype>();

        [ViewVariables]
        public bool Producing { get; private set; } = false;

        private LatheRecipePrototype _producingRecipe = null;

        public override void Initialize()
        {
            base.Initialize();
            _userInterface = Owner.GetComponent<ServerUserInterfaceComponent>().GetBoundUserInterface(LatheUiKey.Key);
            _userInterface.OnReceiveMessage += UserInterfaceOnOnReceiveMessage;
        }

        private void UserInterfaceOnOnReceiveMessage(ServerBoundUserInterfaceMessage serverMsg)
        {
            var message = serverMsg.Message;
            switch (message)
            {
                case LatheQueueRecipeMessage msg:
                    _prototypeManager.TryIndex(msg.ID, out LatheRecipePrototype recipe);
                    if (recipe != null)
                        for (var i = 0; i < msg.Quantity; i++)
                        {
                            Queue.Enqueue(recipe);
                            _userInterface.SendMessage(new LatheFullQueueMessage(GetIDQueue()));
                        }
                    break;
                case LatheSyncRequestMessage msg:
                    if (!Owner.TryGetComponent(out MaterialStorageComponent storage)) return;
                    _userInterface.SendMessage(new LatheFullQueueMessage(GetIDQueue()));
                    if (_producingRecipe != null)
                        _userInterface.SendMessage(new LatheProducingRecipeMessage(_producingRecipe.ID));
                    break;
            }
        }

        internal bool Produce(LatheRecipePrototype recipe)
        {
            if (Producing || !CanProduce(recipe) || !Owner.TryGetComponent(out MaterialStorageComponent storage)) return false;

            _userInterface.SendMessage(new LatheFullQueueMessage(GetIDQueue()));

            Producing = true;
            _producingRecipe = recipe;

            foreach (var (material, amount) in recipe.RequiredMaterials)
            {
                // This should always return true, otherwise CanProduce fucked up.
                storage.RemoveMaterial(material, amount);
            }

            _userInterface.SendMessage(new LatheProducingRecipeMessage(recipe.ID));

            Timer.Spawn(recipe.CompleteTime, () =>
            {
                Producing = false;
                _producingRecipe = null;
                Owner.EntityManager.SpawnEntityAt(recipe.Result, Owner.Transform.GridPosition);
                _userInterface.SendMessage(new LatheStoppedProducingRecipeMessage());
            });

            return true;
        }

        void IActivate.Activate(ActivateEventArgs eventArgs)
        {
            if (!eventArgs.User.TryGetComponent(out IActorComponent actor))
                return;

            _userInterface.Open(actor.playerSession);
            return;
        }
        bool IAttackBy.AttackBy(AttackByEventArgs eventArgs)
        {
            if (!Owner.TryGetComponent(out MaterialStorageComponent storage)
            ||  !eventArgs.AttackWith.TryGetComponent(out MaterialComponent material)) return false;

            var multiplier = 1;

            if (eventArgs.AttackWith.TryGetComponent(out StackComponent stack)) multiplier = stack.Count;

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

            eventArgs.AttackWith.Delete();

            return false;
        }

        private Queue<string> GetIDQueue()
        {
            var queue = new Queue<string>();
            foreach (var recipePrototype in Queue)
            {
                queue.Enqueue(recipePrototype.ID);
            }

            return queue;
        }
    }
}
