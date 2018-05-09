using System;
using SS14.Shared.Interfaces.GameObjects;
using SS14.Shared.Utility;
using YamlDotNet.RepresentationModel;
using SS14.Server.GameObjects;
using Content.Server.GameObjects.EntitySystems;

namespace Content.Server.GameObjects.Components.Interactable.Tools
{
    /// <summary>
    /// Tool used to weld metal together, light things on fire, or melt into constituent parts
    /// </summary>
    class WelderComponent : ToolComponent, EntitySystems.IUse, EntitySystems.IExamine
    {
        SpriteComponent spriteComponent;

        public override string Name => "Welder";

        /// <summary>
        /// Maximum fuel capacity the welder can hold
        /// </summary>
        public float FuelCapacity { get; set; } = 50;

        /// <summary>
        /// Fuel the welder has to do tasks
        /// </summary>
        public float Fuel { get; set; } = 0;

        /// <summary>
        /// Default Cost of using the welder fuel for an action
        /// </summary>
        public const float DefaultFuelCost = 5;

        /// <summary>
        /// Rate at which we expunge fuel from ourselves when activated
        /// </summary>
        public const float FuelLossRate = 0.2f;

        /// <summary>
        /// Status of welder, whether it is ignited
        /// </summary>
        public bool Activated { get; private set; } = false;

        //private string OnSprite { get; set; }
        //private string OffSprite { get; set; }

        public override void Initialize()
        {
            base.Initialize();

            spriteComponent = Owner.GetComponent<SpriteComponent>();
        }

        public override void LoadParameters(YamlMappingNode mapping)
        {
            base.LoadParameters(mapping);

            if (mapping.TryGetNode("Capacity", out YamlNode node))
            {
                FuelCapacity = node.AsFloat();
            }

            //if (mapping.TryGetNode("On", out node))
            //{
            //    OnSprite = node.AsString();
            //}

            //if (mapping.TryGetNode("Off", out node))
            //{
            //    OffSprite = node.AsString();
            //}

            if (mapping.TryGetNode("Fuel", out node))
            {
                Fuel = node.AsFloat();
            }
            else
            {
                Fuel = FuelCapacity;
            }
        }

        public override void Update(float frameTime)
        {
            if (!Activated)
            {
                return;
            }

            Fuel = Math.Max(Fuel - (FuelLossRate * frameTime), 0);

            if (Fuel == 0)
            {
                ToggleStatus();
            }
        }

        public bool CanUse(float value)
        {
            return Fuel > value;
        }

        public override bool CanUse()
        {
            return CanUse(DefaultFuelCost);
        }

        public bool CanActivate()
        {
            return Fuel > 0;
        }

        public bool UseEntity(IEntity user)
        {
            return ToggleStatus();
        }

        /// <summary>
        /// Deactivates welding tool if active, activates welding tool if possible
        /// </summary>
        /// <returns></returns>
        public bool ToggleStatus()
        {
            if (Activated)
            {
                Activated = false;
                // Layer 1 is the flame.
                spriteComponent.LayerSetVisible(1, false);
                return true;
            }
            else if (CanActivate())
            {
                Activated = true;
                spriteComponent.LayerSetVisible(1, true);
                return true;
            }
            else
            {
                return false;
            }
        }

        string IExamine.Examine()
        {
            if(Activated)
            {
                return "The welding tool is currently lit";
            }
            return null;
        }
    }
}
