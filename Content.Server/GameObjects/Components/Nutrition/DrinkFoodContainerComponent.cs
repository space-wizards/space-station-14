using System;
using System.Collections.Generic;
using System.Linq;
using Content.Server.GameObjects.Components.Sound;
using Content.Server.GameObjects.EntitySystems;
using Content.Shared.GameObjects.Components.Nutrition;
using Robust.Server.GameObjects;
using Robust.Server.GameObjects.Components.Container;
using Robust.Server.Interfaces.GameObjects;
using Robust.Shared.GameObjects;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Interfaces.Random;
using Robust.Shared.IoC;
using Robust.Shared.Serialization;
using Robust.Shared.ViewVariables;

namespace Content.Server.GameObjects.Components.Nutrition
{
    [RegisterComponent]
    public sealed class DrinkFoodContainerComponent : SharedDrinkFoodContainerComponent, IMapInit, IUse
    {
        public override string Name => "DrinkFoodContainer";
        private string _useSound;
        private Container _foodContainer;
        // Optimisation scabbed from BallisticMagazineComponent to avoid loading entities until needed
        [ViewVariables] private readonly Stack<IEntity> _loadedFood = new Stack<IEntity>();
        private AppearanceComponent _appearanceComponent;
        [ViewVariables] public int Count => _availableSpawnCount + _loadedFood.Count;
        private int _availableSpawnCount;
        private Dictionary<string, int> _prototypes;
        private string _finishPrototype;
        public int Capacity => _capacity;
        private int _capacity;

        public override void ExposeData(ObjectSerializer serializer)
        {
            base.ExposeData(serializer);
            serializer.DataField(ref _useSound, "use_sound", null);
            // Is a dictionary for stuff with probabilities (mainly donut box)
            serializer.DataField(ref _prototypes, "prototypes", null);
            // If you want the final item to be different e.g. trash
            serializer.DataField(ref _finishPrototype, "spawn_on_finish", null);
            serializer.DataField(ref _availableSpawnCount, "available_spawn_count", 0);
            serializer.DataField(ref _capacity, "capacity", 5);
        }

        public override void Initialize()
        {
            base.Initialize();
            Owner.TryGetComponent(out AppearanceComponent appearance);
            _appearanceComponent = appearance;
            if (_prototypes == null)
            {
                throw new NullReferenceException();
            }

            if (_prototypes.Sum(x => x.Value) != 100)
            {
                throw new ArgumentOutOfRangeException();
            }
        }

        public void MapInit()
        {
            _availableSpawnCount = Capacity;
        }

        protected override void Startup()
        {
            base.Startup();

            _foodContainer =
                ContainerManagerComponent.Ensure<Container>("food_container", Owner, out var existed);

            if (existed)
            {
                foreach (var entity in _foodContainer.ContainedEntities)
                {
                    _loadedFood.Push(entity);
                }
            }

            _updateAppearance();
            _appearanceComponent?.SetData(DrinkFoodContainerVisuals.Capacity, Capacity);
        }

        bool IUse.UseEntity(UseEntityEventArgs eventArgs)
        {
            // TODO: Potentially change this depending upon desired functionality
            IEntity item = TakeItem(eventArgs.User);
            if (item == null)
            {
                return false;
            }
            if (item.TryGetComponent(out ItemComponent itemComponent) &&
                eventArgs.User.TryGetComponent(out HandsComponent handsComponent))
            {
                if (handsComponent.CanPutInHand(itemComponent))
                {
                    handsComponent.PutInHand(itemComponent);
                    return true;
                }
            }

            item.Transform.GridPosition = eventArgs.User.Transform.GridPosition;
            return true;
        }

        // TODO: Somewhat shitcode
        // Tried using .Prob() buuuuuttt trying that for each item wouldn't work.
        private string _getProbItem(Dictionary<string, int> prototypes)
        {
            if (prototypes.Count == 1)
            {
                return prototypes.Keys.ToList()[0];
            }
            var prob = IoCManager.Resolve<IRobustRandom>();
            var result = prob.Next(0, 100);
            var runningTotal = 0;
            foreach (var item in prototypes)
            {
                runningTotal += item.Value;
                if (result < runningTotal)
                {
                    return item.Key;
                }

            }
            throw new Exception();
        }

        public IEntity TakeItem(IEntity user)
        {
            if (_useSound != null)
            {
                if (Owner.TryGetComponent(out SoundComponent soundComponent) && _useSound != null)
                {
                    soundComponent.Play(_useSound);
                }
            }
            IEntity item = null;
            if (_loadedFood.Count > 0)
            {
                item = _loadedFood.Pop();
                _foodContainer.Remove(item);
            }

            if (_availableSpawnCount > 0)
            {
                var prototypeName = _getProbItem(_prototypes);
                item = Owner.EntityManager.SpawnEntity(prototypeName, Owner.Transform.GridPosition);
                _availableSpawnCount -= 1;

            }

            _tryDelete(user);
            _updateAppearance();

            return item;
        }

        private void _tryDelete(IEntity user)
        {
            if (Count <= 0)
            {
                // Ideally this takes priority to be put into inventory rather than the desired item
                if (_finishPrototype != null)
                {
                    var item = Owner.EntityManager.SpawnEntity(_finishPrototype, Owner.Transform.GridPosition);
                    item.Transform.GridPosition = Owner.Transform.GridPosition;
                    Owner.Delete();
                    if (user.TryGetComponent(out HandsComponent handsComponent) &&
                        item.TryGetComponent(out ItemComponent itemComponent))
                    {
                        if (handsComponent.CanPutInHand(itemComponent))
                        {
                            handsComponent.PutInHand(itemComponent);
                        }
                    }
                    return;
                }
                Owner.Delete();
            }
        }

        private void _updateAppearance()
        {
            _appearanceComponent?.SetData(DrinkFoodContainerVisuals.Current, Count);
        }
    }
}
