#nullable enable
using System;
using System.Threading.Tasks;
using Content.Server.Atmos;
using Content.Server.Explosions;
using Content.Server.GameObjects.Components.Chemistry;
using Content.Server.GameObjects.Components.Items.Storage;
using Content.Server.GameObjects.EntitySystems;
using Content.Server.Interfaces.Chat;
using Content.Server.Interfaces.GameObjects;
using Content.Server.Utility;
using Content.Shared.Chemistry;
using Content.Shared.GameObjects;
using Content.Shared.GameObjects.Components.Chemistry;
using Content.Shared.GameObjects.Components.Interactable;
using Content.Shared.GameObjects.EntitySystems;
using Content.Shared.Interfaces;
using Content.Shared.Interfaces.GameObjects.Components;
using Robust.Server.GameObjects;
using Robust.Shared.Audio;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Localization;
using Robust.Shared.Player;
using Robust.Shared.Players;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.Utility;
using Robust.Shared.ViewVariables;

namespace Content.Server.GameObjects.Components.Interactable
{
    [RegisterComponent]
    [ComponentReference(typeof(ToolComponent))]
    [ComponentReference(typeof(IToolComponent))]
    [ComponentReference(typeof(IHotItem))]
    public class WelderComponent : ToolComponent, IExamine, IUse, ISuicideAct, ISolutionChange, IHotItem, IAfterInteract
    {
        [Dependency] private readonly IEntitySystemManager _entitySystemManager = default!;

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

        private bool _welderLit;
        private WelderSystem _welderSystem = default!;
        private SpriteComponent? _spriteComponent;
        private SolutionContainerComponent? _solutionComponent;
        private PointLightComponent? _pointLightComponent;

        [DataField("weldSoundCollection")]
        public string? WeldSoundCollection { get; set; }

        [ViewVariables]
        public float Fuel => _solutionComponent?.Solution?.GetReagentQuantity("WeldingFuel").Float() ?? 0f;

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

        bool IHotItem.IsCurrentlyHot()
        {
            return WelderLit;
        }

        public override void Initialize()
        {
            base.Initialize();

            AddQuality(ToolQuality.Welding);

            _welderSystem = _entitySystemManager.GetEntitySystem<WelderSystem>();

            Owner.TryGetComponent(out _solutionComponent);
            Owner.TryGetComponent(out _spriteComponent);
            Owner.TryGetComponent(out _pointLightComponent);
        }

        public override ComponentState GetComponentState(ICommonSession player)
        {
            return new WelderComponentState(FuelCapacity, Fuel, WelderLit);
        }

        public override async Task<bool> UseTool(IEntity user, IEntity? target, float doAfterDelay, ToolQuality toolQualityNeeded, Func<bool>? doAfterCheck = null)
        {
            bool ExtraCheck()
            {
                var extraCheck = doAfterCheck?.Invoke() ?? true;

                if (!CanWeld(DefaultFuelCost))
                {
                    target?.PopupMessage(user, "Can't weld!");

                    return false;
                }

                return extraCheck;
            }

            var canUse = await base.UseTool(user, target, doAfterDelay, toolQualityNeeded, ExtraCheck);

            return toolQualityNeeded.HasFlag(ToolQuality.Welding) ? canUse && TryWeld(DefaultFuelCost, user) : canUse;
        }

        public async Task<bool> UseTool(IEntity user, IEntity target, float doAfterDelay, ToolQuality toolQualityNeeded, float fuelConsumed, Func<bool>? doAfterCheck = null)
        {
            bool ExtraCheck()
            {
                var extraCheck = doAfterCheck?.Invoke() ?? true;

                return extraCheck && CanWeld(fuelConsumed);
            }

            return await base.UseTool(user, target, doAfterDelay, toolQualityNeeded, ExtraCheck) && TryWeld(fuelConsumed, user);
        }

