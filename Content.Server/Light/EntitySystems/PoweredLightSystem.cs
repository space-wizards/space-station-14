using Content.Server.DeviceLinking.Systems;
using Content.Server.DeviceNetwork;
using Content.Server.DeviceNetwork.Systems;
using Content.Server.Emp;
using Content.Server.Ghost;
using Content.Shared.DeviceLinking.Events;
using Content.Shared.Light.Components;
using Robust.Shared.Containers;
using Robust.Shared.Timing;
using Content.Shared.Light.EntitySystems;

namespace Content.Server.Light.EntitySystems
{
    /// <summary>
    ///     System for the PoweredLightComponents
    /// </summary>
    public sealed class PoweredLightSystem : SharedPoweredLightSystem
    {
        [Dependency] private readonly DeviceLinkSystem _signalSystem = default!;

        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<PoweredLightComponent, ComponentInit>(OnInit);
            SubscribeLocalEvent<PoweredLightComponent, MapInitEvent>(OnMapInit);

            SubscribeLocalEvent<PoweredLightComponent, GhostBooEvent>(OnGhostBoo);
            SubscribeLocalEvent<PoweredLightComponent, SignalReceivedEvent>(OnSignalReceived);
            SubscribeLocalEvent<PoweredLightComponent, DeviceNetworkPacketEvent>(OnPacketReceived);

            SubscribeLocalEvent<PoweredLightComponent, EmpPulseEvent>(OnEmpPulse);
        }

        private void OnGhostBoo(EntityUid uid, PoweredLightComponent light, GhostBooEvent args)
        {
            if (light.IgnoreGhostsBoo)
                return;

            // check cooldown first to prevent abuse
            var time = GameTiming.CurTime;
            if (light.LastGhostBlink != null)
            {
                if (time <= light.LastGhostBlink + light.GhostBlinkingCooldown)
                    return;
            }

            light.LastGhostBlink = time;

            ToggleBlinkingLight(uid, light, true);
            uid.SpawnTimer(light.GhostBlinkingTime, () =>
            {
                ToggleBlinkingLight(uid, light, false);
            });

            args.Handled = true;
        }

        private void OnInit(EntityUid uid, PoweredLightComponent light, ComponentInit args)
        {
            light.LightBulbContainer = _containerSystem.EnsureContainer<ContainerSlot>(uid, LightBulbContainer);
            _signalSystem.EnsureSinkPorts(uid, light.OnPort, light.OffPort, light.TogglePort);
        }

        private void OnMapInit(EntityUid uid, PoweredLightComponent light, MapInitEvent args)
        {
            // TODO: Use ContainerFill dog
            if (light.HasLampOnSpawn != null)
            {
                var entity = EntityManager.SpawnEntity(light.HasLampOnSpawn, EntityManager.GetComponent<TransformComponent>(uid).Coordinates);
                _containerSystem.Insert(entity, light.LightBulbContainer);
            }
            // need this to update visualizers
            UpdateLight(uid, light);
        }

        private void OnSignalReceived(EntityUid uid, PoweredLightComponent component, ref SignalReceivedEvent args)
        {
            if (args.Port == component.OffPort)
                SetState(uid, false, component);
            else if (args.Port == component.OnPort)
                SetState(uid, true, component);
            else if (args.Port == component.TogglePort)
                ToggleLight(uid, component);
        }

        /// <summary>
        /// Turns the light on or of when receiving a <see cref="DeviceNetworkConstants.CmdSetState"/> command.
        /// The light is turned on or of according to the <see cref="DeviceNetworkConstants.StateEnabled"/> value
        /// </summary>
        private void OnPacketReceived(EntityUid uid, PoweredLightComponent component, DeviceNetworkPacketEvent args)
        {
            if (!args.Data.TryGetValue(DeviceNetworkConstants.Command, out string? command) || command != DeviceNetworkConstants.CmdSetState) return;
            if (!args.Data.TryGetValue(DeviceNetworkConstants.StateEnabled, out bool enabled)) return;

            SetState(uid, enabled, component);
        }

        private void OnEmpPulse(EntityUid uid, PoweredLightComponent component, ref EmpPulseEvent args)
        {
            if (TryDestroyBulb(uid, component))
                args.Affected = true;
        }
    }
}
