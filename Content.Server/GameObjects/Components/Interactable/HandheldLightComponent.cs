#nullable enable
using System.Threading.Tasks;
using Content.Server.GameObjects.Components.Items.Clothing;
using Content.Server.GameObjects.Components.Items.Storage;
using Content.Server.GameObjects.Components.Power;
using Content.Shared.GameObjects.Components;
using Content.Shared.GameObjects.EntitySystems;
using Content.Shared.Interfaces;
using Content.Shared.Interfaces.GameObjects.Components;
using Robust.Server.GameObjects;
using Robust.Server.GameObjects.EntitySystems;
using Robust.Shared.GameObjects;
using Robust.Shared.GameObjects.Systems;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Localization;
using Robust.Shared.Serialization;
using Robust.Shared.Utility;
using Robust.Shared.ViewVariables;

namespace Content.Server.GameObjects.Components.Interactable
{
    /// <summary>
    ///     Component that represents a powered handheld light source which can be toggled on and off.
    /// </summary>
    [RegisterComponent]
    internal sealed class HandheldLightComponent : SharedHandheldLightComponent, IUse, IExamine, IInteractUsing
    {
        [ViewVariables(VVAccess.ReadWrite)] public float Wattage = 10f;
        [ViewVariables] private PowerCellSlotComponent _cellSlot = default!;
        private PowerCellComponent? Cell => _cellSlot.Cell;

        /// <summary>
        ///     Status of light, whether or not it is emitting light.
        /// </summary>
        [ViewVariables]
        public bool Activated { get; private set; }

        [ViewVariables] protected override bool HasCell => _cellSlot.HasCell;

        [ViewVariables(VVAccess.ReadWrite)] public string TurnOnSound = "/Audio/Items/flashlight_toggle.ogg";
        [ViewVariables(VVAccess.ReadWrite)] public string TurnOnFailSound = "/Audio/Machines/button.ogg";
        [ViewVariables(VVAccess.ReadWrite)] public string TurnOffSound = "/Audio/Items/flashlight_toggle.ogg";

        public override void ExposeData(ObjectSerializer serializer)
        {
            base.ExposeData(serializer);
            serializer.DataField(ref Wattage, "wattage", 10f);
            serializer.DataField(ref TurnOnSound, "turnOnSound", "/Audio/Items/flashlight_toggle.ogg");
            serializer.DataField(ref TurnOnFailSound, "turnOnFailSound", "/Audio/Machines/button.ogg");
            serializer.DataField(ref TurnOffSound, "turnOffSound", "/Audio/Items/flashlight_toggle.ogg");
        }

        async Task<bool> IInteractUsing.InteractUsing(InteractUsingEventArgs eventArgs)
        {
            if (_cellSlot.InsertCell(eventArgs.Using))
            {
                Dirty();
                return true;
            }
            return false;

            // if (Cell != null) return false;
            //
            // var handsComponent = eventArgs.User.GetComponent<IHandsComponent>();
            //
            // if (!handsComponent.Drop(eventArgs.Using, _cellContainer))
            // {
            //     return false;
            // }
            //
            // EntitySystem.Get<AudioSystem>().PlayFromEntity("/Audio/Items/pistol_magin.ogg", Owner);
            //
            //
            // Dirty();
            //
            // return true;
        }

        void IExamine.Examine(FormattedMessage message, bool inDetailsRange)
        {
            if (Activated)
            {
                message.AddMarkup(Loc.GetString("The light is currently [color=darkgreen]on[/color]."));
            }
            else
            {
                message.AddMarkup(Loc.GetString("The light is currently [color=darkred]off[/color]."));
            }
        }

        bool IUse.UseEntity(UseEntityEventArgs eventArgs)
        {
            return ToggleStatus(eventArgs.User);
        }

        public override void Initialize()
        {
            base.Initialize();

            Owner.EnsureComponent<PointLightComponent>();
            _cellSlot = Owner.EnsureComponent<PowerCellSlotComponent>();

            Dirty();
        }

        /// <summary>
        ///     Illuminates the light if it is not active, extinguishes it if it is active.
        /// </summary>
        /// <returns>True if the light's status was toggled, false otherwise.</returns>
        private bool ToggleStatus(IEntity user)
        {
            return Activated ? TurnOff() : TurnOn(user);
        }

        private bool TurnOff(bool makeNoise = true)
        {
            if (!Activated)
            {
                return false;
            }

            SetState(false);
            Activated = false;

            if (makeNoise)
            {
                EntitySystem.Get<AudioSystem>().PlayFromEntity(TurnOffSound, Owner);
            }

            return true;
        }

        private bool TurnOn(IEntity user)
        {
            if (Activated)
            {
                return false;
            }

            if (Cell == null)
            {
                EntitySystem.Get<AudioSystem>().PlayFromEntity(TurnOnFailSound, Owner);
                Owner.PopupMessage(user, Loc.GetString("Cell missing..."));
                return false;
            }

            // To prevent having to worry about frame time in here.
            // Let's just say you need a whole second of charge before you can turn it on.
            // Simple enough.
            if (Wattage > Cell.CurrentCharge)
            {
                EntitySystem.Get<AudioSystem>().PlayFromEntity(TurnOnFailSound, Owner);
                Owner.PopupMessage(user, Loc.GetString("Dead cell..."));
                return false;
            }

            Activated = true;
            SetState(true);

            EntitySystem.Get<AudioSystem>().PlayFromEntity(TurnOnSound, Owner);
            return true;
        }

        private void SetState(bool on)
        {
            if (Owner.TryGetComponent(out SpriteComponent? sprite))
            {
                sprite.LayerSetVisible(1, on);
            }

            if (Owner.TryGetComponent(out PointLightComponent? light))
            {
                light.Enabled = on;
            }

            if (Owner.TryGetComponent(out ClothingComponent? clothing))
            {
                clothing.ClothingEquippedPrefix = on ? "On" : "Off";
            }

            if (Owner.TryGetComponent(out ItemComponent? item))
            {
                item.EquippedPrefix = on ? "on" : "off";
            }
        }

        public void OnUpdate(float frameTime)
        {
            if (!Activated) return;
            if (Cell == null)
            {
                TurnOff(false);
                return;
            }

            var appearanceComponent = Owner.GetComponent<AppearanceComponent>();

            if (Cell.MaxCharge - Cell.CurrentCharge < Cell.MaxCharge * 0.70)
            {
                appearanceComponent.SetData(HandheldLightVisuals.Power, HandheldLightPowerStates.FullPower);
            }
            else if (Cell.MaxCharge - Cell.CurrentCharge < Cell.MaxCharge * 0.90)
            {
                appearanceComponent.SetData(HandheldLightVisuals.Power, HandheldLightPowerStates.LowPower);
            }
            else
            {
                appearanceComponent.SetData(HandheldLightVisuals.Power, HandheldLightPowerStates.Dying);
            }

            if (!Cell.TryUseCharge(Wattage * frameTime)) TurnOff(false);

            Dirty();
        }

        public override ComponentState GetComponentState()
        {
            if (Cell == null)
            {
                return new HandheldLightComponentState(null, false);
            }

            if (Wattage > Cell.CurrentCharge)
            {
                // Practically zero.
                // This is so the item status works correctly.
                return new HandheldLightComponentState(0, HasCell);
            }

            return new HandheldLightComponentState(Cell.CurrentCharge / Cell.MaxCharge, HasCell);
        }
    }
}
