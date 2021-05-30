#nullable enable
using System.Linq;
using Content.Server.Atmos;
using Content.Server.GameObjects.Components.NodeContainer;
using Content.Server.GameObjects.Components.NodeContainer.Nodes;
using Content.Server.GameObjects.EntitySystems;
using Content.Shared.GameObjects.Components.Atmos;
using Robust.Server.GameObjects;
using Robust.Shared.GameObjects;
using Robust.Shared.Log;
using Robust.Shared.ViewVariables;

namespace Content.Server.GameObjects.Components.Atmos.Piping.Scrubbers
{
    /// <summary>
    ///     Transfers gas from the tile it is on to a <see cref="PipeNode"/>.
    /// </summary>
    public abstract class BaseSiphonComponent : Component
    {

        [ViewVariables]
        private PipeNode? _scrubberOutlet;

        private AtmosphereSystem? _atmosSystem;

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

        private AppearanceComponent? _appearance;

        public override void Initialize()
        {
            base.Initialize();
            Owner.EnsureComponentWarn<PipeNetDeviceComponent>();
            _atmosSystem = EntitySystem.Get<AtmosphereSystem>();
            SetOutlet();
            Owner.TryGetComponent(out _appearance);
            UpdateAppearance();
        }

        public override void HandleMessage(ComponentMessage message, IComponent? component)
        {
            base.HandleMessage(message, component);
            switch (message)
            {
                case PipeNetUpdateMessage:
                    Update();
                    break;
            }
        }

        public void Update()
        {
            if (!SiphonEnabled)
                return;

            var tileAtmos = Owner.Transform.Coordinates.GetTileAtmosphere();

            if (_scrubberOutlet == null || tileAtmos == null || tileAtmos.Air ==  null)
                return;

            ScrubGas(tileAtmos.Air, _scrubberOutlet.Air);
            tileAtmos.Invalidate();
        }

        protected abstract void ScrubGas(GasMixture inletGas, GasMixture outletGas);

        private void SetOutlet()
        {
            if (!Owner.TryGetComponent<NodeContainerComponent>(out var container))
            {
                Logger.Warning($"{nameof(BaseSiphonComponent)} on {Owner?.Prototype?.ID}, Uid {Owner?.Uid} did not have a {nameof(NodeContainerComponent)}.");
                return;
            }
            _scrubberOutlet = container.Nodes.OfType<PipeNode>().FirstOrDefault();
            if (_scrubberOutlet == null)
            {
                Logger.Warning($"{nameof(BaseSiphonComponent)} on {Owner?.Prototype?.ID}, Uid {Owner?.Uid} could not find compatible {nameof(PipeNode)}s on its {nameof(NodeContainerComponent)}.");
                return;
            }
        }

        private void UpdateAppearance()
        {
            _appearance?.SetData(SiphonVisuals.VisualState, new SiphonVisualState(SiphonEnabled));
        }
    }
}
