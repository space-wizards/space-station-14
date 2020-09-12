using Content.Server.Atmos;
using Content.Server.GameObjects.Components.NodeContainer;
using Content.Server.GameObjects.Components.NodeContainer.Nodes;
using Content.Server.GameObjects.EntitySystems;
using Content.Shared.GameObjects.Atmos;
using Robust.Server.GameObjects;
using Robust.Shared.GameObjects.Systems;
using Robust.Shared.Log;
using Robust.Shared.ViewVariables;
using System.Linq;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.IoC;

namespace Content.Server.GameObjects.Components.Atmos.Piping
{
    /// <summary>
    ///     Transfers gas from the tile it is on to a <see cref="PipeNode"/>.
    /// </summary>
    public abstract class BaseSiphonComponent : PipeNetDeviceComponent
    {
        [Dependency] private readonly IEntityManager _entityManager = default!;

        [ViewVariables]
        private PipeNode _scrubberOutlet;

        private AtmosphereSystem _atmosSystem;

        [ViewVariables(VVAccess.ReadWrite)]
        public bool SiphonEnabled
        {
            get => _siphonEnabled;
            set
            {
                _siphonEnabled = value;
                UpdateAppearance();
            }
        }
        private bool _siphonEnabled = true;

        private AppearanceComponent _appearance;

        public override void Initialize()
        {
            base.Initialize();
            _atmosSystem = EntitySystem.Get<AtmosphereSystem>();
            if (!Owner.TryGetComponent<NodeContainerComponent>(out var container))
            {
                JoinedGridAtmos?.RemovePipeNetDevice(this);
                Logger.Error($"{typeof(BaseSiphonComponent)} on entity {Owner.Uid} did not have a {nameof(NodeContainerComponent)}.");
                return;
            }
            _scrubberOutlet = container.Nodes.OfType<PipeNode>().FirstOrDefault();
            if (_scrubberOutlet == null)
            {
                JoinedGridAtmos?.RemovePipeNetDevice(this);
                Logger.Error($"{typeof(BaseSiphonComponent)} on entity {Owner.Uid} could not find compatible {nameof(PipeNode)}s on its {nameof(NodeContainerComponent)}.");
                return;
            }
            Owner.TryGetComponent(out _appearance);
            UpdateAppearance();
        }

        public override void Update()
        {
            if (!SiphonEnabled)
                return;

            var tileAtmos = Owner.Transform.Coordinates.GetTileAtmosphere(_entityManager);
            if (tileAtmos == null)
                return;
            ScrubGas(tileAtmos.Air, _scrubberOutlet.Air);
            _atmosSystem.GetGridAtmosphere(Owner.Transform.GridID)?.Invalidate(tileAtmos.GridIndices);
        }

        protected abstract void ScrubGas(GasMixture inletGas, GasMixture outletGas);

        private void UpdateAppearance()
        {
            _appearance?.SetData(SiphonVisuals.VisualState, new SiphonVisualState(SiphonEnabled));
        }
    }
}
