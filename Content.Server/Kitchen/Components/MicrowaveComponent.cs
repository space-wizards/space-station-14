using System.Linq;
using Content.Server.Chemistry.Components.SolutionManager;
using Content.Server.Chemistry.EntitySystems;
using Content.Server.Power.Components;
using Content.Server.Temperature.Components;
using Content.Server.Temperature.Systems;
using Content.Server.UserInterface;
using Content.Shared.FixedPoint;
using Content.Shared.Kitchen;
using Content.Shared.Kitchen.Components;
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
    public sealed class MicrowaveComponent : SharedMicrowaveComponent
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
        public bool Broken;

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

        public bool Powered => !_entities.TryGetComponent(Owner, out ApcPowerReceiverComponent? receiver) || receiver.Powered;

        private bool HasContents => Storage.ContainedEntities.Count > 0;

        public bool UIDirty = true;
        private bool _lostPower;
        private int _currentCookTimeButtonIndex;

        public void DirtyUi()
        {
            UIDirty = true;
        }

        public Container Storage = default!;

        [ViewVariables] private BoundUserInterface? UserInterface => Owner.GetUIOrNull(MicrowaveUiKey.Key);

        protected override void Initialize()
        {
            base.Initialize();

            _currentCookTimerTime = _cookTimeDefault;

            Storage = ContainerHelpers.EnsureContainer<Container>(Owner, "microwave_entity_container",
                out _);

            if (UserInterface != null)
            {
                UserInterface.OnReceiveMessage += UserInterfaceOnReceiveMessage;
            }
        }

        public void SetCookTime(uint cookTime)
        {
            _currentCookTimerTime = cookTime;
            UIDirty = true;
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
                        UIDirty = true;
                    }

                    break;
                case MicrowaveEjectSolidIndexedMessage msg:
                    if (HasContents)
                    {
                        EjectSolid(msg.EntityID);
                        ClickSound();
                        UIDirty = true;
                    }
                    break;

                case MicrowaveSelectCookTimeMessage msg:
                    _currentCookTimeButtonIndex = msg.ButtonIndex;
                    _currentCookTimerTime = msg.NewCookTime;
                    ClickSound();
                    UIDirty = true;
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
                UIDirty = true;
            }

            if (_busy && Broken)
            {
                SetAppearance(MicrowaveVisualState.Broken);
                //we broke while we were cooking/busy!
                _lostPower = true;
                EjectSolids();
                _busy = false;
                UIDirty = true;
            }

            if (UIDirty)
            {
                UserInterface?.SetState(new MicrowaveUpdateUserInterfaceState
                (
                    Storage.ContainedEntities.Select(item => item).ToArray(),
                    _busy,
                    _currentCookTimeButtonIndex,
                    _currentCookTimerTime
                ));
                UIDirty = false;
            }
        }

        public void SetAppearance(MicrowaveVisualState state)
        {
            var finalState = state;
            if (Broken)
            {
                finalState = MicrowaveVisualState.Broken;
            }

            if (_entities.TryGetComponent(Owner, out AppearanceComponent? appearance))
            {
                appearance.SetData(PowerDeviceVisuals.VisualState, finalState);
            }
        }

        // ReSharper disable once InconsistentNaming
        // ReSharper disable once IdentifierTypo
        public void Wzhzhzh()
        {
            if (!HasContents)
            {
                return;
            }

            _busy = true;
            // Convert storage into Dictionary of ingredients
            var solidsDict = new Dictionary<string, int>();
            var reagentDict = new Dictionary<string, FixedPoint2>();
            foreach (var item in Storage.ContainedEntities)
            {
                // special behavior when being microwaved ;)
                var ev = new BeingMicrowavedEvent(Owner);
                _entities.EventBus.RaiseLocalEvent(item, ev, false);

                if (ev.Handled)
                {
                    _busy = false;
                    UIDirty = true;
                    return;
                }

                var tagSys = EntitySystem.Get<TagSystem>();

                if (tagSys.HasTag(item, "MicrowaveMachineUnsafe")
                    || tagSys.HasTag(item, "Metal"))
                {
                    // destroy microwave
                    Broken = true;
                    SetAppearance(MicrowaveVisualState.Broken);
                    SoundSystem.Play(ItemBreakSound.GetSound(), Filter.Pvs(Owner), Owner);
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
            SoundSystem.Play(_startCookingSound.GetSound(), Filter.Pvs(Owner), Owner, AudioParams.Default);
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

                SoundSystem.Play(_cookingCompleteSound.GetSound(), Filter.Pvs(Owner),
                    Owner, AudioParams.Default.WithVolume(-1f));

                SetAppearance(MicrowaveVisualState.Idle);
                _busy = false;

                UIDirty = true;
            });
            _lostPower = false;
            UIDirty = true;
        }

        /// <summary>
        ///     Adds temperature to every item in the microwave,
        ///     based on the time it took to microwave.
        /// </summary>
        /// <param name="time">The time on the microwave, in seconds.</param>
        public void AddTemperature(float time)
        {
            var solutionContainerSystem = EntitySystem.Get<SolutionContainerSystem>();
            foreach (var entity in Storage.ContainedEntities)
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
            for (var i = Storage.ContainedEntities.Count - 1; i >= 0; i--)
            {
                Storage.Remove(Storage.ContainedEntities.ElementAt(i));
            }
        }

        private void EjectSolid(EntityUid entityId)
        {
            if (_entities.EntityExists(entityId))
            {
                Storage.Remove(entityId);
            }
        }

        private void SubtractContents(FoodRecipePrototype recipe)
        {
            var totalReagentsToRemove = new Dictionary<string, FixedPoint2>(recipe.IngredientsReagents);
            var solutionContainerSystem = EntitySystem.Get<SolutionContainerSystem>();

            // this is spaghetti ngl
            foreach (var item in Storage.ContainedEntities)
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
                    foreach (var item in Storage.ContainedEntities)
                    {
                        var metaData = _entities.GetComponent<MetaDataComponent>(item);
                        if (metaData.EntityPrototype == null)
                        {
                            continue;
                        }

                        if (metaData.EntityPrototype.ID == recipeSolid.Key)
                        {
                            Storage.Remove(item);
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

        public void ClickSound()
        {
            SoundSystem.Play(_clickSound.GetSound(), Filter.Pvs(Owner), Owner, AudioParams.Default.WithVolume(-2f));
        }
    }

    public sealed class BeingMicrowavedEvent : HandledEntityEventArgs
    {
        public EntityUid Microwave;

        public BeingMicrowavedEvent(EntityUid microwave)
        {
            Microwave = microwave;
        }
    }
}
