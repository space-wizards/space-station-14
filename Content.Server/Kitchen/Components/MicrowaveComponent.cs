using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Content.Server.Act;
using Content.Server.Chat.Managers;
using Content.Server.Chemistry.Components;
using Content.Server.Chemistry.EntitySystems;
using Content.Server.Hands.Components;
using Content.Server.Popups;
using Content.Server.Power.Components;
using Content.Server.UserInterface;
using Content.Shared.Acts;
using Content.Shared.Body.Components;
using Content.Shared.Body.Part;
using Content.Shared.Chemistry.Components;
using Content.Shared.FixedPoint;
using Content.Shared.Interaction;
using Content.Shared.Item;
using Content.Shared.Kitchen;
using Content.Shared.Kitchen.Components;
using Content.Shared.Popups;
using Content.Shared.Power;
using Content.Shared.Sound;
using Robust.Server.GameObjects;
using Robust.Shared.Audio;
using Robust.Shared.Containers;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Localization;
using Robust.Shared.Player;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.ViewVariables;

namespace Content.Server.Kitchen.Components
{
    [RegisterComponent]
    [ComponentReference(typeof(IActivate))]
    public class MicrowaveComponent : SharedMicrowaveComponent, IActivate, IInteractUsing, ISuicideAct, IBreakAct
    {
        [Dependency] private readonly IEntityManager _entities = default!;

        [Dependency] private readonly RecipeManager _recipeManager = default!;

        #region YAMLSERIALIZE

        [DataField("cookTime")] private uint _cookTimeDefault = 5;
        [DataField("cookTimeMultiplier")] private int _cookTimeMultiplier = 1000; //For upgrades and stuff I guess?
        [DataField("failureResult")] private string _badRecipeName = "FoodBadRecipe";

        [DataField("beginCookingSound")] private SoundSpecifier _startCookingSound =
            new SoundPathSpecifier("/Audio/Machines/microwave_start_beep.ogg");

        [DataField("foodDoneSound")] private SoundSpecifier _cookingCompleteSound =
            new SoundPathSpecifier("/Audio/Machines/microwave_done_beep.ogg");

        [DataField("clickSound")]
        private SoundSpecifier _clickSound = new SoundPathSpecifier("/Audio/Machines/machine_switch.ogg");

        #endregion YAMLSERIALIZE

        [ViewVariables] private bool _busy = false;
        private bool _broken;

        /// <summary>
        /// This is a fixed offset of 5.
        /// The cook times for all recipes should be divisible by 5,with a minimum of 1 second.
        /// For right now, I don't think any recipe cook time should be greater than 60 seconds.
        /// </summary>
        [ViewVariables] private uint _currentCookTimerTime = 1;

        private bool Powered => !_entities.TryGetComponent(Owner, out ApcPowerReceiverComponent? receiver) || receiver.Powered;

        private bool HasContents => EntitySystem.Get<SolutionContainerSystem>()
                                        .TryGetSolution(Owner, SolutionName, out var solution) &&
                                    (solution.Contents.Count > 0 || _storage.ContainedEntities.Count > 0);

        private bool _uiDirty = true;
        private bool _lostPower;
        private int _currentCookTimeButtonIndex;

        public void DirtyUi()
        {
            _uiDirty = true;
        }

        private Container _storage = default!;
        private const string SolutionName = "microwave";

        [ViewVariables] private BoundUserInterface? UserInterface => Owner.GetUIOrNull(MicrowaveUiKey.Key);

