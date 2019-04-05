using Content.Server.GameObjects.Components.Power;
using Content.Server.GameObjects.EntitySystems;
using Content.Server.Interfaces.GameObjects;
using Content.Shared.GameObjects;
using SS14.Server.GameObjects;
using SS14.Server.GameObjects.Components.Container;
using SS14.Shared.Enums;
using SS14.Shared.GameObjects;
using SS14.Shared.Interfaces.GameObjects;
using SS14.Shared.ViewVariables;

namespace Content.Server.GameObjects.Components.Interactable
{
    /// <summary>
    ///     Component that represents a handheld lightsource which can be toggled on and off.
    /// </summary>
    internal class HandheldLightComponent : Component, IUse, IExamine, IAttackBy
    {
        public const float Wattage = 10;
        [ViewVariables] private ContainerSlot _cellContainer;
        private PointLightComponent _pointLight;
        private SpriteComponent _spriteComponent;

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

        string IExamine.Examine()
        {
            if (Activated) return "The light is currently on.";

            return null;
        }

        bool IUse.UseEntity(IEntity user)
        {
            return ToggleStatus();
        }

        public override void Initialize()
        {
            base.Initialize();

            _pointLight = Owner.GetComponent<PointLightComponent>();
            _spriteComponent = Owner.GetComponent<SpriteComponent>();
            _cellContainer =
                ContainerManagerComponent.Ensure<ContainerSlot>("flashlight_cell_container", Owner, out var existed);

            if (!existed)
            {
                var cell = Owner.EntityManager.SpawnEntity("PowerCellSmallHyper");
                _cellContainer.Insert(cell);
            }
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
                _spriteComponent.LayerSetState(0, "lantern_on");
                _pointLight.State = LightState.On;
            }
            else
            {
                _spriteComponent.LayerSetState(0, "lantern_off");
                _pointLight.State = LightState.Off;
            }

            // Toggle always succeeds.
            return true;
        }

        public void TurnOff()
        {
            if (!Activated) return;

            _spriteComponent.LayerSetState(0, "lantern_off");
            _pointLight.State = LightState.Off;
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

            _spriteComponent.LayerSetState(0, "lantern_on");
            _pointLight.State = LightState.On;
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

            protected override bool IsDisabled(IEntity user, HandheldLightComponent component)
            {
                return component.Cell == null;
            }

            protected override void Activate(IEntity user, HandheldLightComponent component)
            {
                component.EjectCell(user);
            }
        }
    }
}
