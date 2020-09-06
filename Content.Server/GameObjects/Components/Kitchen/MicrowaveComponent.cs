#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Content.Server.GameObjects.Components.Body;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.ViewVariables;
using Content.Server.GameObjects.Components.Chemistry;
using Content.Server.GameObjects.Components.GUI;
using Content.Server.GameObjects.Components.Items.Storage;
using Content.Server.GameObjects.Components.Power.ApcNetComponents;
using Content.Server.GameObjects.EntitySystems;
using Content.Server.Interfaces.Chat;
using Content.Server.Interfaces.GameObjects;
using Content.Server.Utility;
using Content.Shared.Chemistry;
using Content.Shared.GameObjects.Components.Power;
using Content.Shared.Interfaces;
using Content.Shared.Interfaces.GameObjects.Components;
using Content.Shared.Kitchen;
using Content.Shared.Prototypes.Kitchen;
using Robust.Server.GameObjects;
using Robust.Server.GameObjects.Components.Container;
using Robust.Server.GameObjects.Components.UserInterface;
using Robust.Server.GameObjects.EntitySystems;
using Robust.Server.Interfaces.GameObjects;
using Robust.Shared.Audio;
using Robust.Shared.GameObjects.Systems;
using Content.Shared.GameObjects.Components.Body;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Localization;
using Robust.Shared.Serialization;
using Robust.Shared.Timers;

namespace Content.Server.GameObjects.Components.Kitchen
{
    [RegisterComponent]
    [ComponentReference(typeof(IActivate))]
    public class MicrowaveComponent : SharedMicrowaveComponent, IActivate, IInteractUsing, ISolutionChange, ISuicideAct
    {
        [Dependency] private readonly IEntityManager _entityManager = default!;
        [Dependency] private readonly RecipeManager _recipeManager = default!;

        #region YAMLSERIALIZE
        private int _cookTimeDefault;
        private int _cookTimeMultiplier; //For upgrades and stuff I guess?
        private string _badRecipeName = "";
        private string _startCookingSound = "";
        private string _cookingCompleteSound = "";
#endregion

[ViewVariables]
        private bool _busy = false;

        /// <summary>
        /// This is a fixed offset of 5.
        /// The cook times for all recipes should be divisible by 5,with a minimum of 1 second.
        /// For right now, I don't think any recipe cook time should be greater than 60 seconds.
        /// </summary>
        [ViewVariables]
        private uint _currentCookTimerTime = 1;

        private bool Powered => !Owner.TryGetComponent(out PowerReceiverComponent? receiver) || receiver.Powered;
        private bool _hasContents => Owner.TryGetComponent(out SolutionComponent? solution) && (solution.ReagentList.Count > 0 || _storage.ContainedEntities.Count > 0);
        private bool _uiDirty = true;
        private bool _lostPower = false;
        private int _currentCookTimeButtonIndex = 0;

        void ISolutionChange.SolutionChanged(SolutionChangeEventArgs eventArgs) => _uiDirty = true;
        private AudioSystem _audioSystem = default!;
        private Container _storage = default!;

        [ViewVariables] private BoundUserInterface? UserInterface => Owner.GetUIOrNull(MicrowaveUiKey.Key);

        public override void ExposeData(ObjectSerializer serializer)
        {
            base.ExposeData(serializer);
            serializer.DataField(ref _badRecipeName, "failureResult", "FoodBadRecipe");
            serializer.DataField(ref _cookTimeDefault, "cookTime", 5);
            serializer.DataField(ref _cookTimeMultiplier, "cookTimeMultiplier", 1000);
            serializer.DataField(ref _startCookingSound, "beginCookingSound","/Audio/Machines/microwave_start_beep.ogg" );
            serializer.DataField(ref _cookingCompleteSound, "foodDoneSound","/Audio/Machines/microwave_done_beep.ogg" );
        }

        public override void Initialize()
        {
            base.Initialize();

            Owner.EnsureComponent<SolutionComponent>();

            _storage = ContainerManagerComponent.Ensure<Container>("microwave_entity_container", Owner, out var existed);
            _audioSystem = EntitySystem.Get<AudioSystem>();

            if (UserInterface != null)
            {
                UserInterface.OnReceiveMessage += UserInterfaceOnReceiveMessage;
            }
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
                    if (_hasContents)
                    {
                        VaporizeReagents();
                        EjectSolids();
                        ClickSound();
                        _uiDirty = true;
                    }
                    break;
                case MicrowaveEjectSolidIndexedMessage msg:
                    if (_hasContents)
                    {
                        EjectSolid(msg.EntityID);
                        ClickSound();
                        _uiDirty = true;
                    }
                    break;
                case MicrowaveVaporizeReagentIndexedMessage msg:
                    if (_hasContents)
                    {
                        VaporizeReagentQuantity(msg.ReagentQuantity);
                        ClickSound();
                        _uiDirty = true;
                    }
                    break;
                case MicrowaveSelectCookTimeMessage msg:
                    _currentCookTimeButtonIndex = msg.ButtonIndex;
                    _currentCookTimerTime = msg.NewCookTime;
                    ClickSound();
                    _uiDirty = true;
                    break;
            }

        }

