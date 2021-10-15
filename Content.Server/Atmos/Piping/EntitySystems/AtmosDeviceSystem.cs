using System;
using System.Collections.Generic;
using Content.Server.Atmos.EntitySystems;
using Content.Server.Atmos.Piping.Components;
using JetBrains.Annotations;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Timing;

namespace Content.Server.Atmos.Piping.EntitySystems
{
    [UsedImplicitly]
    public class AtmosDeviceSystem : EntitySystem
    {
        [Dependency] private readonly IGameTiming _gameTiming = default!;
        [Dependency] private readonly AtmosphereSystem _atmosphereSystem = default!;

        private readonly AtmosDeviceUpdateEvent _updateEvent = new();

        private float _timer = 0f;
        private readonly HashSet<AtmosDeviceComponent> _joinedDevices = new();

        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<AtmosDeviceComponent, ComponentInit>(OnDeviceInitialize);
            SubscribeLocalEvent<AtmosDeviceComponent, ComponentShutdown>(OnDeviceShutdown);
            SubscribeLocalEvent<AtmosDeviceComponent, EntParentChangedMessage>(OnDeviceParentChanged);
            SubscribeLocalEvent<AtmosDeviceComponent, AnchorStateChangedEvent>(OnDeviceAnchorChanged);
        }

        private bool CanJoinAtmosphere(AtmosDeviceComponent component)
        {
            return !component.RequireAnchored || component.Owner.Transform.Anchored;
        }

        public void JoinAtmosphere(AtmosDeviceComponent component)
        {
            if (!CanJoinAtmosphere(component))
            {
                return;
            }

            // We try to add the device to a valid atmosphere, and if we can't, try to add it to the entity system.
            if (!_atmosphereSystem.AddAtmosDevice(component))
            {
                if (component.JoinSystem)
                {
                    _joinedDevices.Add(component);
                    component.JoinedSystem = true;
                }
                else
                {
                    return;
                }
            }


            component.LastProcess = _gameTiming.CurTime;

            RaiseLocalEvent(component.Owner.Uid, new AtmosDeviceEnabledEvent(), false);
        }

        public void LeaveAtmosphere(AtmosDeviceComponent component)
        {
            // Try to remove the component from an atmosphere, and if not
            if (component.JoinedGrid != null && !_atmosphereSystem.RemoveAtmosDevice(component))
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
            RaiseLocalEvent(component.Owner.Uid, new AtmosDeviceDisabledEvent(), false);
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

            if(component.Owner.Transform.Anchored)
                JoinAtmosphere(component);
            else
                LeaveAtmosphere(component);
        }

        private void OnDeviceParentChanged(EntityUid uid, AtmosDeviceComponent component, ref EntParentChangedMessage args)
        {
            RejoinAtmosphere(component);
        }

        public override void Update(float frameTime)
        {
            _timer += frameTime;

            if (_timer < _atmosphereSystem.AtmosTime)
                return;

            _timer -= _atmosphereSystem.AtmosTime;

            var time = _gameTiming.CurTime;
            foreach (var device in _joinedDevices)
            {
                RaiseLocalEvent(device.Owner.Uid, _updateEvent, false);
                device.LastProcess = time;
            }
        }
    }
}
