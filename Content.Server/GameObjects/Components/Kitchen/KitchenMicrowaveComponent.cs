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
using Robust.Shared.Localization;
using Content.Server.Interfaces;
using Content.Server.Utility;
using Robust.Shared.Audio;
using Content.Server.Interfaces.GameObjects;
using Content.Server.Interfaces.Chat;
using Content.Server.BodySystem;
using Content.Shared.BodySystem;

namespace Content.Server.GameObjects.Components.Kitchen
{
    [RegisterComponent]
    [ComponentReference(typeof(IActivate))]
    public class KitchenMicrowaveComponent : SharedMicrowaveComponent, IActivate, IInteractUsing, ISolutionChange, ISuicideAct
    {
#pragma warning disable 649
        [Dependency] private readonly IEntitySystemManager _entitySystemManager;
        [Dependency] private readonly IEntityManager _entityManager;
        [Dependency] private readonly RecipeManager _recipeManager;
        [Dependency] private readonly IServerNotifyManager _notifyManager;
#pragma warning restore 649

#region YAMLSERIALIZE
        private int _cookTimeDefault;
        private int _cookTimeMultiplier; //For upgrades and stuff I guess?
        private string _badRecipeName;
        private string _startCookingSound;
        private string _cookingCompleteSound;
#endregion

#region VIEWVARIABLES
        [ViewVariables]
        private SolutionComponent _solution;

        [ViewVariables]
        private bool _busy = false;

        /// <summary>
        /// This is a fixed offset of 5.
        /// The cook times for all recipes should be divisible by 5,with a minimum of 1 second.
        /// For right now, I don't think any recipe cook time should be greater than 60 seconds.
        /// </summary>
        [ViewVariables]
        private uint _currentCookTimerTime { get; set; } = 1;
#endregion

        private bool Powered => _powerDevice.Powered;

        private bool HasContents => _solution.ReagentList.Count > 0 || _storage.ContainedEntities.Count > 0;

        void ISolutionChange.SolutionChanged(SolutionChangeEventArgs eventArgs) => UpdateUserInterface();

        private AudioSystem _audioSystem;

        private AppearanceComponent _appearance;
        private PowerDeviceComponent _powerDevice;

        private BoundUserInterface _userInterface;

        private Container _storage;


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
            _userInterface.OnReceiveMessage += UserInterfaceOnReceiveMessage;
        }

        private void UserInterfaceOnReceiveMessage(ServerBoundUserInterfaceMessage message)
        {
            if (!Powered || _busy)
            {
                return;
            }

            switch (message.Message)
            {
                case MicrowaveStartCookMessage msg :
                    wzhzhzh();
                    break;

                case MicrowaveEjectMessage msg :
                    if (HasContents)
                    {
                        VaporizeReagents();
                        EjectSolids();
                        ClickSound();
                        UpdateUserInterface();
                    }

                    break;

                case MicrowaveEjectSolidIndexedMessage msg:
                    if (HasContents)
                    {
                        EjectSolidWithIndex(msg.EntityID);
                        ClickSound();
                        UpdateUserInterface();
                    }
                    break;
                case MicrowaveVaporizeReagentIndexedMessage msg:
                    if (HasContents)
                    {
                        VaporizeReagentWithReagentQuantity(msg.ReagentQuantity);
                        ClickSound();
                        UpdateUserInterface();
                    }
                    break;
                case MicrowaveSelectCookTimeMessage msg:
                    _currentCookTimerTime = msg.newCookTime;
                    ClickSound();
                    UpdateUserInterface();
                    break;
            }

        }

        private void SetAppearance(MicrowaveVisualState state)
        {
            if (_appearance != null || Owner.TryGetComponent(out _appearance))
            {
                _appearance.SetData(PowerDeviceVisuals.VisualState, state);
            }

        }

        private void UpdateUserInterface()
        {
            var solidsVisualList = new List<EntityUid>();
            foreach(var item in _storage.ContainedEntities)
            {
                solidsVisualList.Add(item.Uid);
            }

            _userInterface.SetState(new MicrowaveUpdateUserInterfaceState(_solution.Solution.Contents, solidsVisualList, _busy));
        }

        void IActivate.Activate(ActivateEventArgs eventArgs)
        {
            if (!eventArgs.User.TryGetComponent(out IActorComponent actor) || !Powered)
            {
                return;
            }

            UpdateUserInterface();
            _userInterface.Open(actor.playerSession);

        }

        public bool InteractUsing(InteractUsingEventArgs eventArgs)
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
                {
                    return false;
                }

