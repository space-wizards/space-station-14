using System;
using Content.Server.DeviceNetwork;
using Content.Server.DeviceNetwork.Systems;
using Content.Server.Ghost;
using Content.Server.Light.Components;
using Content.Server.MachineLinking.Events;
using Content.Server.Power.Components;
using Content.Server.Power.EntitySystems;
using Content.Shared.Light;
using Content.Shared.Damage;
using Robust.Server.GameObjects;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Timing;

namespace Content.Server.Light.EntitySystems
{
    /// <summary>
    ///     System for the PoweredLightComponent. Currently bare-bones, to handle events from the DamageableSystem
    /// </summary>
    public class PoweredLightSystem : EntitySystem
    {
        [Dependency] private readonly IGameTiming _gameTiming = default!;

        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<PoweredLightComponent, GhostBooEvent>(OnGhostBoo);
            SubscribeLocalEvent<PoweredLightComponent, DamageChangedEvent>(HandleLightDamaged);

            SubscribeLocalEvent<PoweredLightComponent, SignalReceivedEvent>(OnSignalReceived);
            SubscribeLocalEvent<PoweredLightComponent, PacketSentEvent>(OnPacketReceived);

            SubscribeLocalEvent<LitOnPoweredComponent, PowerChangedEvent>(OnPowerChanged);
            SubscribeLocalEvent<LitOnPoweredComponent, PowerNetBatterySupplyEvent>(OnPowerSupply);
        }

        /// <summary>
        ///     Destroy the light bulb if the light took any damage.
        /// </summary>
        public void HandleLightDamaged(EntityUid uid, PoweredLightComponent component, DamageChangedEvent args)
        {
            // Was it being repaired, or did it take damage?
            if (args.DamageIncreased)
            {
                // Eventually, this logic should all be done by this (or some other) system, not a component.
                component.TryDestroyBulb();
            }
        }

        private void OnGhostBoo(EntityUid uid, PoweredLightComponent light, GhostBooEvent args)
        {
            if (light.IgnoreGhostsBoo)
                return;

            // check cooldown first to prevent abuse
            var time = _gameTiming.CurTime;
            if (light.LastGhostBlink != null)
            {
                if (time <= light.LastGhostBlink + light.GhostBlinkingCooldown)
                    return;
            }

            light.LastGhostBlink = time;

            ToggleBlinkingLight(light, true);
            light.Owner.SpawnTimer(light.GhostBlinkingTime, () =>
            {
                ToggleBlinkingLight(light, false);
            });

            args.Handled = true;
        }

        public void ToggleBlinkingLight(PoweredLightComponent light, bool isNowBlinking)
        {
            if (light.IsBlinking == isNowBlinking)
                return;

            light.IsBlinking = isNowBlinking;

            if (!light.Owner.TryGetComponent(out AppearanceComponent? appearance))
                return;
            appearance.SetData(PoweredLightVisuals.Blinking, isNowBlinking);
        }

        private void OnSignalReceived(EntityUid uid, PoweredLightComponent component, SignalReceivedEvent args)
        {
            switch (args.Port)
            {
                case "state":
                    component.ToggleLight();
                    break;
            }
        }

        /// <summary>
        /// Turns the light on or of when receiving a <see cref="DeviceNetworkConstants.CmdSetState"/> command.
        /// The light is turned on or of according to the <see cref="DeviceNetworkConstants.StateEnabled"/> value
         /// </summary>
        private void OnPacketReceived(EntityUid uid, PoweredLightComponent component, PacketSentEvent args)
        {
            if (!args.Data.TryGetValue(DeviceNetworkConstants.Command, out string? command) || command != DeviceNetworkConstants.CmdSetState) return;
            if (!args.Data.TryGetValue(DeviceNetworkConstants.StateEnabled, out bool enabled)) return;

            component.SetState(enabled);
        }

        private void OnPowerChanged(EntityUid uid, LitOnPoweredComponent component, PowerChangedEvent args)
        {
            if (EntityManager.TryGetComponent<PointLightComponent>(uid, out var light))
            {
                light.Enabled = args.Powered;
            }
        }

        private void OnPowerSupply(EntityUid uid, LitOnPoweredComponent component, PowerNetBatterySupplyEvent args)
        {
            if (EntityManager.TryGetComponent<PointLightComponent>(uid, out var light))
            {
                light.Enabled = args.Supply;
            }
        }
    }
}
