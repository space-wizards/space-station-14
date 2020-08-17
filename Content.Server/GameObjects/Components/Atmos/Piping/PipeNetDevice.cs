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
    public abstract class PipeNetDevice : Component
    {
        public abstract void Update();

        private IGridAtmosphereComponent CurrentGridAtmos => EntitySystem.Get<AtmosphereSystem>().GetGridAtmosphere(Owner.Transform.GridID);

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
            _joinedGridAtmos = CurrentGridAtmos;
            _joinedGridAtmos?.AddPipeNetDevice(this);
        }

        private void LeaveGridAtmos()
        {
            _joinedGridAtmos?.RemovePipeNetDevice(this);
            _joinedGridAtmos = null;
        }
    }
}
