using System;
using System.Text;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Utility;
using YamlDotNet.RepresentationModel;
using Robust.Server.GameObjects;
using Content.Server.GameObjects.EntitySystems;
using Robust.Shared.Maths;
using Robust.Shared.Serialization;
using Robust.Shared.ViewVariables;

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
        [ViewVariables(VVAccess.ReadWrite)]
        public float FuelCapacity
        {
            get => _fuelCapacity;
            set => _fuelCapacity = value;
        }
        private float _fuelCapacity = 50;

        /// <summary>
        /// Fuel the welder has to do tasks
        /// </summary>
        [ViewVariables(VVAccess.ReadWrite)]
        public float Fuel
        {
            get => _fuel;
            set => _fuel = value;
        }
        private float _fuel = 0;

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
        [ViewVariables]
        public bool Activated { get; private set; } = false;

        //private string OnSprite { get; set; }
        //private string OffSprite { get; set; }

        public override void Initialize()
        {
            base.Initialize();

            spriteComponent = Owner.GetComponent<SpriteComponent>();
        }

        public override void ExposeData(ObjectSerializer serializer)
        {
            base.ExposeData(serializer);

            serializer.DataField(ref _fuelCapacity, "Capacity", 50);
            serializer.DataField(ref _fuel, "Fuel", FuelCapacity);
        }

        public void OnUpdate(float frameTime)
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

        public bool TryUse(float value)
        {
            if (!Activated || !CanUse(value))
            {
                return false;
            }

            Fuel -= value;
            return true;
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

        public bool UseEntity(UseEntityEventArgs eventArgs)
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

        void IExamine.Examine(FormattedMessage message)
        {
            if (Activated)
            {
                message.PushColor(Color.Orange);
                message.AddText("Lit\n");
                message.Pop();
            }
            else
            {
                message.AddText("Not lit\n");
            }
            message.AddText("Fuel: ");
            message.PushColor(Fuel < FuelCapacity / 4f ? Color.DarkOrange : Color.Orange);
            message.AddText($"{Math.Round(Fuel)}/{FuelCapacity}");
            message.Pop();
        }
    }
}
