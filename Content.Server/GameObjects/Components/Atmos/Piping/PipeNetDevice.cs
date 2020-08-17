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
            SetGridAtmos();
        }

        public override void OnRemove()
        {
            base.OnRemove();
            ClearGridAtmos();
        }

        private void SetGridAtmos()
        {
            _joinedGridAtmos = CurrentGridAtmos;
            _joinedGridAtmos.AddPipeNetDevice(this);
        }

        private void ClearGridAtmos()
        {
            if (_joinedGridAtmos != null)
            {
                _joinedGridAtmos.RemovePipeNetDevice(this);
                _joinedGridAtmos = null;
            }
        }
    }
}
