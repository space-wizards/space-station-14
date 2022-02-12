using System.Linq;
using System.Threading.Tasks;
using Content.Server.Act;
using Content.Server.Chat.Managers;
using Content.Server.Chemistry.Components.SolutionManager;
using Content.Server.Chemistry.EntitySystems;
using Content.Server.Hands.Components;
using Content.Server.Popups;
using Content.Server.Power.Components;
using Content.Server.Temperature.Components;
using Content.Server.Temperature.Systems;
using Content.Server.UserInterface;
using Content.Shared.Acts;
using Content.Shared.Body.Components;
using Content.Shared.Body.Part;
using Content.Shared.FixedPoint;
using Content.Shared.Interaction;
using Content.Shared.Item;
using Content.Shared.Kitchen;
using Content.Shared.Kitchen.Components;
using Content.Shared.Popups;
using Content.Shared.Power;
using Content.Shared.Sound;
using Content.Shared.Tag;
using Robust.Server.GameObjects;
using Robust.Shared.Audio;
using Robust.Shared.Containers;
using Robust.Shared.Player;

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

        public SoundSpecifier ItemBreakSound = new SoundPathSpecifier("/Audio/Effects/clang.ogg");

        #endregion YAMLSERIALIZE

        [ViewVariables] private bool _busy = false;
        private bool _broken;

        /// <summary>
        /// This is a fixed offset of 5.
        /// The cook times for all recipes should be divisible by 5,with a minimum of 1 second.
        /// For right now, I don't think any recipe cook time should be greater than 60 seconds.
        /// </summary>
        [ViewVariables] private uint _currentCookTimerTime = 1;

        /// <summary>
        ///     The max temperature that this microwave can heat objects to.
        /// </summary>
        [DataField("temperatureUpperThreshold")]
        public float TemperatureUpperThreshold = 373.15f;

        private bool Powered => !_entities.TryGetComponent(Owner, out ApcPowerReceiverComponent? receiver) || receiver.Powered;

        private bool HasContents => _storage.ContainedEntities.Count > 0;

        private bool _uiDirty = true;
        private bool _lostPower;
        private int _currentCookTimeButtonIndex;

        public void DirtyUi()
        {
            _uiDirty = true;
        }

        private Container _storage = default!;

        [ViewVariables] private BoundUserInterface? UserInterface => Owner.GetUIOrNull(MicrowaveUiKey.Key);

        protected override void Initialize()
        {
            base.Initialize();

            _currentCookTimerTime = _cookTimeDefault;

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
                EjectSolids();
                _busy = false;
                _uiDirty = true;
            }

            if (_busy && _broken)
            {
                SetAppearance(MicrowaveVisualState.Broken);
                //we broke while we were cooking/busy!
                _lostPower = true;
                EjectSolids();
                _busy = false;
                _uiDirty = true;
            }

            if (_uiDirty)
            {
                UserInterface?.SetState(new MicrowaveUpdateUserInterfaceState
                (
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
            var reagentDict = new Dictionary<string, FixedPoint2>();
            foreach (var item in _storage.ContainedEntities)
            {
                // special behavior when being microwaved ;)
                var ev = new BeingMicrowavedEvent(Owner);
                _entities.EventBus.RaiseLocalEvent(item, ev, false);

                if (ev.Handled)
                    return;

                var tagSys = EntitySystem.Get<TagSystem>();

                if (tagSys.HasTag(item, "MicrowaveMachineUnsafe")
                    || tagSys.HasTag(item, "Metal"))
                {
                    // destroy microwave
                    _broken = true;
                    SetAppearance(MicrowaveVisualState.Broken);
                    SoundSystem.Play(Filter.Pvs(Owner), ItemBreakSound.GetSound(), Owner);
                    return;
                }

                if (tagSys.HasTag(item, "MicrowaveSelfUnsafe")
                    || tagSys.HasTag(item, "Plastic"))
                {
                    _entities.SpawnEntity(_badRecipeName,
                        _entities.GetComponent<TransformComponent>(Owner).Coordinates);
                    _entities.QueueDeleteEntity(item);
                }

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

                if (!_entities.TryGetComponent<SolutionContainerManagerComponent>(item, out var solMan))
                    continue;

                foreach (var (_, solution) in solMan.Solutions)
                {
                    foreach (var reagent in solution.Contents)
                    {
                        if (reagentDict.ContainsKey(reagent.ReagentId))
                            reagentDict[reagent.ReagentId] += reagent.Quantity;
                        else
                            reagentDict.Add(reagent.ReagentId, reagent.Quantity);
                    }
                }
            }

            // Check recipes
            FoodRecipePrototype? recipeToCook = null;
            foreach (var r in _recipeManager.Recipes.Where(r =>
                CanSatisfyRecipe(r, solidsDict, reagentDict)))
            {
                recipeToCook = r;
            }

            SetAppearance(MicrowaveVisualState.Cooking);
            var time = _currentCookTimerTime * _cookTimeMultiplier;
            SoundSystem.Play(Filter.Pvs(Owner), _startCookingSound.GetSound(), Owner, AudioParams.Default);
            Owner.SpawnTimer((int) (_currentCookTimerTime * _cookTimeMultiplier), () =>
            {
                if (_lostPower)
                {
                    return;
                }

                AddTemperature(time);

                if (recipeToCook != null)
                {
                    SubtractContents(recipeToCook);
                    _entities.SpawnEntity(recipeToCook.Result,
                        _entities.GetComponent<TransformComponent>(Owner).Coordinates);
                }

                EjectSolids();

                SoundSystem.Play(Filter.Pvs(Owner), _cookingCompleteSound.GetSound(), Owner,
                    AudioParams.Default.WithVolume(-1f));

                SetAppearance(MicrowaveVisualState.Idle);
                _busy = false;

                _uiDirty = true;
            });
            _lostPower = false;
            _uiDirty = true;
        }

        /// <summary>
        ///     Adds temperature to every item in the microwave,
        ///     based on the time it took to microwave.
        /// </summary>
        /// <param name="time">The time on the microwave, in seconds.</param>
        public void AddTemperature(float time)
        {
            var solutionContainerSystem = EntitySystem.Get<SolutionContainerSystem>();
            foreach (var entity in _storage.ContainedEntities)
            {
                if (_entities.TryGetComponent(entity, out TemperatureComponent? temp))
                {
                    EntitySystem.Get<TemperatureSystem>().ChangeHeat(entity, time / 10, false, temp);
                }

                if (_entities.TryGetComponent(entity, out SolutionContainerManagerComponent? solutions))
                {
                    foreach (var (_, solution) in solutions.Solutions)
                    {
                        if (solution.Temperature > TemperatureUpperThreshold)
                            continue;

                        solutionContainerSystem.AddThermalEnergy(entity, solution, time / 10);
                    }
                }
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
            var totalReagentsToRemove = new Dictionary<string, FixedPoint2>(recipe.IngredientsReagents);
            var solutionContainerSystem = EntitySystem.Get<SolutionContainerSystem>();

            // this is spaghetti ngl
            foreach (var item in _storage.ContainedEntities)
            {
                if (!_entities.TryGetComponent<SolutionContainerManagerComponent>(item, out var solMan))
                    continue;

                // go over every solution
                foreach (var (_, solution) in solMan.Solutions)
                {
                    foreach (var (reagent, _) in recipe.IngredientsReagents)
                    {
                        // removed everything
                        if (!totalReagentsToRemove.ContainsKey(reagent))
                            continue;

                        if (!solution.ContainsReagent(reagent))
                            continue;

                        var quant = solution.GetReagentQuantity(reagent);

                        if (quant >= totalReagentsToRemove[reagent])
                        {
                            quant = totalReagentsToRemove[reagent];
                            totalReagentsToRemove.Remove(reagent);
                        }
                        else
                        {
                            totalReagentsToRemove[reagent] -= quant;
                        }

                        solutionContainerSystem.TryRemoveReagent(item, solution, reagent, quant);
                    }
                }
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

        private bool CanSatisfyRecipe(FoodRecipePrototype recipe, Dictionary<string, int> solids, Dictionary<string, FixedPoint2> reagents)
        {
            if (_currentCookTimerTime != recipe.CookTime)
            {
                return false;
            }

            foreach (var solid in recipe.IngredientsSolids)
            {
                if (!solids.ContainsKey(solid.Key))
                    return false;

                if (solids[solid.Key] < solid.Value)
                    return false;
            }

            foreach (var reagent in recipe.IngredientsReagents)
            {
                if (!reagents.ContainsKey(reagent.Key))
                    return false;

                if (reagents[reagent.Key] < reagent.Value)
                    return false;
            }

            return true;
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

    public class BeingMicrowavedEvent : HandledEntityEventArgs
    {
        public EntityUid Microwave;

        public BeingMicrowavedEvent(EntityUid microwave)
        {
            Microwave = microwave;
        }
    }
}
