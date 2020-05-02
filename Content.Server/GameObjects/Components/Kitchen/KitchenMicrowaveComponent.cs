using System.Linq;
using Content.Server.GameObjects.EntitySystems;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.ViewVariables;
using Content.Server.GameObjects.Components.Chemistry;
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

namespace Content.Server.GameObjects.Components.Kitchen
{
    [RegisterComponent]
    [ComponentReference(typeof(IActivate))]
    public class KitchenMicrowaveComponent : SharedMicrowaveComponent, IActivate, ISolutionChange
    {
#pragma warning disable 649
        [Dependency] private readonly IEntitySystemManager _entitySystemManager;
        [Dependency] private readonly IEntityManager _entityManager;
        [Dependency] private readonly RecipeManager _recipeManager;
#pragma warning restore 649

        private int _cookTimeDefault;
        private int _cookTimeMultiplier; //For upgrades and stuff I guess?
        private string _badRecipeName;
        [ViewVariables]
        private SolutionComponent _contents;

        [ViewVariables]
        public bool _busy = false;

        private bool Powered => _powerDevice.Powered;

        private bool HasContents => _contents.ReagentList.Count > 0;

        private AppearanceComponent _appearance;

        private AudioSystem _audioSystem;

        private PowerDeviceComponent _powerDevice;

        private Container _storage;

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
                    EjectReagents();
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
                        EjectReagents();
                    }

                    var entityToSpawn = success ? r._result : _badRecipeName;
                    _entityManager.SpawnEntity(entityToSpawn, Owner.Transform.GridPosition);
                    _audioSystem.Play("/Audio/machines/microwave_done_beep.ogg");
                    SetAppearance(MicrowaveVisualState.Idle);
                    _busy = false;
                });
                return;
            }
        }

        /// <summary>
        /// This actually deletes all the reagents.
        /// </summary>
        private void EjectReagents()
        {
            _contents.RemoveAllSolution();
        }
        private bool CanSatisfyRecipe(FoodRecipePrototype recipe)
        {
            foreach (var item in recipe._ingredients)
            {
                if (!_contents.ContainsReagent(item.Key, out var amount))
                {
                    return false;
                }

                if (amount.Int() < item.Value)
                {
                    return false;
                }
            }

            return true;
        }

        private void SubtractContents(FoodRecipePrototype recipe)
        {
            foreach(var item in recipe._ingredients)
            {
                _contents.TryRemoveReagent(item.Key, ReagentUnit.New(item.Value));
            }
        }

        private void SetAppearance(MicrowaveVisualState state)
        {
            if (_appearance != null || Owner.TryGetComponent(out _appearance))
                _appearance.SetData(PowerDeviceVisuals.VisualState, state);
        }

        private void UpdateUserInterface()
        {
            _userInterface.SetState(new MicrowaveUserInterfaceState(_contents.Solution.Contents.ToList()));
        }
    }
}
