using System;
using Content.Server.Atmos.EntitySystems;
using Content.Server.Atmos.Piping.Components;
using JetBrains.Annotations;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Physics;
using Robust.Shared.Timing;

namespace Content.Server.Atmos.Piping.EntitySystems
{
    [UsedImplicitly]
    public class AtmosDeviceSystem : EntitySystem
    {
        [Dependency] private IGameTiming _gameTiming = default!;

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
            return !component.RequireAnchored || !component.Owner.Transform.Anchored;
        }

        public void JoinAtmosphere(AtmosDeviceComponent component)
        {
            if (!CanJoinAtmosphere(component))
                return;

            // We try to add the device to a valid atmosphere.
            if (!Get<AtmosphereSystem>().AddAtmosDevice(component))
                return;

            component.LastProcess = _gameTiming.CurTime;

            RaiseLocalEvent(component.Owner.Uid, new AtmosDeviceEnabledEvent(), false);
        }

        public void LeaveAtmosphere(AtmosDeviceComponent component)
        {
            if (!Get<AtmosphereSystem>().RemoveAtmosDevice(component))
                return;

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

        private void OnDeviceAnchorChanged(EntityUid uid, AtmosDeviceComponent component, AnchorStateChangedEvent args)
        {
            // Do nothing if the component doesn't require being anchored to function.
            if (!component.RequireAnchored)
                return;

            if(component.Owner.Transform.Anchored)
                JoinAtmosphere(component);
            else
                LeaveAtmosphere(component);
        }

        private void OnDeviceParentChanged(EntityUid uid, AtmosDeviceComponent component, EntParentChangedMessage args)
        {
            RejoinAtmosphere(component);
        }
    }
}
