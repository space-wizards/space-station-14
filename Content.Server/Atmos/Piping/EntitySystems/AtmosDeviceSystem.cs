using Content.Server.Atmos.Components;
using Content.Server.Atmos.EntitySystems;
using Content.Server.Atmos.Piping.Components;
using JetBrains.Annotations;
using Robust.Shared.Timing;
using Robust.Shared.Utility;

namespace Content.Server.Atmos.Piping.EntitySystems
{
    [UsedImplicitly]
    public sealed class AtmosDeviceSystem : EntitySystem
    {
        [Dependency] private readonly IGameTiming _gameTiming = default!;
        [Dependency] private readonly AtmosphereSystem _atmosphereSystem = default!;

        private float _timer;

        // Set of atmos devices that are off-grid but have JoinSystem set.
        private readonly HashSet<Entity<AtmosDeviceComponent>> _joinedDevices = new();

        private static AtmosDeviceDisabledEvent _disabledEv = new();
        private static AtmosDeviceEnabledEvent _enabledEv = new();

        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<AtmosDeviceComponent, ComponentInit>(OnDeviceInitialize);
            SubscribeLocalEvent<AtmosDeviceComponent, ComponentShutdown>(OnDeviceShutdown);
            // Re-anchoring should be handled by the parent change.
            SubscribeLocalEvent<AtmosDeviceComponent, EntParentChangedMessage>(OnDeviceParentChanged);
            SubscribeLocalEvent<AtmosDeviceComponent, AnchorStateChangedEvent>(OnDeviceAnchorChanged);
        }

        public void JoinAtmosphere(Entity<AtmosDeviceComponent> ent)
        {
            if (ent.Comp.JoinedGrid != null)
            {
                DebugTools.Assert(HasComp<GridAtmosphereComponent>(ent.Comp.JoinedGrid));
                DebugTools.Assert(Transform(ent).GridUid == ent.Comp.JoinedGrid);
                DebugTools.Assert(ent.Comp.RequireAnchored == Transform(ent).Anchored);
                return;
            }

            var component = ent.Comp;
            var transform = Transform(ent);

            if (component.RequireAnchored && !transform.Anchored)
                return;

            // Attempt to add device to a grid atmosphere.
            bool onGrid = (transform.GridUid != null) && _atmosphereSystem.AddAtmosDevice(transform.GridUid!.Value, ent);

            if (!onGrid && component.JoinSystem)
            {
                _joinedDevices.Add(ent);
                component.JoinedSystem = true;
            }

            component.LastProcess = _gameTiming.CurTime;
            RaiseLocalEvent(ent, ref _enabledEv);
        }

        public void LeaveAtmosphere(Entity<AtmosDeviceComponent> ent)
        {
            var component = ent.Comp;
            // Try to remove the component from an atmosphere, and if not
            if (component.JoinedGrid != null && !_atmosphereSystem.RemoveAtmosDevice(component.JoinedGrid.Value, ent))
            {
                // The grid might have been removed but not us... This usually shouldn't happen.
                component.JoinedGrid = null;
                return;
            }

            if (component.JoinedSystem)
            {
                _joinedDevices.Remove(ent);
                component.JoinedSystem = false;
            }

            component.LastProcess = TimeSpan.Zero;
            RaiseLocalEvent(ent, ref _disabledEv);
        }

        public void RejoinAtmosphere(Entity<AtmosDeviceComponent> component)
        {
            LeaveAtmosphere(component);
            JoinAtmosphere(component);
        }

        private void OnDeviceInitialize(Entity<AtmosDeviceComponent> ent, ref ComponentInit args)
        {
            JoinAtmosphere(ent);
        }

        private void OnDeviceShutdown(Entity<AtmosDeviceComponent> ent, ref ComponentShutdown args)
        {
            LeaveAtmosphere(ent);
        }

        private void OnDeviceAnchorChanged(Entity<AtmosDeviceComponent> ent, ref AnchorStateChangedEvent args)
        {
            // Do nothing if the component doesn't require being anchored to function.
            if (!ent.Comp.RequireAnchored)
                return;

            if (args.Anchored)
                JoinAtmosphere(ent);
            else
                LeaveAtmosphere(ent);
        }

        private void OnDeviceParentChanged(Entity<AtmosDeviceComponent> ent, ref EntParentChangedMessage args)
        {
            RejoinAtmosphere(ent);
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
            var ev = new AtmosDeviceUpdateEvent(_atmosphereSystem.AtmosTime, null, null);
            foreach (var device in _joinedDevices)
            {
                DebugTools.Assert(!HasComp<GridAtmosphereComponent>(Transform(device).GridUid));
                RaiseLocalEvent(device, ref ev);
                device.Comp.LastProcess = time;
            }
        }
    }
}
