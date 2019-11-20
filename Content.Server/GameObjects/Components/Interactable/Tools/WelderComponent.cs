using System;
using Content.Server.GameObjects.Components.Chemistry;
using Content.Server.GameObjects.EntitySystems;
using Robust.Server.GameObjects;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Localization;
using Robust.Shared.Serialization;
using Robust.Shared.Utility;
using Robust.Shared.ViewVariables;

namespace Content.Server.GameObjects.Components.Interactable.Tools
{
    /// <summary>
    /// Tool used to weld metal together, light things on fire, or melt into constituent parts
    /// </summary>
    [RegisterComponent]
    class WelderComponent : ToolComponent, IUse, IExamine
    {
        SpriteComponent spriteComponent;

        public override string Name => "Welder";
        private string WelderFuelReagentName;
        private SolutionComponent solutionComponent;
        /// <summary>
        /// Maximum fuel capacity the welder can hold
        /// </summary>
        [ViewVariables(VVAccess.ReadWrite)]
        public int FuelCapacity
        {
            get => _fuelCapacity;
            set => _fuelCapacity = value;
        }
        private int _fuelCapacity = 50;

        /// <summary>
        /// Fuel the welder has to do tasks
        /// </summary>
        [ViewVariables(VVAccess.ReadWrite)]
        public int CurrentWelderFuel
        {
            get => solutionComponent.GetReagentQuantity(WelderFuelReagentName);
            set => _fuel = value;
        }
        private int _fuel = 0;

        /// <summary>
        /// Default Cost of using the welder fuel for an action
        /// </summary>
        public const int DefaultFuelCost = 5;

        /// <summary>
        /// Rate at which we expunge fuel from ourselves when activated
        /// </summary>
        public int FuelLossRate = 1;

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
            solutionComponent = Owner.GetComponent<SolutionComponent>();
            WelderFuelReagentName = solutionComponent.ReagentList[0].ReagentId;
            if(solutionComponent.ContainsReagent(WelderFuelReagentName, out var fuel))
            {
                CurrentWelderFuel = fuel;
            }
        }

        public override void ExposeData(ObjectSerializer serializer)
        {
            base.ExposeData(serializer);
        }

        public void OnUpdate(float frameTime)
        {
            if (!Activated)
            {
                return;
            }
            solutionComponent.TryRemoveReagent(WelderFuelReagentName, FuelLossRate);



            if (CurrentWelderFuel == 0)
            {
                ToggleStatus();
            }
        }

        public bool TryUse(int value)
        {
            if (!Activated || !CanUse(value))
            {
                return false;
            }

            CurrentWelderFuel -= value;
            return true;
        }

        public bool CanUse(int value)
        {
            return CurrentWelderFuel >= value;
        }

        public override bool CanUse()
        {
            return CanUse(DefaultFuelCost);
        }

        public bool CanActivate()
        {
            return CurrentWelderFuel > 0;
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
            var loc = IoCManager.Resolve<ILocalizationManager>();
            if (Activated)
            {
                message.AddMarkup(loc.GetString("[color=orange]Lit[/color]\n"));
            }
            else
            {
                message.AddText(loc.GetString("Not lit\n"));
            }

        }
    }
}