        private bool TryWeld(float value, IEntity? user = null, bool silent = false)
        {
            if (!WelderLit)
            {
                if (!silent && user != null)
                    Owner.PopupMessage(user, Loc.GetString("The welder is turned off!"));

                return false;
            }

            if (!CanWeld(value))
            {
                if (!silent && user != null)
                    Owner.PopupMessage(user, Loc.GetString("The welder does not have enough fuel for that!"));

                return false;
            }

            if (_solutionComponent == null)
                return false;

            var succeeded = _solutionComponent.TryRemoveReagent("WeldingFuel", ReagentUnit.New(value));

            if (succeeded && !silent)
            {
                PlaySoundCollection(WeldSoundCollection);
            }
            return succeeded;
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
        private bool ToggleWelderStatus(IEntity? user = null)
        {
            var item = Owner.GetComponent<ItemComponent>();

            if (WelderLit)
            {
                WelderLit = false;
                // Layer 1 is the flame.
                item.EquippedPrefix = "off";
                _spriteComponent?.LayerSetVisible(1, false);

                if (_pointLightComponent != null) _pointLightComponent.Enabled = false;

                PlaySoundCollection("WelderOff", -5);
                _welderSystem.Unsubscribe(this);
                return true;
            }

            if (!CanLitWelder() && user != null)
            {
                Owner.PopupMessage(user, Loc.GetString("The welder has no fuel left!"));
                return false;
            }

            WelderLit = true;
            item.EquippedPrefix = "on";
            _spriteComponent?.LayerSetVisible(1, true);

            if (_pointLightComponent != null) _pointLightComponent.Enabled = true;

            PlaySoundCollection("WelderOn", -5);
            _welderSystem.Subscribe(this);

            Owner.Transform.Coordinates
                .GetTileAtmosphere()?.HotspotExpose(700f, 50f, true);

            return true;
        }

        bool IUse.UseEntity(UseEntityEventArgs eventArgs)
        {
            return ToggleWelderStatus(eventArgs.User);
        }

        public void Examine(FormattedMessage message, bool inDetailsRange)
        {
            if (WelderLit)
            {
                message.AddMarkup(Loc.GetString("[color=orange]Lit[/color]\n"));
            }
            else
            {
                message.AddText(Loc.GetString("Not lit\n"));
            }

            if (inDetailsRange)
            {
                message.AddMarkup(Loc.GetString("Fuel: [color={0}]{1}/{2}[/color].",
                    Fuel < FuelCapacity / 4f ? "darkorange" : "orange", Math.Round(Fuel), FuelCapacity));
            }
        }

        protected override void Shutdown()
        {
            base.Shutdown();
            _welderSystem.Unsubscribe(this);
        }

        public void OnUpdate(float frameTime)
        {
            if (!HasQuality(ToolQuality.Welding) || !WelderLit || Owner.Deleted)
                return;

            _solutionComponent?.TryRemoveReagent("WeldingFuel", ReagentUnit.New(FuelLossRate * frameTime));

            Owner.Transform.Coordinates
                .GetTileAtmosphere()?.HotspotExpose(700f, 50f, true);

            if (Fuel == 0)
                ToggleWelderStatus();

        }

        SuicideKind ISuicideAct.Suicide(IEntity victim, IChatManager chat)
        {
            string othersMessage;
            string selfMessage;

            if (TryWeld(5, victim, silent: true))
            {
                PlaySoundCollection(WeldSoundCollection);

                othersMessage =
                    Loc.GetString(
                        "{0:theName} welds {0:their} every orifice closed! It looks like {0:theyre} trying to commit suicide!",
                        victim);
                victim.PopupMessageOtherClients(othersMessage);

                selfMessage = Loc.GetString("You weld your every orifice closed!");
                victim.PopupMessage(selfMessage);

                return SuicideKind.Heat;
            }

            othersMessage = Loc.GetString("{0:theName} bashes themselves with the unlit welding torch!", victim);
            victim.PopupMessageOtherClients(othersMessage);

            selfMessage = Loc.GetString("You bash yourself with the unlit welding torch!");
            victim.PopupMessage(selfMessage);

            return SuicideKind.Blunt;
        }

        public void SolutionChanged(SolutionChangeEventArgs eventArgs)
        {
            Dirty();
        }


        async Task<bool> IAfterInteract.AfterInteract(AfterInteractEventArgs eventArgs)
        {
            if (eventArgs.Target == null || !eventArgs.CanReach)
            {
                return false;
            }

            if (eventArgs.Target.TryGetComponent(out ReagentTankComponent? tank)
                && tank.TankType == ReagentTankType.Fuel
                && eventArgs.Target.TryGetComponent(out ISolutionInteractionsComponent? targetSolution)
                && targetSolution.CanDrain
                && _solutionComponent != null)
            {
                if (WelderLit)
                {
                    // Oh no no
                    eventArgs.Target.SpawnExplosion();
                    return true;
                }

                var trans = ReagentUnit.Min(_solutionComponent.EmptyVolume, targetSolution.DrainAvailable);
                if (trans > 0)
                {
                    var drained = targetSolution.Drain(trans);
                    _solutionComponent.TryAddSolution(drained);

                    SoundSystem.Play(Filter.Pvs(Owner), "/Audio/Effects/refill.ogg", Owner);
                    eventArgs.Target.PopupMessage(eventArgs.User, Loc.GetString("Welder refueled"));
                }
            }

            return true;
        }
    }
}
