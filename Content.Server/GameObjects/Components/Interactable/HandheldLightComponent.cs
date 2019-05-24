using Content.Server.GameObjects.Components.Power;
using Content.Server.GameObjects.EntitySystems;
using Content.Server.Interfaces.GameObjects;
using Content.Shared.GameObjects;
using Robust.Server.GameObjects;
using Robust.Server.GameObjects.Components.Container;
using Robust.Server.Interfaces.GameObjects;
using Robust.Shared.Enums;
using Robust.Shared.GameObjects;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Utility;
using Robust.Shared.ViewVariables;

namespace Content.Server.GameObjects.Components.Interactable
{
    /// <summary>
    ///     Component that represents a handheld lightsource which can be toggled on and off.
    /// </summary>
    internal class HandheldLightComponent : Component, IUse, IExamine, IAttackBy, IMapInit
    {
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

            eventArgs.User.GetComponent<IHandsComponent>().Drop(eventArgs.AttackWith, _cellContainer);

            return _cellContainer.Insert(eventArgs.AttackWith);
        }

        void IExamine.Examine(FormattedMessage message)
        {
            if (Activated)
            {
                message.AddText("The light is currently on.");
            }
        }

        bool IUse.UseEntity(UseEntityEventArgs eventArgs)
        {
            return ToggleStatus();
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
        public bool ToggleStatus()
        {
            // Update the activation state.
            Activated = !Activated;

            // Update sprite and light states to match the activation.
            if (Activated)
            {
                SetState(LightState.On);
            }
            else
            {
                SetState(LightState.Off);
            }

            // Toggle always succeeds.
            return true;
        }

        public void TurnOff()
        {
            if (!Activated) return;

            SetState(LightState.Off);
            Activated = false;
        }

        public void TurnOn()
        {
            if (Activated) return;

            var cell = Cell;
            if (cell == null) return;

            // To prevent having to worry about frame time in here.
            // Let's just say you need a whole second of charge before you can turn it on.
            // Simple enough.
            if (cell.AvailableCharge(1) < Wattage) return;

            SetState(LightState.On);
        }

        private void SetState(LightState newState)
        {
            _spriteComponent.LayerSetVisible(1, newState == LightState.On);
            _pointLight.State = newState;
            if (_clothingComponent != null)
            {
                _clothingComponent.ClothingEquippedPrefix = newState.ToString();
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
            if (Cell == null) return;

            var cell = Cell;

            if (!_cellContainer.Remove(cell.Owner)) return;

            if (!user.TryGetComponent(out HandsComponent hands)
                || !hands.PutInHand(cell.Owner.GetComponent<ItemComponent>()))
                cell.Owner.Transform.GridPosition = user.Transform.GridPosition;
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
            var cell = Owner.EntityManager.SpawnEntity("PowerCellSmallHyper");
            _cellContainer.Insert(cell);
        }
    }
}
