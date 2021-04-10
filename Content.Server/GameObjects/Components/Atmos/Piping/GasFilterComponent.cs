#nullable enable
using System;
using System.Linq;
using Content.Server.Atmos;
using Content.Server.GameObjects.Components.NodeContainer;
using Content.Server.GameObjects.Components.NodeContainer.Nodes;
using Content.Server.Interfaces.GameObjects;
using Content.Shared.Atmos;
using Content.Shared.GameObjects.Components.Atmos;
using Robust.Server.GameObjects;
using Robust.Shared.Containers;
using Robust.Shared.GameObjects;
using Robust.Shared.Log;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.ViewVariables;

namespace Content.Server.GameObjects.Components.Atmos.Piping
{
    [RegisterComponent]
    public class GasFilterComponent : Component, IAtmosProcess
    {
        public override string Name => "GasFilter";

        /// <summary>
        ///     If the filter is currently filtering.
        /// </summary>
        [ViewVariables(VVAccess.ReadWrite)]
        public bool Enabled
        {
            get => _enabled;
            set
            {
                _enabled = value;
                UpdateAppearance();
            }
        }
        private bool _enabled;

        [ViewVariables(VVAccess.ReadWrite)]
        public Gas GasToFilter
        {
            get => _gasToFilter;
            set
            {
                _gasToFilter = value;
                UpdateAppearance();
            }
        }

        [DataField("gasToFilter")] private Gas _gasToFilter = Gas.Plasma;

        [ViewVariables(VVAccess.ReadWrite)]
        public int VolumeFilterRate
        {
            get => _volumeFilterRate;
            set => _volumeFilterRate = Math.Clamp(value, 0, MaxVolumeFilterRate);
        }

        [DataField("startingVolumePumpRate")]
        private int _volumeFilterRate;

        [ViewVariables(VVAccess.ReadWrite)]
        public int MaxVolumeFilterRate
        {
            get => _maxVolumeFilterRate;
            set => Math.Max(value, 0);
        }

        [DataField("maxVolumePumpRate")] private int _maxVolumeFilterRate = 100;

        [DataField("inlet")] [ViewVariables]
        private string _inletName = "inlet";

        /// <summary>
        ///     The direction the filtered-out gas goes.
        /// </summary>
        [DataField("filter")] [ViewVariables]
        private string _filter = "filter";

        /// <summary>
        ///     The direction the rest of the gas goes.
        /// </summary>
        [DataField("outlet")] [ViewVariables]
        private string _outlet = "outlet";

        [ViewVariables]
        private PipeNode? _inletPipe;

        [ViewVariables]
        private PipeNode? _filterOutletPipe;

        [ViewVariables]
        private PipeNode? _outletPipe;

        [ComponentDependency]
        private readonly AppearanceComponent? _appearance = default;

        public override void Initialize()
        {
            base.Initialize();
            UpdateAppearance();
        }

        private void UpdateAppearance()
        {
            _appearance?.SetData(FilterVisuals.VisualState, new FilterVisualState(Enabled));
        }

        public void ProcessAtmos(IGridAtmosphereComponent atmosphere)
        {
            if (!Enabled)
                return;

            if (!Owner.TryGetComponent(out NodeContainerComponent? nodeContainer))
                return;

            if(!nodeContainer.Nodes.TryGetValue(_inletName, out var inlet))
        }
    }
}
