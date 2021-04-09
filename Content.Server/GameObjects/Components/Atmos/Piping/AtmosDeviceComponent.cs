#nullable enable
using System;
using Content.Server.Atmos;
using Content.Server.GameObjects.EntitySystems;
using Robust.Shared.GameObjects;
using Robust.Shared.Physics;

namespace Content.Server.GameObjects.Components.Atmos.Piping
{
    /// <summary>
    ///     Adds itself to a <see cref="IGridAtmosphereComponent"/> to be updated by.
    ///     TODO: Make compatible with unanchoring/anchoring. Currently assumes that the Owner does not move.
    /// </summary>
    [RegisterComponent]
    public class AtmosDeviceComponent : Component
    {
        private static readonly AtmosDeviceUpdateEvent Event = new ();

        public override string Name => "AtmosDevice";

        public IGridAtmosphereComponent? Atmosphere { get; private set; }

        public float DeltaTime { get; private set; }

        public override void Initialize()
        {
            base.Initialize();

            JoinAtmosphere();
        }

        private bool CanJoinAtmosphere()
        {
            return !Owner.TryGetComponent(out PhysicsComponent? physics) || physics.BodyType == BodyType.Static;
        }

        public override void OnRemove()
        {
            base.OnRemove();
            LeaveAtmosphere();
        }

        public void Update(float timer)
        {
            DeltaTime = timer;
            Owner.EntityManager.EventBus.RaiseLocalEvent(Owner.Uid, Event);
        }

        public void JoinAtmosphere()
        {
            if (!CanJoinAtmosphere())
                return;

            // We try to get a valid, non-space atmosphere.
            if (!EntitySystem.Get<AtmosphereSystem>().TryGetSimulatedGridAtmosphere(Owner.Transform.GridID, out var atmosphere))
                return;

            Atmosphere = atmosphere;
            Atmosphere.AddAtmosDevice(this);
        }

        public void LeaveAtmosphere()
        {
            Atmosphere?.RemoveAtmosDevice(this);
            Atmosphere = null;
        }

        public void RejoinAtmosphere()
        {
            LeaveAtmosphere();
            JoinAtmosphere();
        }
    }

    public class AtmosDeviceUpdateEvent : EntityEventArgs
    {
    }
}