        public void OnUpdate()
        {

            if (!Powered)
            {
                //TODO:If someone cuts power currently, microwave magically keeps going. FIX IT!
                SetAppearance(MicrowaveVisualState.Idle);
            }

            if (_busy && !Powered)
            {
                //we lost power while we were cooking/busy!
                _lostPower = true;
                VaporizeReagents();
                EjectSolids();
                _busy = false;
                _uiDirty = true;
            }

            if (_uiDirty && Owner.TryGetComponent(out SolutionComponent? solution))
            {
                UserInterface?.SetState(new MicrowaveUpdateUserInterfaceState
                (
                    solution.Solution.Contents.ToArray(),
                    _storage.ContainedEntities.Select(item => item.Uid).ToArray(),
                    _busy,
                    _currentCookTimeButtonIndex,
                    _currentCookTimerTime
                ));
                _uiDirty = false;
            }
        }

        private void SetAppearance(MicrowaveVisualState state)
        {
            if (Owner.TryGetComponent(out AppearanceComponent? appearance))
            {
                appearance.SetData(PowerDeviceVisuals.VisualState, state);
            }
        }

        void IActivate.Activate(ActivateEventArgs eventArgs)
        {
            if (!eventArgs.User.TryGetComponent(out IActorComponent? actor) || !Powered)
            {
                return;
            }

            _uiDirty = true;
            UserInterface?.Open(actor.playerSession);
        }

        public async Task<bool> InteractUsing(InteractUsingEventArgs eventArgs)
        {
            if (!Powered)
            {
                Owner.PopupMessage(eventArgs.User, Loc.GetString("It has no power!"));
                return false;
            }

            var itemEntity = eventArgs.User.GetComponent<HandsComponent>().GetActiveHand?.Owner;

            if (itemEntity == null)
            {
                eventArgs.User.PopupMessage(Loc.GetString("You have no active hand!"));
                return false;
            }

            if (itemEntity.TryGetComponent<PourableComponent>(out var attackPourable))
            {
                if (!itemEntity.TryGetComponent<SolutionComponent>(out var attackSolution)
                    || !attackSolution.CanPourOut)
                {
                    return false;
                }

                if (!Owner.TryGetComponent(out SolutionComponent? solution))
                {
                    return false;
                }

                //Get transfer amount. May be smaller than _transferAmount if not enough room
                var realTransferAmount = ReagentUnit.Min(attackPourable.TransferAmount, solution.EmptyVolume);
                if (realTransferAmount <= 0) //Special message if container is full
                {
                    Owner.PopupMessage(eventArgs.User, Loc.GetString("Container is full"));
                    return false;
                }

                //Move units from attackSolution to targetSolution
                var removedSolution = attackSolution.SplitSolution(realTransferAmount);
                if (!solution.TryAddSolution(removedSolution))
                {
                    return false;
                }

                Owner.PopupMessage(eventArgs.User, Loc.GetString("Transferred {0}u", removedSolution.TotalVolume));
                return true;
            }

            if (!itemEntity.TryGetComponent(typeof(ItemComponent), out var food))
            {

                Owner.PopupMessage(eventArgs.User, "That won't work!");
                return false;
            }

            var ent = food.Owner; //Get the entity of the ItemComponent.
            _storage.Insert(ent);
            _uiDirty = true;
            return true;
        }

