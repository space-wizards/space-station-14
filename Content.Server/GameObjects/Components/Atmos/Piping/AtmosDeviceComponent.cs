#nullable enable
using System;
using Content.Server.Atmos;
using Content.Server.GameObjects.EntitySystems;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Physics;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.Timing;
using Robust.Shared.ViewVariables;

namespace Content.Server.GameObjects.Components.Atmos.Piping
{
    /// <summary>
    ///     Adds itself to a <see cref="IGridAtmosphereComponent"/> to be updated by.
    /// </summary>
    [RegisterComponent]
    public class AtmosDeviceComponent : Component
    {
        private static readonly AtmosDeviceUpdateEvent Event = new ();

        private TimeSpan _lastTime = TimeSpan.Zero;

        public override string Name => "AtmosDevice";

        /// <summary>
        ///     Whether this device requires being anchored to join an atmosphere.
        /// </summary>
        [ViewVariables(VVAccess.ReadWrite)]
        [DataField("requireAnchored")]
        public bool RequireAnchored { get; private set; } = true;

        public IGridAtmosphereComponent? Atmosphere { get; private set; }

        /// <summary>
        ///     Time since the last atmos process.
        /// </summary>
        public TimeSpan DeltaTime { get; private set; }

        public override void Initialize()
        {
            base.Initialize();
            JoinAtmosphere();
        }

        private bool CanJoinAtmosphere()
        {
            return !RequireAnchored || !Owner.TryGetComponent(out PhysicsComponent? physics) || physics.BodyType == BodyType.Static;
        }

        public override void OnRemove()
        {
            base.OnRemove();
            LeaveAtmosphere();
        }

        public void Update(IGameTiming gameTiming)
        {
            DeltaTime = gameTiming.CurTime - _lastTime;
            _lastTime = gameTiming.CurTime;
            Owner.EntityManager.EventBus.RaiseLocalEvent(Owner.Uid, Event);
        }

        public void JoinAtmosphere()
        {
            if (!CanJoinAtmosphere())
                return;

            // We try to get a valid, simulated atmosphere.
            if (!EntitySystem.Get<AtmosphereSystem>().TryGetSimulatedGridAtmosphere(Owner.Transform.MapPosition, out var atmosphere))
                return;

            Atmosphere = atmosphere;
            Atmosphere.AddAtmosDevice(this);

            // We update this as to not have it be zero by the time the next process occurs.
            _lastTime = IoCManager.Resolve<IGameTiming>().CurTime;
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
