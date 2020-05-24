using System;
using Content.Server.GameObjects.Components.Chemistry;
using Content.Server.GameObjects.EntitySystems;
using Content.Server.Interfaces;
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
        [Dependency] private IServerNotifyManager _notifyManager;
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

            AddQuality(ToolQuality.Welding);

            _welderSystem = _entitySystemManager.GetEntitySystem<WelderSystem>();

            Owner.TryGetComponent(out _solutionComponent);
            Owner.TryGetComponent(out _spriteComponent);
        }

        public override ComponentState GetComponentState()
        {
            return new WelderComponentState(FuelCapacity, Fuel, WelderLit);
        }

        public override bool UseTool(IEntity user, IEntity target, ToolQuality toolQualityNeeded)
        {
            var canUse = base.UseTool(user, target, toolQualityNeeded);

            return toolQualityNeeded.HasFlag(ToolQuality.Welding) ? canUse && TryWeld(DefaultFuelCost, user) : canUse;
        }

        public bool UseTool(IEntity user, IEntity target, ToolQuality toolQualityNeeded, float fuelConsumed)
        {
            return base.UseTool(user, target, toolQualityNeeded) && TryWeld(fuelConsumed, user);
        }

        private bool TryWeld(float value, IEntity user = null)
        {
            if (!WelderLit)
            {
                _notifyManager.PopupMessage(Owner, user, Loc.GetString("The welder is turned off!"));
                return false;
            }

            if (!CanWeld(value))
            {
                _notifyManager.PopupMessage(Owner, user, Loc.GetString("The welder does not have enough fuel for that!"));
            }

            if (_solutionComponent == null)
                return false;

            return _solutionComponent.TryRemoveReagent("chem.WeldingFuel", ReagentUnit.New(value));
        }

        private bool CanWeld(float value)
        {
            return Fuel > value || Qualities != ToolQuality.Welding;
        }

        private bool CanLitWelder()
        {
            return Fuel > 0 || Qualities != ToolQuality.Welding;
        }

        /// <summary>
        /// Deactivates welding tool if active, activates welding tool if possible
        /// </summary>
        private bool ToggleWelderStatus(IEntity user = null)
        {
            var item = Owner.GetComponent<ItemComponent>();

            if (WelderLit)
            {
                WelderLit = false;
                // Layer 1 is the flame.
                item.EquippedPrefix = "off";
                _spriteComponent.LayerSetVisible(1, false);
                PlaySoundCollection("WelderOff", -5);
                _welderSystem.Unsubscribe(this);
                return true;
            }

            if (!CanLitWelder())
            {
                _notifyManager.PopupMessage(Owner, user, Loc.GetString("The welder has no fuel left!"));
                return false;
            }

            WelderLit = true;
            item.EquippedPrefix = "on";
            _spriteComponent.LayerSetVisible(1, true);
            PlaySoundCollection("WelderOn", -5);
            _welderSystem.Subscribe(this);
            return true;
        }

        public bool UseEntity(UseEntityEventArgs eventArgs)
        {
            return ToggleWelderStatus(eventArgs.User);
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
            if (!HasQuality(ToolQuality.Welding) || !WelderLit)
                return;

            _solutionComponent.TryRemoveReagent("chem.WeldingFuel", ReagentUnit.New(FuelLossRate * frameTime));

            if (Fuel == 0)
                ToggleWelderStatus();

            Dirty();
        }
    }
}
