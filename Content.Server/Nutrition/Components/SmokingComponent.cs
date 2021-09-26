using System.Threading.Tasks;
using Content.Server.Clothing.Components;
using Content.Shared.Interaction;
using Content.Shared.Smoking;
using Content.Shared.Temperature;
using Robust.Server.GameObjects;
using Robust.Shared.GameObjects;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.ViewVariables;

namespace Content.Server.Nutrition.Components
{
    /// <summary>
    /// This item acts as a representation for smokable consumables.
    ///
    /// To smoke a cigar, you need:
    /// <list type="bullet">
    /// <item><description> a hot item (implements IHotItem interface)</description></item>
    /// <item><description> that's a alight.</description></item>
    /// <item><description>  for the target cigar be Unlit. Lit cigars are already lit and butt's don't have any "fuel" left.</description></item>
    ///</list>
    /// TODO: Add reagents that interact when smoking
    /// TODO: Allow suicide via excessive Smoking
    /// </summary>
    [RegisterComponent]
    public class SmokingComponent : Component, IInteractUsing
    {
        public override string Name => "Smoking";

        private SharedBurningStates _currentState = SharedBurningStates.Unlit;

        [ComponentDependency] private readonly ClothingComponent? _clothingComponent = default!;
        [ComponentDependency] private readonly AppearanceComponent? _appearanceComponent = default!;

        /// <summary>
        /// Duration represents how long will this item last.
        /// Generally it ticks down whether it's time-based
        /// or consumption-based.
        /// </summary>
        [ViewVariables] [DataField("duration")]
        private int _duration = 30;

        /// <summary>
        /// What is the temperature of the cigar?
        ///
        /// For a regular cigar, the temp approaches around 400°C or 580°C
        /// dependant on where you measure.
        /// </summary>
        //[ViewVariables] [DataField("temperature")]
        //private float _temperature = 673.15f;

        [ViewVariables]
        public SharedBurningStates CurrentState
        {
            get => _currentState;
            set
            {
                _currentState = value;

                if (_clothingComponent != null)
                {
                    switch (_currentState)
                    {
                        case SharedBurningStates.Lit:
                            _clothingComponent.EquippedPrefix = "lit";
                            _clothingComponent.ClothingEquippedPrefix = "lit";
                            break;
                        default:
                            _clothingComponent.EquippedPrefix = "unlit";
                            _clothingComponent.ClothingEquippedPrefix = "unlit";
                            break;
                    }
                }

                _appearanceComponent?.SetData(SmokingVisuals.Smoking, _currentState);
            }
        }

        // TODO: ECS this method and component.
        async Task<bool> IInteractUsing.InteractUsing(InteractUsingEventArgs eventArgs)
        {
            if (CurrentState != SharedBurningStates.Unlit)
                return false;

            var isHotEvent = new IsHotEvent();
            Owner.EntityManager.EventBus.RaiseLocalEvent(eventArgs.Using.Uid, isHotEvent, false);

            if (!isHotEvent.IsHot)
                return false;

            CurrentState = SharedBurningStates.Lit;
            // TODO More complex handling of cigar consumption
            Owner.SpawnTimer(_duration * 1000, () => CurrentState = SharedBurningStates.Burnt);
            return true;

        }
    }
}
