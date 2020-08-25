using Content.Server.Atmos;
using Content.Server.GameObjects.EntitySystems;
using Robust.Shared.GameObjects;
using Robust.Shared.GameObjects.Systems;
using Robust.Shared.Log;

namespace Content.Server.GameObjects.Components.Atmos.Piping
{
    /// <summary>
    ///     Adds itself to a <see cref="IGridAtmosphereComponent"/> to be updated by.
    ///     TODO: Make compatible with unanchoring/anchoring. Currently assumes that the Owner does not move.
    /// </summary>
    public abstract class PipeNetDeviceComponent : Component
    {
        public abstract void Update();

        private IGridAtmosphereComponent _joinedGridAtmos;

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
            if (gridAtmos == null)
            {
                Logger.Error($"{nameof(PipeNetDeviceComponent)} on entity {Owner.Uid} could not find an {nameof(IGridAtmosphereComponent)}.");
                return;
            }
            _joinedGridAtmos = gridAtmos;
            _joinedGridAtmos?.AddPipeNetDevice(this);
        }

        private void LeaveGridAtmos()
        {
            _joinedGridAtmos?.RemovePipeNetDevice(this);
            _joinedGridAtmos = null;
        }
    }
}
