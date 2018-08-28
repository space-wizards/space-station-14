using Content.Server.GameObjects.EntitySystems;
using SS14.Server.GameObjects;
using SS14.Shared.Enums;
using SS14.Shared.GameObjects;
using SS14.Shared.Interfaces.GameObjects;
using SS14.Shared.Serialization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Content.Server.GameObjects.Components.Interactable
{
    /// <summary>
    /// Component that represents a handheld lightsource which can be toggled on and off.
    /// </summary>
    class HandheldLightComponent : Component, EntitySystems.IUse, EntitySystems.IExamine
    {
        PointLightComponent pointLight;
        SpriteComponent spriteComponent;

        public override string Name => "HandheldLight";

        /// <summary>
        /// Status of light, whether or not it is emitting light.
        /// </summary>
        public bool Activated { get; private set; } = false;

        public override void Initialize()
        {
            base.Initialize();

            pointLight = Owner.GetComponent<PointLightComponent>();
            spriteComponent = Owner.GetComponent<SpriteComponent>();
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
                spriteComponent.LayerSetState(0, "lantern_on");
                pointLight.State = LightState.On;
            }
            else
            {
                spriteComponent.LayerSetState(0, "lantern_off");
                pointLight.State = LightState.Off;
            }

            // Toggle always succeeds.
            return true;
        }

        string IExamine.Examine()
        {
            if (Activated)
            {
                return "The light is currently on.";
            }

            return null;
        }
    }
}