        protected override void Initialize()
        {
            base.Initialize();

            _currentCookTimerTime = _cookTimeDefault;

            EntitySystem.Get<SolutionContainerSystem>().EnsureSolution(Owner, SolutionName);

            _storage = ContainerHelpers.EnsureContainer<Container>(Owner, "microwave_entity_container",
                out _);

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
                case MicrowaveStartCookMessage:
                    Wzhzhzh();
                    break;
                case MicrowaveEjectMessage:
                    if (HasContents)
                    {
                        VaporizeReagents();
                        EjectSolids();
                        ClickSound();
                        _uiDirty = true;
                    }

                    break;
                case MicrowaveEjectSolidIndexedMessage msg:
                    if (HasContents)
                    {
                        EjectSolid(msg.EntityID);
                        ClickSound();
                        _uiDirty = true;
                    }

                    break;
                case MicrowaveVaporizeReagentIndexedMessage msg:
                    if (HasContents)
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

            if (_busy && _broken)
            {
                SetAppearance(MicrowaveVisualState.Broken);
                //we broke while we were cooking/busy!
                _lostPower = true;
                VaporizeReagents();
                EjectSolids();
                _busy = false;
                _uiDirty = true;
            }

            if (_uiDirty && EntitySystem.Get<SolutionContainerSystem>()
                .TryGetSolution(Owner, SolutionName, out var solution))
            {
                UserInterface?.SetState(new MicrowaveUpdateUserInterfaceState
                (
                    solution.Contents.ToArray(),
                    _storage.ContainedEntities.Select(item => item).ToArray(),
                    _busy,
                    _currentCookTimeButtonIndex,
                    _currentCookTimerTime
                ));
                _uiDirty = false;
            }
        }

        private void SetAppearance(MicrowaveVisualState state)
        {
            var finalState = state;
            if (_broken)
            {
                finalState = MicrowaveVisualState.Broken;
            }

            if (_entities.TryGetComponent(Owner, out AppearanceComponent? appearance))
            {
                appearance.SetData(PowerDeviceVisuals.VisualState, finalState);
            }
        }

        public void OnBreak(BreakageEventArgs eventArgs)
        {
            _broken = true;
            SetAppearance(MicrowaveVisualState.Broken);
        }

        void IActivate.Activate(ActivateEventArgs eventArgs)
        {
            if (!_entities.TryGetComponent(eventArgs.User, out ActorComponent? actor) || !Powered)
            {
                return;
            }

            _uiDirty = true;
            UserInterface?.Toggle(actor.PlayerSession);
        }

        async Task<bool> IInteractUsing.InteractUsing(InteractUsingEventArgs eventArgs)
        {
            if (!Powered)
            {
                Owner.PopupMessage(eventArgs.User, Loc.GetString("microwave-component-interact-using-no-power"));
                return false;
            }

            if (_broken)
            {
                Owner.PopupMessage(eventArgs.User, Loc.GetString("microwave-component-interact-using-broken"));
                return false;
            }

            if (_entities.GetComponent<HandsComponent>(eventArgs.User).GetActiveHandItem?.Owner is not {Valid: true} itemEntity)
            {
                eventArgs.User.PopupMessage(Loc.GetString("microwave-component-interact-using-no-active-hand"));
                return false;
            }

            if (_entities.TryGetComponent<SolutionTransferComponent?>(itemEntity, out var attackPourable))
            {
                var solutionsSystem = EntitySystem.Get<SolutionContainerSystem>();
                if (!solutionsSystem.TryGetDrainableSolution(itemEntity, out var attackSolution))
                {
                    return false;
                }

                if (!solutionsSystem.TryGetSolution(Owner, SolutionName, out var solution))
                {
                    return false;
                }

                //Get transfer amount. May be smaller than _transferAmount if not enough room
                var realTransferAmount = FixedPoint2.Min(attackPourable.TransferAmount, solution.AvailableVolume);
                if (realTransferAmount <= 0) //Special message if container is full
                {
                    Owner.PopupMessage(eventArgs.User,
                        Loc.GetString("microwave-component-interact-using-container-full"));
                    return false;
                }

                //Move units from attackSolution to targetSolution
                var removedSolution = EntitySystem.Get<SolutionContainerSystem>()
                    .Drain(itemEntity, attackSolution, realTransferAmount);
                if (!EntitySystem.Get<SolutionContainerSystem>().TryAddSolution(Owner, solution, removedSolution))
                {
                    return false;
                }

                Owner.PopupMessage(eventArgs.User, Loc.GetString("microwave-component-interact-using-transfer-success",
                    ("amount", removedSolution.TotalVolume)));
                return true;
            }

            if (!_entities.TryGetComponent(itemEntity, typeof(SharedItemComponent), out var food))
            {
                Owner.PopupMessage(eventArgs.User, "microwave-component-interact-using-transfer-fail");
                return false;
            }

            var ent = food.Owner; //Get the entity of the ItemComponent.
            _storage.Insert(ent);
            _uiDirty = true;
            return true;
        }

