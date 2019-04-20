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

namespace Content.Server.GameObjects.Components.Research
{
    public class LatheComponent : SharedLatheComponent, IAttackHand, IAttackBy, IActivate
    {
        [Dependency]
        private IPrototypeManager _prototypeManager;

        public Queue<LatheRecipePrototype> Queue => _queue;
        private readonly Queue<LatheRecipePrototype> _queue = new Queue<LatheRecipePrototype>();

        public bool Producing => _producing;
        private bool _producing = false;


        internal bool Produce(LatheRecipePrototype recipe)
        {
            SendNetworkMessage(new LatheFullQueueMessage(GetIDQueue()));
            if (!CanProduce(recipe)) return false;
            Owner.TryGetComponent(out MaterialStorageComponent storage);

            if (storage == null) return false;

            _producing = true;

            foreach (var (material, amount) in recipe.RequiredMaterials)
            {
                // This should always return true, otherwise CanProduce fucked up.
                storage.RemoveMaterial(material, amount);
            }

            SendNetworkMessage(new LatheProducingRecipeMessage(recipe.ID));

            Timer.Spawn(recipe.CompleteTime, () =>
            {
                _producing = false;
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

            foreach (var mat in material.MaterialTypes.Values)
            {

                storage.InsertMaterial(mat.ID, 1000 * mult);
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
            }
        }
    }
}
