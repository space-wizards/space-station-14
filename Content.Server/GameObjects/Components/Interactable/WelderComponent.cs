using System;
using Content.Server.GameObjects.Components.Chemistry;
using Content.Server.GameObjects.EntitySystems;
using Content.Shared.Chemistry;
using Content.Shared.GameObjects;
using Content.Shared.GameObjects.Components.Interactable;
using Robust.Server.GameObjects;
using Robust.Shared.GameObjects;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Interfaces.Random;
using Robust.Shared.IoC;
using Robust.Shared.Localization;
using Robust.Shared.Utility;
using Robust.Shared.ViewVariables;

namespace Content.Server.GameObjects.Components.Interactable
{
    [RegisterComponent]
    [ComponentReference(typeof(ToolComponent))]
    public class WelderComponent : ToolComponent, IExamine, IUse
    {
#pragma warning disable 649
        [Dependency] private IEntitySystemManager _entitySystemManager;
        [Dependency] private readonly IRobustRandom _robustRandom;
#pragma warning restore 649

        public override string Name => "Welder";
        public override uint? NetID => ContentNetIDs.WELDER;

        /// <summary>
        /// Default Cost of using the welder fuel for an action
        /// </summary>
        public const float DefaultFuelCost = 10;

        /// <summary>
        /// Rate at which we expunge fuel from ourselves when activated
        /// </summary>
        public const float FuelLossRate = 0.5f;

        private bool _welderLit = false;

        private WelderSystem _welderSystem;
        private SpriteComponent _spriteComponent;
        private SolutionComponent _solutionComponent;

        [ViewVariables]
        public float Fuel => _solutionComponent?.Solution.GetReagentQuantity("chem.WeldingFuel").Float() ?? 0f;

        [ViewVariables]
        public float FuelCapacity => _solutionComponent?.MaxVolume.Float() ?? 0f;

        /// <summary>
        /// Status of welder, whether it is ignited
        /// </summary>
        [ViewVariables]
        public bool WelderLit
        {
            get => _welderLit;
            private set
            {
                _welderLit = value;
                Dirty();
            }
        }

        public override void Initialize()
        {
            base.Initialize();

            _behavior = Tool.Welder;

            _welderSystem = _entitySystemManager.GetEntitySystem<WelderSystem>();

            Owner.TryGetComponent(out _solutionComponent);
            Owner.TryGetComponent(out _spriteComponent);
        }

        public override ComponentState GetComponentState()
        {
            return new WelderComponentState(FuelCapacity, Fuel, WelderLit);
        }

        public bool CanUse()
        {
            return CanWeld(DefaultFuelCost);
        }

        public bool TryUse()
        {
            return TryWeld(DefaultFuelCost);
        }

        public bool TryWeld(float value)
        {
            if (!WelderLit || !CanWeld(value) || _solutionComponent == null)
            {
                return false;
            }

            return _solutionComponent.TryRemoveReagent("chem.WeldingFuel", ReagentUnit.New(value));
        }

        public bool CanWeld(float value)
        {
            return Fuel > value || Behavior != Tool.Welder;
        }

        public bool CanLitWelder()
        {
            return Fuel > 0 || Behavior != Tool.Welder;
        }

        /// <summary>
        /// Deactivates welding tool if active, activates welding tool if possible
        /// </summary>
        /// <returns></returns>
        public bool ToggleWelderStatus()
        {
            if (WelderLit)
            {
                WelderLit = false;
                // Layer 1 is the flame.
                _spriteComponent.LayerSetVisible(1, false);
                PlaySoundCollection("WelderOff", -5);
                _welderSystem.Unsubscribe(this);
                return true;
            }

            if (!CanLitWelder()) return false;

            WelderLit = true;
            _spriteComponent.LayerSetVisible(1, true);
            PlaySoundCollection("WelderOn", -5);
            _welderSystem.Subscribe(this);
            return true;
        }

        public bool UseEntity(UseEntityEventArgs eventArgs)
        {
            return ToggleWelderStatus();
        }

        public void Examine(FormattedMessage message)
        {
            if (WelderLit)
            {
                message.AddMarkup(Loc.GetString("[color=orange]Lit[/color]\n"));
            }
            else
            {
                message.AddText(Loc.GetString("Not lit\n"));
            }

            message.AddMarkup(Loc.GetString("Fuel: [color={0}]{1}/{2}[/color].",
                Fuel < FuelCapacity / 4f ? "darkorange" : "orange", Math.Round(Fuel), FuelCapacity));
        }

        public void OnUpdate(float frameTime)
        {
            if (Behavior != Tool.Welder || !WelderLit)
            {
                return;
            }

            _solutionComponent.TryRemoveReagent("chem.WeldingFuel", ReagentUnit.New(FuelLossRate * frameTime));

            if (Fuel == 0)
            {
                ToggleWelderStatus();
            }

            Dirty();
        }
    }
}
