#nullable enable
using System;
using System.Threading.Tasks;
using Content.Server.Act;
using Content.Server.Atmos;
using Content.Server.Chat.Managers;
using Content.Server.Chemistry.Components;
using Content.Server.Explosion;
using Content.Server.Items;
using Content.Server.Notification;
using Content.Shared.Audio;
using Content.Shared.Chemistry;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.Chemistry.Solution.Components;
using Content.Shared.Examine;
using Content.Shared.Interaction;
using Content.Shared.NetIDs;
using Content.Shared.Notification;
using Content.Shared.Notification.Managers;
using Content.Shared.Sound;
using Content.Shared.Temperature;
using Content.Shared.Tool;
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

namespace Content.Server.Tools.Components
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

        [DataField("weldSounds")]
        private SoundSpecifier WeldSounds { get; set; } = default!;

        [DataField("welderOffSounds")]
        private SoundSpecifier WelderOffSounds { get; set; } = new SoundCollectionSpecifier("WelderOff");

        [DataField("welderOnSounds")]
        private SoundSpecifier WelderOnSounds { get; set; } = new SoundCollectionSpecifier("WelderOn");

        [DataField("welderRefill")]
        private SoundSpecifier WelderRefill { get; set; } = new SoundPathSpecifier("/Audio/Effects/refill.ogg");

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

        protected override void Initialize()
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
                    Owner.PopupMessage(user, Loc.GetString("welder-component-welder-not-lit-message"));

                return false;
            }

            if (!CanWeld(value))
            {
                if (!silent && user != null)
                    Owner.PopupMessage(user, Loc.GetString("welder-component-cannot-weld-message"));

                return false;
            }

            if (_solutionComponent == null)
                return false;

            var succeeded = _solutionComponent.TryRemoveReagent("WeldingFuel", ReagentUnit.New(value));

            if (succeeded && !silent && WeldSounds.TryGetSound(out var weldSounds))
            {
                PlaySound(weldSounds);
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

                if(WelderOffSounds.TryGetSound(out var welderOffSOunds))
                    PlaySound(welderOffSOunds, -5);
                _welderSystem.Unsubscribe(this);
                return true;
            }

            if (!CanLitWelder() && user != null)
            {
                Owner.PopupMessage(user, Loc.GetString("welder-component-no-fuel-message"));
                return false;
            }

            WelderLit = true;
            item.EquippedPrefix = "on";
            _spriteComponent?.LayerSetVisible(1, true);

            if (_pointLightComponent != null) _pointLightComponent.Enabled = true;

            if (WelderOnSounds.TryGetSound(out var welderOnSOunds))
                PlaySound(welderOnSOunds, -5);
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
                message.AddMarkup(Loc.GetString("welder-component-on-examine-welder-lit-message") + "\n");
            }
            else
            {
                message.AddText(Loc.GetString("welder-component-on-examine-welder-not-lit-message") + "\n");
            }

            if (inDetailsRange)
            {
                message.AddMarkup(Loc.GetString("welder-component-on-examine-detailed-message",
                                                ("colorName", Fuel < FuelCapacity / 4f ? "darkorange" : "orange"),
                                                ("fuelLeft", Math.Round(Fuel)),
                                                ("fuelCapacity", FuelCapacity)));
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
                if(WeldSounds.TryGetSound(out var weldSound))
                    PlaySound(weldSound);

                othersMessage =
                    Loc.GetString("welder-component-suicide-lit-others-message",
                                  ("victim",victim));
                victim.PopupMessageOtherClients(othersMessage);

                selfMessage = Loc.GetString("welder-component-suicide-lit-message");
                victim.PopupMessage(selfMessage);

                return SuicideKind.Heat;
            }

            othersMessage = Loc.GetString("welder-component-suicide-unlit-others-message", ("victim", victim));
            victim.PopupMessageOtherClients(othersMessage);

            selfMessage = Loc.GetString("welder-component-suicide-unlit-message");
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
                    if(WelderRefill.TryGetSound(out var welderRefillSound))
                        SoundSystem.Play(Filter.Pvs(Owner), welderRefillSound, Owner);
                    eventArgs.Target.PopupMessage(eventArgs.User, Loc.GetString("welder-component-after-interact-refueled-message"));
                }
            }

            return true;
        }

        private void PlaySound(string soundName, float volume = -5f)
        {
            SoundSystem.Play(Filter.Pvs(Owner), soundName, Owner, AudioHelpers.WithVariation(0.15f).WithVolume(volume));
        }
    }
}
