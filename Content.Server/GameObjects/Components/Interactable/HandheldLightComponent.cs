using Content.Server.GameObjects.Components.Power;
using Content.Server.GameObjects.Components.Sound;
using Content.Server.GameObjects.EntitySystems;
using Content.Server.Interfaces.GameObjects;
using Content.Shared.GameObjects;
using Content.Shared.Interfaces;
using Robust.Server.GameObjects;
using Robust.Server.GameObjects.Components.Container;
using Robust.Server.Interfaces.GameObjects;
using Robust.Shared.GameObjects;
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
    internal class HandheldLightComponent : Component, IUse, IExamine, IAttackBy, IMapInit
    {
#pragma warning disable 649
        [Dependency] private readonly ISharedNotifyManager _notifyManager;
        [Dependency] private readonly ILocalizationManager _localizationManager;
#pragma warning restore 649

        public const float Wattage = 10;
        [ViewVariables] private ContainerSlot _cellContainer;
        private PointLightComponent _pointLight;
        private SpriteComponent _spriteComponent;
        private ClothingComponent _clothingComponent;

        [ViewVariables]
        private PowerCellComponent Cell
        {
            get
            {
                if (_cellContainer.ContainedEntity == null) return null;

                _cellContainer.ContainedEntity.TryGetComponent(out PowerCellComponent cell);
                return cell;
            }
        }
        

        public override string Name => "HandheldLight";

        /// <summary>
        ///     Status of light, whether or not it is emitting light.
        /// </summary>
        [ViewVariables]
        public bool Activated { get; private set; }

        bool IAttackBy.AttackBy(AttackByEventArgs eventArgs)
        {
            if (!eventArgs.AttackWith.HasComponent<PowerCellComponent>()) return false;

            if (Cell != null) return false;

            var handsComponent = eventArgs.User.GetComponent<IHandsComponent>();

            if (!handsComponent.Drop(eventArgs.AttackWith, _cellContainer))
            {
                return false;
            }

            if (Owner.TryGetComponent(out SoundComponent soundComponent))
            {
                soundComponent.Play("/Audio/items/weapons/pistol_magin.ogg");
            }

            return true;

        }

        void IExamine.Examine(FormattedMessage message)
        {
            var loc = IoCManager.Resolve<ILocalizationManager>();

            if (Activated)
            {
                message.AddMarkup(loc.GetString("The light is currently [color=darkgreen]on[/color]."));
            }
        }

        bool IUse.UseEntity(UseEntityEventArgs eventArgs)
        {
            return ToggleStatus(eventArgs.User);
        }

        public override void Initialize()
        {
            base.Initialize();

            _pointLight = Owner.GetComponent<PointLightComponent>();
            _spriteComponent = Owner.GetComponent<SpriteComponent>();
            Owner.TryGetComponent(out _clothingComponent);
            _cellContainer =
                ContainerManagerComponent.Ensure<ContainerSlot>("flashlight_cell_container", Owner, out var existed);
        }

        /// <summary>
        ///     Illuminates the light if it is not active, extinguishes it if it is active.
        /// </summary>
        /// <returns>True if the light's status was toggled, false otherwise.</returns>
        private bool ToggleStatus(IEntity user)
        {
            // Update sprite and light states to match the activation.
            if (Activated)
            {
                TurnOff();
            }
            else
            {
                TurnOn(user);
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

            if (Owner.TryGetComponent(out SoundComponent soundComponent))
            {
                soundComponent.Play("/Audio/items/flashlight_toggle.ogg");
            }
        }

        private void TurnOn(IEntity user)
        {
            if (Activated)
            {
                return;
            }

            var cell = Cell;
            SoundComponent soundComponent;
            if (cell == null)
            {
                if (Owner.TryGetComponent(out soundComponent))
                {
                    soundComponent.Play("/Audio/machines/button.ogg");
                }
                _notifyManager.PopupMessage(Owner, user, _localizationManager.GetString("Cell missing..."));
                return;
            }

            // To prevent having to worry about frame time in here.
            // Let's just say you need a whole second of charge before you can turn it on.
            // Simple enough.
            if (cell.AvailableCharge(1) < Wattage)
            {
                if (Owner.TryGetComponent(out soundComponent))
                {
                    soundComponent.Play("/Audio/machines/button.ogg");
                }
                _notifyManager.PopupMessage(Owner, user, _localizationManager.GetString("Dead cell..."));
                return;
            }

            Activated = true;
            SetState(true);

            if (Owner.TryGetComponent(out soundComponent))
            {
                soundComponent.Play("/Audio/items/flashlight_toggle.ogg");
            }
        }

        private void SetState(bool on)
        {
            _spriteComponent.LayerSetVisible(1, on);
            _pointLight.Enabled = on;
            if (_clothingComponent != null)
            {
                _clothingComponent.ClothingEquippedPrefix = on ? "On" : "Off";
            }
        }

        public void OnUpdate(float frameTime)
        {
            if (!Activated) return;

            var cell = Cell;
            if (cell == null || !cell.TryDeductWattage(Wattage, frameTime)) TurnOff();
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

            if (!user.TryGetComponent(out HandsComponent hands))
            {
                return;
            }

            if (!hands.PutInHand(cell.Owner.GetComponent<ItemComponent>()))
            {
                cell.Owner.Transform.GridPosition = user.Transform.GridPosition;
            }

            if (Owner.TryGetComponent(out SoundComponent soundComponent))
            {
                soundComponent.Play("/Audio/items/weapons/pistol_magout.ogg");
            }
        }

        [Verb]
        public sealed class EjectCellVerb : Verb<HandheldLightComponent>
        {
            protected override string GetText(IEntity user, HandheldLightComponent component)
            {
                return component.Cell == null ? "Eject cell (cell missing)" : "Eject cell";
            }

            protected override VerbVisibility GetVisibility(IEntity user, HandheldLightComponent component)
            {
                return component.Cell == null ? VerbVisibility.Disabled : VerbVisibility.Visible;
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
            var cell = Owner.EntityManager.SpawnEntity("PowerCellSmallHyper", Owner.Transform.GridPosition);
            _cellContainer.Insert(cell);
        }
    }
}
