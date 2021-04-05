#nullable enable
using Content.Server.Atmos;
using Content.Server.GameObjects.EntitySystems;
using Robust.Shared.GameObjects;

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

        public override string Name => "PipeNetDevice";

        private IGridAtmosphereComponent? JoinedGridAtmos { get; set; }

        private PipeNetUpdateMessage _cachedUpdateMessage = new();

        public override void Initialize()
        {
            base.Initialize();
            JoinGridAtmos();
        }

        public override void OnRemove()
        {
            base.OnRemove();
            LeaveGridAtmos();
        }

        public void Update()
        {
            SendMessage(_cachedUpdateMessage);
            Owner.EntityManager.EventBus.RaiseLocalEvent(Owner.Uid, Event);
        }

        private void JoinGridAtmos()
        {
            var gridAtmos = EntitySystem.Get<AtmosphereSystem>()
                .GetGridAtmosphere(Owner.Transform.GridID);
            JoinedGridAtmos = gridAtmos;
            JoinedGridAtmos.AddPipeNetDevice(this);
        }

        private void LeaveGridAtmos()
        {
            JoinedGridAtmos?.RemovePipeNetDevice(this);
            JoinedGridAtmos = null;
        }
    }

    public class PipeNetUpdateMessage : ComponentMessage
    {

    }

    public class AtmosDeviceUpdateEvent : EntityEventArgs
    {

    }
}