        // ReSharper disable once InconsistentNaming
        // ReSharper disable once IdentifierTypo
        private void Wzhzhzh()
        {
            if (!HasContents)
            {
                return;
            }

            _busy = true;
            // Convert storage into Dictionary of ingredients
            var solidsDict = new Dictionary<string, int>();
            foreach (var item in _storage.ContainedEntities)
            {
                var metaData = _entities.GetComponent<MetaDataComponent>(item);
                if (metaData.EntityPrototype == null)
                {
                    continue;
                }

                if (solidsDict.ContainsKey(metaData.EntityPrototype.ID))
                {
                    solidsDict[metaData.EntityPrototype.ID]++;
                }
                else
                {
                    solidsDict.Add(metaData.EntityPrototype.ID, 1);
                }
            }

            var failState = MicrowaveSuccessState.RecipeFail;
            foreach (var id in solidsDict.Keys)
            {
                if (_recipeManager.SolidAppears(id))
                {
                    continue;
                }

                failState = MicrowaveSuccessState.UnwantedForeignObject;
                break;
            }

            // Check recipes
            FoodRecipePrototype? recipeToCook = null;
            foreach (var r in _recipeManager.Recipes.Where(r =>
                CanSatisfyRecipe(r, solidsDict) == MicrowaveSuccessState.RecipePass))
            {
                recipeToCook = r;
            }

            SetAppearance(MicrowaveVisualState.Cooking);
            SoundSystem.Play(Filter.Pvs(Owner), _startCookingSound.GetSound(), Owner, AudioParams.Default);
            Owner.SpawnTimer((int) (_currentCookTimerTime * _cookTimeMultiplier), () =>
            {
                if (_lostPower)
                {
                    return;
                }

                if (failState == MicrowaveSuccessState.UnwantedForeignObject)
                {
                    VaporizeReagents();
                    EjectSolids();
                }
                else
                {
                    if (recipeToCook != null)
                    {
                        SubtractContents(recipeToCook);
                        _entities.SpawnEntity(recipeToCook.Result, _entities.GetComponent<TransformComponent>(Owner).Coordinates);
                    }
                    else
                    {
                        VaporizeReagents();
                        VaporizeSolids();
                        _entities.SpawnEntity(_badRecipeName, _entities.GetComponent<TransformComponent>(Owner).Coordinates);
                    }
                }

                SoundSystem.Play(Filter.Pvs(Owner), _cookingCompleteSound.GetSound(), Owner,
                    AudioParams.Default.WithVolume(-1f));

                SetAppearance(MicrowaveVisualState.Idle);
                _busy = false;

                _uiDirty = true;
            });
            _lostPower = false;
            _uiDirty = true;
        }

        private void VaporizeReagents()
        {
            if (EntitySystem.Get<SolutionContainerSystem>().TryGetSolution(Owner, SolutionName, out var solution))
            {
                EntitySystem.Get<SolutionContainerSystem>().RemoveAllSolution(Owner, solution);
            }
        }

        private void VaporizeReagentQuantity(Solution.ReagentQuantity reagentQuantity)
        {
            if (EntitySystem.Get<SolutionContainerSystem>().TryGetSolution(Owner, SolutionName, out var solution))
            {
                EntitySystem.Get<SolutionContainerSystem>()
                    .TryRemoveReagent(Owner, solution, reagentQuantity.ReagentId, reagentQuantity.Quantity);
            }
        }

