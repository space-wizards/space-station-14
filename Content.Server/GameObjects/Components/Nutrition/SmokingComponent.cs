using System.Threading.Tasks;
using Content.Server.GameObjects.Components.GUI;
using Content.Server.GameObjects.Components.Items.Clothing;
using Content.Server.GameObjects.Components.Items.Storage;
using Content.Shared.GameObjects.Components;
using Content.Shared.GameObjects.Components.Inventory;
using Content.Shared.GameObjects.Components.Nutrition;
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
    /// To smoke a non-electric cigar, you need:
    /// <list type="bullet">
    /// <item><description> a hot item (implements IHotItem interface)</description></item>
    /// <item><description> that's a alight.</description></item>
    /// <item><description>  for the target cigar be Unlit. Lit cigars are already lit and butt's don't have any "fuel" left.</description></item>
    /// <item><description>  you need to have cigar equipped as mask.</description></item>
    ///</list>
    /// TODO: Add reagents that interact when smoking
    /// TODO: Allow suicide via excessive Smoking
    /// TODO: E-cigarettes
    /// </summary>
    [RegisterComponent]
    public class SmokingComponent : Component, IInteractUsing, IHotItem
    {
        public override string Name => "Smoking";

        private SharedSmokingStates _currentState;
        private ClothingComponent _clothingComponent;

        /// <summary>
        /// Determines the type of cigarette.
        /// <list type="bullet">
        /// <item><term>true</term><description>Then this is an e-cig which can
        /// be turned on/off without needing to be light up.</description></item>
        /// <item><term>false</term><description>then the cigarette will need to be set alight
        /// with a heat source. And it will slowly drain until it becomes an unusable.</description></item>
        /// </list>
        /// </summary>
        private bool _electric;

        /// <summary>
        /// Duration represents how long will this item last.
        /// Generally it ticks down whether it's time-based
        /// or consumption-based.
        /// </summary>
        [ViewVariables(VVAccess.ReadOnly)]
        private int _duration;

        /// <summary>
        /// What is the temperature of the cigar?
        /// <list type="bullet">
        /// <item><description>Usually for e-cigs it's around 40-50°C</description></item>
        /// <item><description>For a regular cigar, the temp approaches around 400°C or 580°C
        /// dependant on where you measure.</description></item>
        /// </list>
        /// </summary>
        [ViewVariables]
        private float _temperature;

        [ViewVariables]
        public SharedSmokingStates CurrentState
        {
            get => _currentState;
            private set
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
            serializer.DataField(ref _electric, "electric", false);
            serializer.DataField(ref _duration, "duration", 30);
            serializer.DataField(ref _temperature, "temperature", 673.15f);
        }


        public async Task<bool> InteractUsing(InteractUsingEventArgs eventArgs)
        {
            if (eventArgs.Using.TryGetComponent(out IHotItem lighter)
                && lighter.IsCurrentlyHot()
                && CurrentState == SharedSmokingStates.Unlit
                && Owner.TryGetComponent<ClothingComponent>(out _clothingComponent)
                && IsItemEquippedInSlot(_clothingComponent, eventArgs.User, EquipmentSlotDefines.Slots.MASK)
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
            return !_electric && _currentState == SharedSmokingStates.Lit;
        }
    }
}
