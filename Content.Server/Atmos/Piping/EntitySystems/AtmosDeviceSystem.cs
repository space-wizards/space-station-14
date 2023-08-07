using Content.Server.Atmos.EntitySystems;
using Content.Server.Atmos.Piping.Components;
using JetBrains.Annotations;
using Robust.Shared.Timing;

namespace Content.Server.Atmos.Piping.EntitySystems
{
    [UsedImplicitly]
    public sealed class AtmosDeviceSystem : EntitySystem
    {
        [Dependency] private readonly IGameTiming _gameTiming = default!;
        [Dependency] private readonly AtmosphereSystem _atmosphereSystem = default!;

        private float _timer = 0f;

        // Set of atmos devices that are off-grid but have JoinSystem set.
        private readonly HashSet<AtmosDeviceComponent> _joinedDevices = new();

        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<AtmosDeviceComponent, ComponentInit>(OnDeviceInitialize);
            SubscribeLocalEvent<AtmosDeviceComponent, ComponentShutdown>(OnDeviceShutdown);
            // Re-anchoring should be handled by the parent change.
            SubscribeLocalEvent<AtmosDeviceComponent, EntParentChangedMessage>(OnDeviceParentChanged);
            SubscribeLocalEvent<AtmosDeviceComponent, AnchorStateChangedEvent>(OnDeviceAnchorChanged);
        }

        public void JoinAtmosphere(AtmosDeviceComponent component)
        {
            var transform = Transform(component.Owner);

            if (component.RequireAnchored && !transform.Anchored)
                return;

            // Attempt to add device to a grid atmosphere.
            bool onGrid = (transform.GridUid != null) && _atmosphereSystem.AddAtmosDevice(transform.GridUid!.Value, component);

            if (!onGrid && component.JoinSystem)
            {
                _joinedDevices.Add(component);
                component.JoinedSystem = true;
            }

            component.LastProcess = _gameTiming.CurTime;
            RaiseLocalEvent(component.Owner, new AtmosDeviceEnabledEvent(), false);
        }

        public void LeaveAtmosphere(AtmosDeviceComponent component)
        {
            // Try to remove the component from an atmosphere, and if not
            if (component.JoinedGrid != null && !_atmosphereSystem.RemoveAtmosDevice(component.JoinedGrid.Value, component))
            {
                // The grid might have been removed but not us... This usually shouldn't happen.
                component.JoinedGrid = null;
                return;
            }

            if (component.JoinedSystem)
            {
                _joinedDevices.Remove(component);
                component.JoinedSystem = false;
            }

            component.LastProcess = TimeSpan.Zero;
            RaiseLocalEvent(component.Owner, new AtmosDeviceDisabledEvent(), false);
        }

        public void RejoinAtmosphere(AtmosDeviceComponent component)
        {
            LeaveAtmosphere(component);
            JoinAtmosphere(component);
        }

        private void OnDeviceInitialize(EntityUid uid, AtmosDeviceComponent component, ComponentInit args)
        {
            JoinAtmosphere(component);
        }

        private void OnDeviceShutdown(EntityUid uid, AtmosDeviceComponent component, ComponentShutdown args)
        {
            LeaveAtmosphere(component);
        }

        private void OnDeviceAnchorChanged(EntityUid uid, AtmosDeviceComponent component, ref AnchorStateChangedEvent args)
        {
            // Do nothing if the component doesn't require being anchored to function.
            if (!component.RequireAnchored)
                return;

            if (args.Anchored)
                JoinAtmosphere(component);
            else
                LeaveAtmosphere(component);
        }

        private void OnDeviceParentChanged(EntityUid uid, AtmosDeviceComponent component, ref EntParentChangedMessage args)
        {
            RejoinAtmosphere(component);
        }

        /// <summary>
        /// Update atmos devices that are off-grid but have JoinSystem set. For devices updates when
        /// a device is on a grid, see AtmosphereSystem:UpdateProcessing().
        /// </summary>
        public override void Update(float frameTime)
        {
            _timer += frameTime;

            if (_timer < _atmosphereSystem.AtmosTime)
                return;

            _timer -= _atmosphereSystem.AtmosTime;

            var time = _gameTiming.CurTime;
            foreach (var device in _joinedDevices)
            {
                RaiseLocalEvent(device.Owner, new AtmosDeviceUpdateEvent(_atmosphereSystem.AtmosTime), false);
                device.LastProcess = time;
            }
        }
    }
}
