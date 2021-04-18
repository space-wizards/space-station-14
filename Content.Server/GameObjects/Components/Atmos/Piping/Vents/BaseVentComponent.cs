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

namespace Content.Server.GameObjects.Components.Atmos.Piping.Vents
{
    /// <summary>
    ///     Transfers gas from a <see cref="PipeNode"/> to the tile it is on.
    /// </summary>
    public abstract class BaseVentComponent : Component
    {

        [ViewVariables]
        private PipeNode? _ventInlet;

        private AtmosphereSystem? _atmosSystem;

        [ViewVariables(VVAccess.ReadWrite)]
        public bool VentEnabled
        {
            get => _ventEnabled;
            set
            {
                _ventEnabled = value;
                UpdateAppearance();
            }
        }
        private bool _ventEnabled = true;

        private AppearanceComponent? _appearance;

        public override void Initialize()
        {
            base.Initialize();
            Owner.EnsureComponentWarn<PipeNetDeviceComponent>();
            _atmosSystem = EntitySystem.Get<AtmosphereSystem>();
            SetInlet();
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
            if (!VentEnabled)
                return;

            var tileAtmos = Owner.Transform.Coordinates.GetTileAtmosphere();

            if (_ventInlet == null || tileAtmos == null || tileAtmos.Air == null)
                return;

            VentGas(_ventInlet.Air, tileAtmos.Air);
            tileAtmos.Invalidate();
        }

        protected abstract void VentGas(GasMixture inletGas, GasMixture outletGas);

        private void SetInlet()
        {
            if (!Owner.TryGetComponent<NodeContainerComponent>(out var container))
            {
                Logger.Warning($"{nameof(BaseVentComponent)} on {Owner?.Prototype?.ID}, Uid {Owner?.Uid} did not have a {nameof(NodeContainerComponent)}.");
                return;
            }
            _ventInlet = container.Nodes.OfType<PipeNode>().FirstOrDefault();
            if (_ventInlet == null)
            {
                Logger.Warning($"{nameof(BaseVentComponent)} on {Owner?.Prototype?.ID}, Uid {Owner?.Uid} could not find compatible {nameof(PipeNode)}s on its {nameof(NodeContainerComponent)}.");
                return;
            }
        }

        private void UpdateAppearance()
        {
            _appearance?.SetData(VentVisuals.VisualState, new VentVisualState(VentEnabled));
        }
    }
}
