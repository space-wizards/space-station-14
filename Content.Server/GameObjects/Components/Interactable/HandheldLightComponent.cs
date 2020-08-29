#nullable enable
using System.Threading.Tasks;
using Content.Server.GameObjects.Components.GUI;
using Content.Server.GameObjects.Components.Items.Clothing;
using Content.Server.GameObjects.Components.Items.Storage;
using Content.Server.GameObjects.Components.Power;
using Content.Server.Interfaces.GameObjects.Components.Items;
using Content.Shared.GameObjects.Components;
using Content.Shared.GameObjects.EntitySystems;
using Content.Shared.GameObjects.Verbs;
using Content.Shared.Interfaces;
using Content.Shared.Interfaces.GameObjects.Components;
using Robust.Server.GameObjects;
using Robust.Server.GameObjects.Components.Container;
using Robust.Server.GameObjects.EntitySystems;
using Robust.Server.Interfaces.GameObjects;
using Robust.Shared.GameObjects;
using Robust.Shared.GameObjects.Systems;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Localization;
using Robust.Shared.Utility;
using Robust.Shared.ViewVariables;

namespace Content.Server.GameObjects.Components.Interactable
{
    /// <summary>
    ///     Component that represents a handheld lightsource which can be toggled on and off.
    /// </summary>
    [RegisterComponent]
    internal sealed class HandheldLightComponent : SharedHandheldLightComponent, IUse, IExamine, IInteractUsing,
        IMapInit
    {
        [Dependency] private readonly ISharedNotifyManager _notifyManager = default!;

        [ViewVariables(VVAccess.ReadWrite)] public float Wattage { get; set; } = 10;
        [ViewVariables] private ContainerSlot _cellContainer = default!;

        [ViewVariables]
        private BatteryComponent? Cell
        {
            get
            {
                if (_cellContainer.ContainedEntity == null) return null;
                if (_cellContainer.ContainedEntity.TryGetComponent(out BatteryComponent? cell))
                {
                    return cell;
                }

                return null;
            }
        }

        /// <summary>
        ///     Status of light, whether or not it is emitting light.
        /// </summary>
        [ViewVariables]
        public bool Activated { get; private set; }

        [ViewVariables] protected override bool HasCell => Cell != null;

        async Task<bool> IInteractUsing.InteractUsing(InteractUsingEventArgs eventArgs)
        {
            if (!eventArgs.Using.HasComponent<BatteryComponent>()) return false;

            if (Cell != null) return false;

            var handsComponent = eventArgs.User.GetComponent<IHandsComponent>();

            if (!handsComponent.Drop(eventArgs.Using, _cellContainer))
            {
                return false;
            }

            EntitySystem.Get<AudioSystem>().PlayFromEntity("/Audio/Items/pistol_magin.ogg", Owner);


            Dirty();

            return true;
        }

        void IExamine.Examine(FormattedMessage message, bool inDetailsRange)
        {
            if (Activated)
            {
                message.AddMarkup(Loc.GetString("The light is currently [color=darkgreen]on[/color]."));
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
            _cellContainer =
                ContainerManagerComponent.Ensure<ContainerSlot>("flashlight_cell_container", Owner, out _);

            Dirty();
        }

        /// <summary>
        ///     Illuminates the light if it is not active, extinguishes it if it is active.
        /// </summary>
        /// <returns>True if the light's status was toggled, false otherwise.</returns>
        private bool ToggleStatus(IEntity user)
        {
            var item = Owner.GetComponent<ItemComponent>();
            // Update sprite and light states to match the activation.
            if (Activated)
            {
                TurnOff();
                item.EquippedPrefix = "off";
            }
            else
            {
                TurnOn(user);
                item.EquippedPrefix = "on";
            }

            // Toggle always succeeds.
            return true;
        }

        private void TurnOff()
        {
            if (!Activated)
            {
                return;
            }

            SetState(false);
            Activated = false;

            EntitySystem.Get<AudioSystem>().PlayFromEntity("/Audio/Items/flashlight_toggle.ogg", Owner);
        }

        private void TurnOn(IEntity user)
        {
            if (Activated)
            {
                return;
            }

            var cell = Cell;
            if (cell == null)
            {
                EntitySystem.Get<AudioSystem>().PlayFromEntity("/Audio/Machines/button.ogg", Owner);

                _notifyManager.PopupMessage(Owner, user, Loc.GetString("Cell missing..."));
                return;
            }

            // To prevent having to worry about frame time in here.
            // Let's just say you need a whole second of charge before you can turn it on.
            // Simple enough.
            if (Wattage > cell.CurrentCharge)
            {
                EntitySystem.Get<AudioSystem>().PlayFromEntity("/Audio/Machines/button.ogg", Owner);
                _notifyManager.PopupMessage(Owner, user, Loc.GetString("Dead cell..."));
                return;
            }

            Activated = true;
            SetState(true);

            EntitySystem.Get<AudioSystem>().PlayFromEntity("/Audio/Items/flashlight_toggle.ogg", Owner);
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
        }

        public void OnUpdate(float frameTime)
        {
            if (!Activated || Cell == null) return;

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

            if (Cell == null || !Cell.TryUseCharge(Wattage * frameTime)) TurnOff();

            Dirty();
        }

        private void EjectCell(IEntity user)
        {
            if (Cell == null)
            {
                return;
            }

            var cell = Cell;

            if (!_cellContainer.Remove(cell.Owner))
            {
                return;
            }

            Dirty();

            if (!user.TryGetComponent(out HandsComponent? hands))
            {
                return;
            }

            if (!hands.PutInHand(cell.Owner.GetComponent<ItemComponent>()))
            {
                cell.Owner.Transform.GridPosition = user.Transform.GridPosition;
            }

            EntitySystem.Get<AudioSystem>().PlayFromEntity("/Audio/Items/pistol_magout.ogg", Owner);
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

        [Verb]
        public sealed class EjectCellVerb : Verb<HandheldLightComponent>
        {
            protected override void GetData(IEntity user, HandheldLightComponent component, VerbData data)
            {
                if (!ActionBlockerSystem.CanInteract(user))
                {
                    data.Visibility = VerbVisibility.Invisible;
                    return;
                }

                if (component.Cell == null)
                {
                    data.Text = Loc.GetString("Eject cell (cell missing)");
                    data.Visibility = VerbVisibility.Disabled;
                }
                else
                {
                    data.Text = Loc.GetString("Eject cell");
                }
            }

            protected override void Activate(IEntity user, HandheldLightComponent component)
            {
                component.EjectCell(user);
            }
        }

        void IMapInit.MapInit()
        {
            if (_cellContainer.ContainedEntity != null)
            {
                return;
            }

            var cell = Owner.EntityManager.SpawnEntity("PowerCellSmallStandard", Owner.Transform.GridPosition);
            _cellContainer.Insert(cell);
        }
    }
}
