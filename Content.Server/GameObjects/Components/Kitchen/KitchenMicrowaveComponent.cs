using System.Collections.Generic;
using System.Linq;
using Content.Server.GameObjects.EntitySystems;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.ViewVariables;
using Content.Server.GameObjects.Components.Chemistry;
using Content.Server.GameObjects.Components.Nutrition;
using Content.Shared.Chemistry;
using Robust.Shared.Serialization;
using Robust.Shared.Interfaces.GameObjects;
using Content.Shared.Prototypes.Kitchen;
using Content.Shared.Kitchen;
using Robust.Shared.Timers;
using Robust.Server.GameObjects;
using Content.Shared.GameObjects.Components.Power;
using Robust.Server.GameObjects.EntitySystems;
using Robust.Server.GameObjects.Components.Container;
using Content.Server.GameObjects.Components.Power;
using Robust.Server.GameObjects.Components.UserInterface;
using Robust.Server.Interfaces.GameObjects;
using Robust.Shared.Prototypes;

namespace Content.Server.GameObjects.Components.Kitchen
{
    [RegisterComponent]
    [ComponentReference(typeof(IActivate))]
    public class KitchenMicrowaveComponent : SharedMicrowaveComponent, IActivate, IAttackBy, ISolutionChange
    {
#pragma warning disable 649
        [Dependency] private readonly IEntitySystemManager _entitySystemManager;
        [Dependency] private readonly IEntityManager _entityManager;
        [Dependency] private readonly RecipeManager _recipeManager;
        [Dependency] private readonly IPrototypeManager _prototypeManager;
#pragma warning restore 649

        private int _cookTimeDefault;
        private int _cookTimeMultiplier; //For upgrades and stuff I guess?
        private string _badRecipeName;
        [ViewVariables]
        private SolutionComponent _contents;

        [ViewVariables]
        public bool _busy = false;

        private bool Powered => _powerDevice.Powered;

        private bool HasContents => _contents.ReagentList.Count > 0 || _entityContents.Count > 0;

        private AppearanceComponent _appearance;

        private AudioSystem _audioSystem;

        private PowerDeviceComponent _powerDevice;

        private Container _storage;

        private Dictionary<string, int> _entityContents;

        private BoundUserInterface _userInterface;
        void ISolutionChange.SolutionChanged(SolutionChangeEventArgs eventArgs) => UpdateUserInterface();
        public override void ExposeData(ObjectSerializer serializer)
        {
            base.ExposeData(serializer);
            serializer.DataField(ref _badRecipeName, "failureResult", "FoodBadRecipe");
            serializer.DataField(ref _cookTimeDefault, "cookTime", 5);
            serializer.DataField(ref _cookTimeMultiplier, "cookTimeMultiplier", 1000);
        }

        public override void Initialize()
        {
            base.Initialize();
            _contents ??= Owner.TryGetComponent(out SolutionComponent solutionComponent)
                ? solutionComponent
                : Owner.AddComponent<SolutionComponent>();

            _storage = ContainerManagerComponent.Ensure<Container>("microwave_entity_container", Owner, out var existed);
            _appearance = Owner.GetComponent<AppearanceComponent>();
            _powerDevice = Owner.GetComponent<PowerDeviceComponent>();
            _audioSystem = _entitySystemManager.GetEntitySystem<AudioSystem>();
            _userInterface = Owner.GetComponent<ServerUserInterfaceComponent>()
                .GetBoundUserInterface(MicrowaveUiKey.Key);
            _entityContents = new Dictionary<string, int>();
            _userInterface.OnReceiveMessage += UserInterfaceOnReceiveMessage;

        }

        private void UserInterfaceOnReceiveMessage(ServerBoundUserInterfaceMessage message)
        {
            if (!Powered || _busy) return;

            switch (message.Message)
            {
                case MicrowaveStartCookMessage msg :
                    if (!HasContents) return;
                    UpdateUserInterface();
                    wzhzhzh();
                    break;

                case MicrowaveEjectMessage msg :
                    if (!HasContents) return;
                    DestroyReagents();
                    EjectSolids();
                    UpdateUserInterface();
                    break;
            }

        }


