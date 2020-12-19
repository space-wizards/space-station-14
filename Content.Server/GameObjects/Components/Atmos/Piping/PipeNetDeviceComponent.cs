using Content.Server.Atmos;
using Content.Server.GameObjects.EntitySystems;
using Robust.Shared.GameObjects;
using Robust.Shared.GameObjects.Systems;

namespace Content.Server.GameObjects.Components.Atmos.Piping
{
    /// <summary>
    ///     Adds itself to a <see cref="IGridAtmosphereComponent"/> to be updated by.
    ///     TODO: Make compatible with unanchoring/anchoring. Currently assumes that the Owner does not move.
    /// </summary>
    public abstract class PipeNetDeviceComponent : Component
    {
        public abstract void Update();

        protected IGridAtmosphereComponent JoinedGridAtmos { get; private set; }

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
