using System.Threading.Tasks;
using Content.Server.GameObjects.Components.GUI;
using Content.Server.GameObjects.Components.Items.Clothing;
using Content.Server.GameObjects.Components.Items.Storage;
using Content.Shared.GameObjects.Components;
using Content.Shared.GameObjects.Components.Inventory;
using Content.Shared.Interfaces.GameObjects.Components;
using Robust.Server.GameObjects;
using Robust.Shared.GameObjects;
using Robust.Shared.GameObjects.Components.Timers;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Serialization;
using Robust.Shared.ViewVariables;

namespace Content.Server.GameObjects.Components.Nutrition
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
    public class SmokingComponent : Component, IInteractUsing, IHotItem
    {
        public override string Name => "Smoking";

        private SharedSmokingStates _currentState;
        private ClothingComponent _clothingComponent;

        /// <summary>
        /// Duration represents how long will this item last.
        /// Generally it ticks down whether it's time-based
        /// or consumption-based.
        /// </summary>
        [ViewVariables(VVAccess.ReadOnly)]
        private int _duration;

        /// <summary>
        /// What is the temperature of the cigar?
        ///
        /// For a regular cigar, the temp approaches around 400°C or 580°C
        /// dependant on where you measure.
        /// </summary>
        [ViewVariables]
        private float _temperature;

        [ViewVariables]
        private SharedSmokingStates CurrentState
        {
            get => _currentState;
            set
            {
                _currentState = value;

                switch (_currentState)
                {
                    case SharedSmokingStates.Lit:
                        _clothingComponent.EquippedPrefix = "lit";
                        _clothingComponent.ClothingEquippedPrefix = "lit";
                        break;
                    default:
                        _clothingComponent.EquippedPrefix = "unlit";
                        _clothingComponent.ClothingEquippedPrefix = "unlit";
                        break;
                }

                if (Owner.TryGetComponent(out AppearanceComponent appearance))
                {
                    appearance.SetData(SmokingVisuals.Smoking, _currentState);
                }
            }
        }

        public override void Initialize()
        {
            base.Initialize();

            Owner.TryGetComponent(out _clothingComponent);
            _currentState = SharedSmokingStates.Unlit;
        }

        public override void ExposeData(ObjectSerializer serializer)
        {
            base.ExposeData(serializer);
            serializer.DataField(ref _duration, "duration", 30);
            serializer.DataField(ref _temperature, "temperature", 673.15f);
        }


        public async Task<bool> InteractUsing(InteractUsingEventArgs eventArgs)
        {
            if (eventArgs.Using.TryGetComponent(out IHotItem lighter)
                && lighter.IsCurrentlyHot()
                && CurrentState == SharedSmokingStates.Unlit
            )
            {
                CurrentState = SharedSmokingStates.Lit;
                // TODO More complex handling of cigar consumption
                Owner.SpawnTimer(_duration * 1000, () => CurrentState = SharedSmokingStates.Burnt);
                return true;
            }

            return false;
        }

        private static bool IsItemEquippedInSlot(ItemComponent clothingComponent, IEntity user,
            EquipmentSlotDefines.Slots slot)
        {
            return user.TryGetComponent<InventoryComponent>(out var inventoryComponent)
                   && inventoryComponent.GetSlotItem(slot) == clothingComponent;
        }

        public bool IsCurrentlyHot()
        {
            return _currentState == SharedSmokingStates.Lit;
        }
    }
}