        // ReSharper disable once InconsistentNaming
        // ReSharper disable once IdentifierTypo
        private void wzhzhzh()
        {
            if (!_hasContents)
            {
                return;
            }

            _busy = true;
            // Convert storage into Dictionary of ingredients
            var solidsDict = new Dictionary<string, int>();
            foreach(var item in _storage.ContainedEntities)
            {
                if (item.Prototype == null)
                {
                    continue;
                }

                if(solidsDict.ContainsKey(item.Prototype.ID))
                {
                    solidsDict[item.Prototype.ID]++;
                }
                else
                {
                    solidsDict.Add(item.Prototype.ID, 1);
                }
            }

            var failState = MicrowaveSuccessState.RecipeFail;
            foreach(var id in solidsDict.Keys)
            {
                if(_recipeManager.SolidAppears(id))
                {
                    continue;
                }

                failState = MicrowaveSuccessState.UnwantedForeignObject;
                break;
            }

            // Check recipes
            FoodRecipePrototype? recipeToCook = null;
            foreach (var r in _recipeManager.Recipes.Where(r => CanSatisfyRecipe(r, solidsDict) == MicrowaveSuccessState.RecipePass))
            {
                recipeToCook = r;
            }

            var goodMeal = (recipeToCook != null)
                           &&
                           (_currentCookTimerTime == (uint)recipeToCook.CookTime);
            SetAppearance(MicrowaveVisualState.Cooking);
            _audioSystem.PlayFromEntity(_startCookingSound, Owner, AudioParams.Default);
            Timer.Spawn((int)(_currentCookTimerTime * _cookTimeMultiplier), (Action)(() =>
            {
                if (_lostPower)
                {
                    return;
                }

                if(failState == MicrowaveSuccessState.UnwantedForeignObject)
                {
                    VaporizeReagents();
                    EjectSolids();
                }
                else
                {
                    if (goodMeal)
                    {
                        SubtractContents(recipeToCook!);
                    }
                    else
                    {
                        VaporizeReagents();
                        VaporizeSolids();
                    }

                    if (recipeToCook != null)
                    {
                        var entityToSpawn = goodMeal ? recipeToCook.Result : _badRecipeName;
                        _entityManager.SpawnEntity(entityToSpawn, Owner.Transform.Coordinates);
                    }
                }
                _audioSystem.PlayFromEntity(_cookingCompleteSound, Owner, AudioParams.Default.WithVolume(-1f));

                SetAppearance(MicrowaveVisualState.Idle);
                _busy = false;

                _uiDirty = true;
            }));
            _lostPower = false;
            _uiDirty = true;
        }

        private void VaporizeReagents()
        {
            if (Owner.TryGetComponent(out SolutionComponent? solution))
            {
                solution.RemoveAllSolution();
            }
        }

        private void VaporizeReagentQuantity(Solution.ReagentQuantity reagentQuantity)
        {
            if (Owner.TryGetComponent(out SolutionComponent? solution))
            {
                solution?.TryRemoveReagent(reagentQuantity.ReagentId, reagentQuantity.Quantity);
            }
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

        private void EjectSolid(EntityUid entityID)
        {
            if (_entityManager.EntityExists(entityID))
            {
                _storage.Remove(_entityManager.GetEntity(entityID));
            }
        }


        private void SubtractContents(FoodRecipePrototype recipe)
        {
            if (!Owner.TryGetComponent(out SolutionComponent? solution))
            {
                return;
            }

            foreach(var recipeReagent in recipe.IngredientsReagents)
            {
                solution?.TryRemoveReagent(recipeReagent.Key, ReagentUnit.New(recipeReagent.Value));
            }

            foreach (var recipeSolid in recipe.IngredientsSolids)
            {
                for (var i = 0; i < recipeSolid.Value; i++)
                {
                    foreach (var item in _storage.ContainedEntities)
                    {
                        if (item.Prototype == null)
                        {
                            continue;
                        }

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

        private MicrowaveSuccessState CanSatisfyRecipe(FoodRecipePrototype recipe, Dictionary<string,int> solids)
        {
            if (!Owner.TryGetComponent(out SolutionComponent? solution))
            {
                return MicrowaveSuccessState.RecipeFail;
            }

            foreach (var reagent in recipe.IngredientsReagents)
            {
                if (!solution.ContainsReagent(reagent.Key, out var amount))
                {
                    return MicrowaveSuccessState.RecipeFail;
                }

                if (amount.Int() < reagent.Value)
                {
                    return MicrowaveSuccessState.RecipeFail;
                }
            }

            foreach (var solid in recipe.IngredientsSolids)
            {
                if (!solids.ContainsKey(solid.Key))
                {
                    return MicrowaveSuccessState.RecipeFail;
                }

                if (solids[solid.Key] < solid.Value)
                {
                    return MicrowaveSuccessState.RecipeFail;
                }
            }


            return MicrowaveSuccessState.RecipePass;
        }

        private void ClickSound()
        {
            _audioSystem.PlayFromEntity("/Audio/Machines/machine_switch.ogg",Owner,AudioParams.Default.WithVolume(-2f));
        }

        public SuicideKind Suicide(IEntity victim, IChatManager chat)
        {
            var headCount = 0;
            if (victim.TryGetComponent<BodyManagerComponent>(out var bodyManagerComponent))
            {
                var heads = bodyManagerComponent.GetBodyPartsOfType(BodyPartType.Head);
                foreach (var head in heads)
                {
                    var droppedHead = bodyManagerComponent.DropPart(head);

                    if (droppedHead == null)
                    {
                        continue;
                    }

                    _storage.Insert(droppedHead);
                    headCount++;
                }
            }

            var othersMessage = Loc.GetString("{0:theName} is trying to cook {0:their} head!", victim);
            victim.PopupMessageOtherClients(othersMessage);

            var selfMessage = Loc.GetString("You cook your head!");
            victim.PopupMessage(selfMessage);

            _currentCookTimerTime = 10;
            ClickSound();
            _uiDirty = true;
            wzhzhzh();
            return SuicideKind.Heat;
        }
    }
}
