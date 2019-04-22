using System.Collections.Generic;
using Content.Server.GameObjects.Components.Stack;
using Content.Server.GameObjects.EntitySystems;
using Content.Shared.GameObjects.Components.Materials;
using Content.Shared.GameObjects.Components.Research;
using Content.Shared.Research;
using Robust.Server.GameObjects;
using Robust.Shared.GameObjects;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Interfaces.GameObjects.Components;
using Robust.Shared.Interfaces.Network;
using Robust.Shared.Interfaces.Timers;
using Robust.Shared.IoC;
using Robust.Shared.Prototypes;
using Robust.Shared.Timers;
using Robust.Shared.Utility;
using Robust.Shared.ViewVariables;

namespace Content.Server.GameObjects.Components.Research
{
    public class LatheComponent : SharedLatheComponent, IAttackHand, IAttackBy, IActivate
    {
        public const int VolumePerSheet = 3750;

        [Dependency]
#pragma warning disable CS0649
        private IPrototypeManager _prototypeManager;
#pragma warning restore

        [ViewVariables]
        public Queue<LatheRecipePrototype> Queue => _queue;
        private readonly Queue<LatheRecipePrototype> _queue = new Queue<LatheRecipePrototype>();

        [ViewVariables]
        public bool Producing => _producing;
        private bool _producing = false;

        private LatheRecipePrototype _producingRecipe = null;

        internal bool Produce(LatheRecipePrototype recipe)
        {
            SendNetworkMessage(new LatheFullQueueMessage(GetIDQueue()));
            if (!CanProduce(recipe)) return false;
            Owner.TryGetComponent(out MaterialStorageComponent storage);

            if (storage == null) return false;

            _producing = true;
            _producingRecipe = recipe;

            foreach (var (material, amount) in recipe.RequiredMaterials)
            {
                // This should always return true, otherwise CanProduce fucked up.
                storage.RemoveMaterial(material, amount);
            }

            SendNetworkMessage(new LatheProducingRecipeMessage(recipe.ID));

            Timer.Spawn(recipe.CompleteTime, () =>
            {
                _producing = false;
                _producingRecipe = null;
                var transform = Owner.GetComponent<ITransformComponent>();
                Owner.EntityManager.TrySpawnEntityAt(recipe.Result, transform.GridPosition, out var entity);
                SendNetworkMessage(new LatheStoppedProducingRecipeMessage());
            });

            return true;
        }

        void IActivate.Activate(ActivateEventArgs eventArgs)
        {
            eventArgs.User.TryGetComponent(out BasicActorComponent actor);

            if (actor == null) return;

            SendNetworkMessage(new LatheMenuOpenMessage(), actor.playerSession?.ConnectedClient);
        }

        bool IAttackHand.AttackHand(AttackHandEventArgs eventArgs)
        {
            eventArgs.User.TryGetComponent(out BasicActorComponent actor);

            if (actor == null) return false;

            SendNetworkMessage(new LatheMenuOpenMessage(), actor.playerSession?.ConnectedClient);

            return false;
        }

        bool IAttackBy.AttackBy(AttackByEventArgs eventArgs)
        {
            Owner.TryGetComponent(out MaterialStorageComponent storage);
            eventArgs.AttackWith.TryGetComponent(out MaterialComponent material);
            eventArgs.AttackWith.TryGetComponent(out StackComponent stack);

            if (storage == null || material == null) return false;

            var mult = 1;

            if (stack != null) mult = stack.Count;

            int totalAmount = 0;

            // Check if it can insert all materials.
            foreach (var mat in material.MaterialTypes.Values)
            {
                // 1000cm3 per material.
                // TODO: Change how MaterialComponent works so this is not hard-coded.
                if (!storage.CanInsertMaterial(mat.ID, VolumePerSheet * mult)) return false;
                totalAmount += VolumePerSheet * mult;
            }

            // Check if it can take ALL of the material's volume.
            if (storage.CanTakeAmount(totalAmount)) return false;

            foreach (var mat in material.MaterialTypes.Values)
            {

                storage.InsertMaterial(mat.ID, VolumePerSheet * mult);
            }

            eventArgs.AttackWith.Delete();

            return false;
        }

        private Queue<string> GetIDQueue()
        {
            var queue = new Queue<string>();
            foreach (var recipePrototype in _queue)
            {
                queue.Enqueue(recipePrototype.ID);
            }

            return queue;
        }

        public override void HandleMessage(ComponentMessage message, INetChannel netChannel = null, IComponent component = null)
        {
            switch (message)
            {
                case LatheQueueRecipeMessage msg:
                    _prototypeManager.TryIndex(msg.ID, out LatheRecipePrototype recipe);
                    if (recipe != null)
                        for (var i = 0; i < msg.Quantity; i++)
                        {
                            _queue.Enqueue(recipe);
                            SendNetworkMessage(new LatheFullQueueMessage(GetIDQueue()));
                        }
                    break;
                case LatheSyncRequestMessage msg:
                    if (netChannel == null) break;
                    Owner.TryGetComponent(out MaterialStorageComponent storage);
                    SendNetworkMessage(new LatheFullQueueMessage(GetIDQueue()), netChannel);
                    if (_producingRecipe != null)
                        SendNetworkMessage(new LatheProducingRecipeMessage(_producingRecipe.ID), netChannel);
                    storage.Update(netChannel);
                    break;
            }
        }
    }
}
