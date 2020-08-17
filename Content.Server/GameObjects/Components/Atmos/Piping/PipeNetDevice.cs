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

        /// <summary>
        ///     If the <see cref="IGridAtmosphereComponent"/> this is being updates by should continue to do so,
        ///     or get rid of this from its queue.
        /// </summary>
        public bool ContinueAtmosUpdates { get; private set; } = true;

        private IGridAtmosphereComponent CurrentGridAtmos => EntitySystem.Get<AtmosphereSystem>().GetGridAtmosphere(Owner.Transform.GridID);

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
            CurrentGridAtmos?.AddPipeNetDevice(this);
        }

        private void LeaveGridAtmos()
        {
            ContinueAtmosUpdates = false;
        }
    }
}