        void IActivate.Activate(ActivateEventArgs eventArgs)
        {
            if (!eventArgs.User.TryGetComponent(out IActorComponent actor))
                return;
            if (!Powered) return;
            UpdateUserInterface();
            _userInterface.Open(actor.playerSession);

        }

        public bool AttackBy(AttackByEventArgs eventArgs)
        {
            var itemEntity = eventArgs.User.GetComponent<HandsComponent>().GetActiveHand.Owner;
            if (itemEntity.TryGetComponent(typeof(FoodComponent), out var food))
            {
                if (_entityContents.TryGetValue(itemEntity.Prototype.ID, out var  quantity) && quantity > 0)
                {
                    quantity++;
                    food.Owner.Delete();
                    UpdateUserInterface();
                    return true;
                }
                else
                {
                    _storage.Insert(food.Owner);
                }

                _entityContents.Add(itemEntity.Prototype.ID, 1);
                UpdateUserInterface();
                return true;
            }

            return false;
        }

        //This is required.
        private void wzhzhzh()
        {
            _busy = true;
            foreach(var r in _recipeManager.Recipes)
            {

                var success = CanSatisfyRecipe(r);
                SetAppearance(MicrowaveVisualState.Cooking);
                _audioSystem.Play("/Audio/machines/microwave_start_beep.ogg");
                var time = success ? r._cookTime : _cookTimeDefault;
                Timer.Spawn(time * _cookTimeMultiplier, () =>
                {

                    if (success)
                    {
                        SubtractContents(r);
                    }
                    else
                    {
                        DestroyReagents();
                        EjectSolids();
                    }

                    var entityToSpawn = success ? r._result : _badRecipeName;
                    _entityManager.SpawnEntity(entityToSpawn, Owner.Transform.GridPosition);
                    _audioSystem.Play("/Audio/machines/microwave_done_beep.ogg");
                    SetAppearance(MicrowaveVisualState.Idle);
                    _busy = false;
                });
                _busy = false;
                UpdateUserInterface();
                return;
            }
        }

        /// <summary>
        /// This actually deletes all the reagents.
        /// </summary>
        private void DestroyReagents()
        {
            _contents.RemoveAllSolution();
        }

        private void EjectSolids()
        {

            foreach (var item in _storage.ContainedEntities.ToList())
            {
                _storage.Remove(item);
            }

            foreach (var kvp in _entityContents)
            {
                if (kvp.Value > 1 && _prototypeManager.TryIndex(kvp.Key, out EntityPrototype proto))
                {
                    for(int i = 0; i <= kvp.Value - 1; i++)
                        _entityManager.SpawnEntity(proto.Name, Owner.Transform.GridPosition);

                }
            }

            _entityContents.Clear();
        }
        private bool CanSatisfyRecipe(FoodRecipePrototype recipe)
        {
            foreach (var reagent in recipe._ingReagents)
            {
                if (!_contents.ContainsReagent(reagent.Key, out var amount))
                {
                    return false;
                }

                if (amount.Int() < reagent.Value)
                {
                    return false;
                }
            }

            foreach (var solid in recipe._ingSolids)
            {
                if (!_entityContents.TryGetValue(solid.Key, out var amount))
                {
                    return false;
                }

                if (amount < solid.Value)
                {
                    return false;
                }
            }

            return true;
        }

        private void SubtractContents(FoodRecipePrototype recipe)
        {
            foreach(var item in recipe._ingReagents)
            {
                _contents.TryRemoveReagent(item.Key, ReagentUnit.New(item.Value));
            }

            foreach(var item in recipe._ingSolids)
            {
                _entityContents.TryGetValue(item.Key, out var value);
                value -= item.Value;
            }
        }

        private void SetAppearance(MicrowaveVisualState state)
        {
            if (_appearance != null || Owner.TryGetComponent(out _appearance))
                _appearance.SetData(PowerDeviceVisuals.VisualState, state);
        }

        private void UpdateUserInterface()
        {
            _userInterface.SetState(new MicrowaveUserInterfaceState(_contents.Solution.Contents.ToList(), solids:_entityContents));
        }


    }
}