                _notifyManager.PopupMessage(Owner.Transform.GridPosition, eventArgs.User,
                    Loc.GetString("Transferred {0}u", removedSolution.TotalVolume));
                return true;
            }

            if (!itemEntity.TryGetComponent(typeof(FoodComponent), out var food))
            {

                _notifyManager.PopupMessage(Owner, eventArgs.User, "That won't work!");
                return false;
            }

            var ent = food.Owner; //Get the entity of the ItemComponent.
            _storage.Insert(ent);
            UpdateUserInterface();
            return true;

        }

        //This is required. It's 'cook'.
        private void wzhzhzh()
        {
            if (!HasContents)
            {
                return;
            }

            _busy = true;
            // Convert storage into Dictionary of ingredients
            var solidsDict = new Dictionary<string, int>();
            foreach(var item in _storage.ContainedEntities)
            {
                if(solidsDict.ContainsKey(item.Prototype.ID))
                {
                    solidsDict[item.Prototype.ID]++;
                }
                else
                {
                    solidsDict.Add(item.Prototype.ID, 1);
                }
            }

            // Check recipes
            FoodRecipePrototype recipeToCook = null;
            foreach(var r in _recipeManager.Recipes)
            {
                if (!CanSatisfyRecipe(r, solidsDict))
                {
                    continue;
                }

                recipeToCook = r;
            }

            var goodMeal = (recipeToCook != null)
                           &&
                           (_currentCookTimerTime == (uint)recipeToCook.CookTime) ? true : false;

            SetAppearance(MicrowaveVisualState.Cooking);
            _audioSystem.Play(_startCookingSound,Owner, AudioParams.Default);
            Timer.Spawn((int)(_currentCookTimerTime * _cookTimeMultiplier), () =>
            {

                if (goodMeal)
                {
                    SubtractContents(recipeToCook);
                }
                else
                {
                    VaporizeReagents();
                    VaporizeSolids();
                }

                var entityToSpawn = goodMeal ? recipeToCook.Result : _badRecipeName;
                _entityManager.SpawnEntity(entityToSpawn, Owner.Transform.GridPosition);
                _audioSystem.Play(_cookingCompleteSound,Owner, AudioParams.Default);
                SetAppearance(MicrowaveVisualState.Idle);
                _busy = false;
                UpdateUserInterface();
            });
            UpdateUserInterface();
        }

        private void VaporizeReagents()
        {
            _solution.RemoveAllSolution();
        }

        private void VaporizeReagentWithReagentQuantity(Solution.ReagentQuantity reagentQuantity)
        {
            _solution.TryRemoveReagent(reagentQuantity.ReagentId, reagentQuantity.Quantity);
        }

        private void VaporizeSolids()
        {
            for(var i = _storage.ContainedEntities.Count-1; i>=0; i--)
            {
                var item = _storage.ContainedEntities.ElementAt(i);
                _storage.Remove(item);
                item.Delete();
            }
        }

        private void EjectSolids()
        {

            for(var i = _storage.ContainedEntities.Count-1; i>=0; i--)
            {
                _storage.Remove(_storage.ContainedEntities.ElementAt(i));
            }
        }

        private void EjectSolidWithIndex(EntityUid entityID)
        {
            if (_entityManager.EntityExists(entityID))
            {
                _storage.Remove(_entityManager.GetEntity(entityID));
            }
        }


        private void SubtractContents(FoodRecipePrototype recipe)
        {
            foreach(var recipeReagent in recipe.IngredientsReagents)
            {
                _solution.TryRemoveReagent(recipeReagent.Key, ReagentUnit.New(recipeReagent.Value));
            }

            foreach (var recipeSolid in recipe.IngredientsSolids)
            {
                for (var i = 0; i < recipeSolid.Value; i++)
                {
                    foreach (var item in _storage.ContainedEntities)
                    {
                        if (item.Prototype.ID == recipeSolid.Key)
                        {
                            _storage.Remove(item);
                            item.Delete();
                            break;
                        }
                    }
                }
            }

        }

        private bool CanSatisfyRecipe(FoodRecipePrototype recipe, Dictionary<string,int> solids)
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
                if (!solids.ContainsKey(solid.Key))
                {
                    return false;
                }

                if (solids[solid.Key] < solid.Value)
                {
                    return false;
                }
            }

            return true;
        }

        private void ClickSound()
        {

            _audioSystem.Play("/Audio/machines/machine_switch.ogg",Owner, AudioParams.Default.WithVolume(-2f));

        }

        public SuicideKind Suicide(IEntity victim, IChatManager chat)
        {
            int headCount = 0;
            if (victim.TryGetComponent<BodyManagerComponent>(out var bodyManagerComponent))
            {
                var heads = bodyManagerComponent.GetBodyPartsOfType(BodyPartType.Head);
                foreach (var head in heads)
                {
                    var droppedHead = bodyManagerComponent.DisconnectBodyPart(head, true);
                    _storage.Insert(droppedHead);
                    headCount++;
                }
            }
            chat.EntityMe(victim, Loc.GetPluralString("is trying to cook {0:their} head!", "is trying to cook {0:their} heads!", headCount, victim));
            _currentCookTimerTime = 10;
            ClickSound();
            UpdateUserInterface();
            wzhzhzh();
            return SuicideKind.Heat;
        }
    }
}
