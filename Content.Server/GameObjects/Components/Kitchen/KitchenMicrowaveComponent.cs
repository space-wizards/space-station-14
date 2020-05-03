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
using Robust.Shared.Localization;
using Content.Server.Interfaces;

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
        [Dependency] private readonly IServerNotifyManager _notifyManager;
#pragma warning restore 649

        private int _cookTimeDefault;
        private int _cookTimeMultiplier; //For upgrades and stuff I guess?
        private string _badRecipeName;
        private string _startCookingSound;
        private string _cookingCompleteSound;
        [ViewVariables]
        private SolutionComponent _solution;

        [ViewVariables]
        private bool _busy = false;

        private bool Powered => _powerDevice.Powered;

        private bool HasContents => _solution.ReagentList.Count > 0 || _storage.ContainedEntities.Count > 0;

        private AppearanceComponent _appearance;

        private AudioSystem _audioSystem;

        private PowerDeviceComponent _powerDevice;

        private Container _storage;

        private Dictionary<string, int> _solids;
        private List<EntityUid> _solidsVisualList;

        private BoundUserInterface _userInterface;
        void ISolutionChange.SolutionChanged(SolutionChangeEventArgs eventArgs) => UpdateUserInterface();
        public override void ExposeData(ObjectSerializer serializer)
        {
            base.ExposeData(serializer);
            serializer.DataField(ref _badRecipeName, "failureResult", "FoodBadRecipe");
            serializer.DataField(ref _cookTimeDefault, "cookTime", 5);
            serializer.DataField(ref _cookTimeMultiplier, "cookTimeMultiplier", 1000);
            serializer.DataField(ref _startCookingSound, "beginCookingSound","/Audio/machines/microwave_start_beep.ogg" );
            serializer.DataField(ref _cookingCompleteSound, "foodDoneSound","/Audio/machines/microwave_done_beep.ogg" );
        }

        public override void Initialize()
        {
            base.Initialize();
            _solution ??= Owner.TryGetComponent(out SolutionComponent solutionComponent)
                ? solutionComponent
                : Owner.AddComponent<SolutionComponent>();

            _storage = ContainerManagerComponent.Ensure<Container>("microwave_entity_container", Owner, out var existed);
            _appearance = Owner.GetComponent<AppearanceComponent>();
            _powerDevice = Owner.GetComponent<PowerDeviceComponent>();
            _audioSystem = _entitySystemManager.GetEntitySystem<AudioSystem>();
            _userInterface = Owner.GetComponent<ServerUserInterfaceComponent>()
                .GetBoundUserInterface(MicrowaveUiKey.Key);

            _solids = new Dictionary<string, int>();
            _solidsVisualList = new List<EntityUid>();
            _userInterface.OnReceiveMessage += UserInterfaceOnReceiveMessage;

        }

        private void UserInterfaceOnReceiveMessage(ServerBoundUserInterfaceMessage message)
        {
            if (!Powered || _busy || !HasContents) return;

            switch (message.Message)
            {
                case MicrowaveStartCookMessage msg :
                    wzhzhzh();
                    break;

                case MicrowaveEjectMessage msg :
                    VaporizeReagents();
                    EjectSolids();
                    UpdateUserInterface();
                    break;

                case MicrowaveEjectSolidIndexedMessage msg:
                    EjectIndexedSolid(msg.index);
                    UpdateUserInterface();
                    break;
            }

        }

        private void SetAppearance(MicrowaveVisualState state)
        {
            if (_appearance != null || Owner.TryGetComponent(out _appearance))
                _appearance.SetData(PowerDeviceVisuals.VisualState, state);
        }

        private void UpdateUserInterface()
        {
            _solidsVisualList.Clear();
            foreach(var item in _storage.ContainedEntities.ToList())
            {
                _solidsVisualList.Add(item.Uid);
            }

            _userInterface.SetState(new MicrowaveUpdateUserInterfaceState(_solution.Solution.Contents.ToList(), _solidsVisualList));
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

            if(itemEntity.TryGetComponent<PourableComponent>(out var attackPourable))
            {
                //Get target and check if it can be poured into
                if (!Owner.TryGetComponent<SolutionComponent>(out var mySolution)
                    || !mySolution.CanPourIn)
                {
                    return false;
                }

                if (!itemEntity.TryGetComponent<SolutionComponent>(out var attackSolution)
                    || !attackSolution.CanPourOut)
                {
                    return false;
                }

                //Get transfer amount. May be smaller than _transferAmount if not enough room
                var realTransferAmount = ReagentUnit.Min(attackPourable.TransferAmount, mySolution.EmptyVolume);
                if (realTransferAmount <= 0) //Special message if container is full
                {
                    _notifyManager.PopupMessage(Owner.Transform.GridPosition, eventArgs.User,
                        Loc.GetString("Container is full"));
                    return false;
                }

                //Move units from attackSolution to targetSolution
                var removedSolution = attackSolution.SplitSolution(realTransferAmount);
                if (!mySolution.TryAddSolution(removedSolution))
                    return false;

                _notifyManager.PopupMessage(Owner.Transform.GridPosition, eventArgs.User,
                    Loc.GetString("Transferred {0}u", removedSolution.TotalVolume));
                return true;
            }

            if (itemEntity.TryGetComponent(typeof(FoodComponent), out var food))
            {
                var ent = food.Owner; //Get the entity of the ItemComponent.
                
                _storage.Insert(ent);

                UpdateUserInterface();
                return true;
            }
           
            return false;
        }

        //This is required.
        private void wzhzhzh()
        {
            _busy = true;
            // Convert storage into Dictionary of ingredients
            _solids.Clear();
            foreach(var item in _storage.ContainedEntities.ToList())
            {
                if(_solids.ContainsKey(item.Prototype.ID))
                {
                    _solids[item.Prototype.ID]++;
                }
                else
                {
                    _solids.Add(item.Prototype.ID, 1);
                }
            }

            // Check recipes
            foreach(var r in _recipeManager.Recipes)
            {

                var success = CanSatisfyRecipe(r);
                SetAppearance(MicrowaveVisualState.Cooking);
                _audioSystem.Play(_startCookingSound);
                var time = success ? r.CookTime : _cookTimeDefault;
                Timer.Spawn(time * _cookTimeMultiplier, () =>
                {

                    if (success)
                    {
                        SubtractContents(r);
                    }
                    else
                    {
                        VaporizeReagents();
                        VaporizeSolids();
                    }

                    var entityToSpawn = success ? r.Result : _badRecipeName;
                    _entityManager.SpawnEntity(entityToSpawn, Owner.Transform.GridPosition);
                    _audioSystem.Play(_cookingCompleteSound);
                    SetAppearance(MicrowaveVisualState.Idle);
                    _busy = false;
                });
                UpdateUserInterface();
                return;
            }
        }

        private void VaporizeReagents()
        {
            _solution.RemoveAllSolution();

        }

        private void VaporizeSolids()
        {
            foreach (var item in _storage.ContainedEntities.ToList())
            {
                item.Delete();
            }
        }

        private void EjectSolids()
        {

            foreach (var item in _storage.ContainedEntities.ToList())
            {
                _storage.Remove(item);
            }

            _solids.Clear();
        }

        private void EjectIndexedSolid(int index)
        {
            var entityToRemove = _storage.ContainedEntities.ToArray()[index];
            _storage.Remove(entityToRemove);
        }


        private void SubtractContents(FoodRecipePrototype recipe)
        {
            foreach(var kvp in recipe.IngredientsReagents)
            {
                _solution.TryRemoveReagent(kvp.Key, ReagentUnit.New(kvp.Value));
            }

            foreach (var solid in recipe.IngredientsSolids)
            {
                _solids[solid.Key] -= solid.Value;
            }
        }

        private bool CanSatisfyRecipe(FoodRecipePrototype recipe)
        {
            foreach (var reagent in recipe.IngredientsReagents)
            {
                if (!_solution.ContainsReagent(reagent.Key, out var amount))
                {
                    return false;
                }

                if (amount.Int() < reagent.Value)
                {
                    return false;
                }
            }

            foreach (var solid in recipe.IngredientsSolids)
            {
                if (!_solids.ContainsKey(solid.Key))
                {
                    return false;
                }

                if (_solids[solid.Key] < solid.Value)
                {
                    return false;
                }
            }

            return true;
        }

    }
}
