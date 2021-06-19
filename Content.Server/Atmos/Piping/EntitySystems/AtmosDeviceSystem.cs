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
            SubscribeLocalEvent<AtmosDeviceComponent, PhysicsBodyTypeChangedEvent>(OnDeviceBodyTypeChanged);
            SubscribeLocalEvent<AtmosDeviceComponent, EntParentChangedMessage>(OnDeviceParentChanged);
        }

        private bool CanJoinAtmosphere(AtmosDeviceComponent component)
        {
            return !component.RequireAnchored || !component.Owner.TryGetComponent(out PhysicsComponent? physics) || physics.BodyType == BodyType.Static;
        }

        public void JoinAtmosphere(AtmosDeviceComponent component)
        {
            if (!CanJoinAtmosphere(component))
                return;

            // We try to get a valid, simulated atmosphere.
            if (!Get<AtmosphereSystem>().TryGetSimulatedGridAtmosphere(component.Owner.Transform.MapPosition, out var atmosphere))
                return;

            component.LastProcess = _gameTiming.CurTime;
            component.Atmosphere = atmosphere;
            atmosphere.AddAtmosDevice(component);

            RaiseLocalEvent(component.Owner.Uid, new AtmosDeviceEnabledEvent(atmosphere), false);
        }

        public void LeaveAtmosphere(AtmosDeviceComponent component)
        {
            var atmosphere = component.Atmosphere;
            atmosphere?.RemoveAtmosDevice(component);
            component.Atmosphere = null;
            component.LastProcess = TimeSpan.Zero;

            if(atmosphere != null)
                RaiseLocalEvent(component.Owner.Uid, new AtmosDeviceDisabledEvent(atmosphere), false);
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

        private void OnDeviceBodyTypeChanged(EntityUid uid, AtmosDeviceComponent component, PhysicsBodyTypeChangedEvent args)
        {
            // Do nothing if the component doesn't require being anchored to function.
            if (!component.RequireAnchored)
                return;

            if (args.New == BodyType.Static)
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
