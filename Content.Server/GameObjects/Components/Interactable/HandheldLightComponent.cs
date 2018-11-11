using Content.Server.GameObjects.EntitySystems;
using Content.Server.GameObjects.Components.Power;
using SS14.Server.GameObjects;
using SS14.Server.GameObjects.Components.Container;
using SS14.Shared.Enums;
using SS14.Shared.Interfaces.GameObjects;
using SS14.Shared.GameObjects;
using SS14.Shared.ViewVariables;

namespace Content.Server.GameObjects.Components.Interactable
{
    /// <summary>
    /// Component that represents a handheld lightsource which can be toggled on and off.
    /// </summary>
    class HandheldLightComponent : Component, IUse, IExamine
    {
        private PointLightComponent _pointLight;
        private SpriteComponent _spriteComponent;
        [ViewVariables] private ContainerSlot _cellContainer;

        private PowerCellComponent Cell
        {
            get
            {
                if (_cellContainer.ContainedEntity == null)
                {
                    return null;
                }

                _cellContainer.ContainedEntity.TryGetComponent(out PowerCellComponent cell);
                return cell;
            }
        }

        public override string Name => "HandheldLight";

        /// <summary>
        /// Status of light, whether or not it is emitting light.
        /// </summary>
        [ViewVariables]
        public bool Activated { get; private set; } = false;

        public const float Wattage = 10;

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

        bool IUse.UseEntity(IEntity user)
        {
            return ToggleStatus();
        }

        /// <summary>
        /// Illuminates the light if it is not active, extinguishes it if it is active.
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
            if (!Activated)
            {
                return;
            }

            _spriteComponent.LayerSetState(0, "lantern_off");
            _pointLight.State = LightState.Off;
            Activated = false;
        }

        public void TurnOn()
        {
            if (Activated)
            {
                return;
            }

            var cell = Cell;
            if (cell == null)
            {
                return;
            }

            // To prevent having to worry about frame time in here.
            // Let's just say you need a whole second of charge before you can turn it on.
            // Simple enough.
            if (cell.AvailableCharge(1) < Wattage)
            {
                return;
            }

            _spriteComponent.LayerSetState(0, "lantern_on");
            _pointLight.State = LightState.On;
        }

        string IExamine.Examine()
        {
            if (Activated)
            {
                return "The light is currently on.";
            }

            return null;
        }

        public void OnUpdate(float frameTime)
        {
            if (!Activated)
            {
                return;
            }

            var cell = Cell;
            if (cell == null || !cell.TryDeductWattage(Wattage, frameTime))
            {
                TurnOff();
            }
        }
    }
}
