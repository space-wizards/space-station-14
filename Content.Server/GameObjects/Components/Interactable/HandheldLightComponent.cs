using System.Collections.Generic;
using Content.Server.GameObjects.Components.Power;
using Content.Server.GameObjects.EntitySystems;
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
    internal class HandheldLightComponent : Component, IUse, IExamine, IVerbProviderComponent
    {
        public const float Wattage = 10;
        [ViewVariables] private ContainerSlot _cellContainer;
        private PointLightComponent _pointLight;
        private SpriteComponent _spriteComponent;

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
            if (Cell == null)
            {
                return;
            }

            var cell = Cell;

            if (!_cellContainer.Remove(cell.Owner))
            {
                return;
            }

            if (!user.TryGetComponent(out HandsComponent hands)
                || !hands.PutInHand(cell.Owner.GetComponent<ItemComponent>()))
            {
                cell.Owner.Transform.LocalPosition = user.Transform.LocalPosition;
            }
        }

        public class EjectCellVerb : Verb
        {
            public override string GetName(IEntity user, IComponent component)
            {
                var flashlight = (HandheldLightComponent) component;
                return flashlight.Cell == null ? "Eject cell (cell missing)" : "Eject cell";
            }

            public override bool IsDisabled(IEntity user, IComponent component)
            {
                var flashlight = (HandheldLightComponent) component;
                return flashlight.Cell == null;
            }

            public override void Activate(IEntity user, IComponent component)
            {
                var flashlight = (HandheldLightComponent) component;
                flashlight.EjectCell(user);
            }
        }

        public IEnumerable<Verb> GetVerbs(IEntity userEntity)
        {
            yield return new EjectCellVerb();
        }
    }
}
