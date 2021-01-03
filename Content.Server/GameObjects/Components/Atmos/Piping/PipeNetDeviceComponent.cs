#nullable enable
using Content.Server.Atmos;
using Content.Server.GameObjects.EntitySystems;
using Robust.Shared.GameObjects;
using Robust.Shared.GameObjects.Systems;
using System.Collections.Generic;

namespace Content.Server.GameObjects.Components.Atmos.Piping
{
    /// <summary>
    ///     Adds itself to a <see cref="IGridAtmosphereComponent"/> to be updated by.
    ///     TODO: Make compatible with unanchoring/anchoring. Currently assumes that the Owner does not move.
    /// </summary>
    [RegisterComponent]
    public class PipeNetDeviceComponent : Component
    {
        public override string Name => "PipeNetDevice";

        private IGridAtmosphereComponent? JoinedGridAtmos { get; set; }

        private IEnumerable<IPipeNetUpdated> DevicesToUpdate { get; set; } = new List<IPipeNetUpdated>();

        public override void Initialize()
        {
            base.Initialize();
            JoinGridAtmos();
            DevicesToUpdate = Owner.GetAllComponents<IPipeNetUpdated>();
        }

        public override void OnRemove()
        {
            base.OnRemove();
            LeaveGridAtmos();
        }

        public void Update()
        {
            var message = new PipeNetUpdateMessage();
            foreach (var device in DevicesToUpdate)
            {
                device.Update(message);
            }
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
}