        private void VaporizeSolids()
        {
            for (var i = _storage.ContainedEntities.Count - 1; i >= 0; i--)
            {
                var item = _storage.ContainedEntities.ElementAt(i);
                _storage.Remove(item);
                _entities.DeleteEntity(item);
            }
        }

        private void EjectSolids()
        {
            for (var i = _storage.ContainedEntities.Count - 1; i >= 0; i--)
            {
                _storage.Remove(_storage.ContainedEntities.ElementAt(i));
            }
        }

        private void EjectSolid(EntityUid entityId)
        {
            if (_entities.EntityExists(entityId))
            {
                _storage.Remove(entityId);
            }
        }

        private void SubtractContents(FoodRecipePrototype recipe)
        {
            var solutionUid = Owner;
            if (!EntitySystem.Get<SolutionContainerSystem>().TryGetSolution(Owner, SolutionName, out var solution))
            {
                return;
            }

            foreach (var recipeReagent in recipe.IngredientsReagents)
            {
                EntitySystem.Get<SolutionContainerSystem>()
                    .TryRemoveReagent(solutionUid, solution, recipeReagent.Key, FixedPoint2.New(recipeReagent.Value));
            }

            foreach (var recipeSolid in recipe.IngredientsSolids)
            {
                for (var i = 0; i < recipeSolid.Value; i++)
                {
                    foreach (var item in _storage.ContainedEntities)
                    {
                        var metaData = _entities.GetComponent<MetaDataComponent>(item);
                        if (metaData.EntityPrototype == null)
                        {
                            continue;
                        }

                        if (metaData.EntityPrototype.ID == recipeSolid.Key)
                        {
                            _storage.Remove(item);
                            _entities.DeleteEntity(item);
                            break;
                        }
                    }
                }
            }
        }

        private MicrowaveSuccessState CanSatisfyRecipe(FoodRecipePrototype recipe, Dictionary<string, int> solids)
        {
            if (_currentCookTimerTime != recipe.CookTime)
            {
                return MicrowaveSuccessState.RecipeFail;
            }

            if (!EntitySystem.Get<SolutionContainerSystem>().TryGetSolution(Owner, SolutionName, out var solution))
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
            SoundSystem.Play(Filter.Pvs(Owner), _clickSound.GetSound(), Owner, AudioParams.Default.WithVolume(-2f));
        }

        SuicideKind ISuicideAct.Suicide(EntityUid victim, IChatManager chat)
        {
            var headCount = 0;

            if (_entities.TryGetComponent<SharedBodyComponent?>(victim, out var body))
            {
                var headSlots = body.GetSlotsOfType(BodyPartType.Head);

                foreach (var slot in headSlots)
                {
                    var part = slot.Part;

                    if (part == null ||
                        !body.TryDropPart(slot, out var dropped))
                    {
                        continue;
                    }

                    foreach (var droppedPart in dropped.Values)
                    {
                        if (droppedPart.PartType != BodyPartType.Head)
                        {
                            continue;
                        }

                        _storage.Insert(droppedPart.Owner);
                        headCount++;
                    }
                }
            }

            var othersMessage = headCount > 1
                ? Loc.GetString("microwave-component-suicide-multi-head-others-message", ("victim", victim))
                : Loc.GetString("microwave-component-suicide-others-message", ("victim", victim));

            victim.PopupMessageOtherClients(othersMessage);

            var selfMessage = headCount > 1
                ? Loc.GetString("microwave-component-suicide-multi-head-message")
                : Loc.GetString("microwave-component-suicide-message");

            victim.PopupMessage(selfMessage);

            _currentCookTimerTime = 10;
            ClickSound();
            _uiDirty = true;
            Wzhzhzh();
            return SuicideKind.Heat;
        }
    }
}
