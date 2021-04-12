using System;
using Content.Server.Atmos;
using Content.Server.GameObjects.Components.NodeContainer;
using Content.Server.GameObjects.Components.NodeContainer.Nodes;
using Content.Server.Interfaces.GameObjects;
using Content.Shared.Atmos;
using Robust.Shared.GameObjects;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.ViewVariables;

namespace Content.Server.GameObjects.Components.Atmos.Piping.Trinary
{
    [RegisterComponent]
    public class GasMixerComponent : Component, IAtmosProcess
    {
        public override string Name => "GasMixer";

        [ViewVariables(VVAccess.ReadWrite)]
        private bool _enabled = true;

        [ViewVariables(VVAccess.ReadWrite)]
        [DataField("inletOne")]
        private string _inletOneName = "inletOne";

        [ViewVariables(VVAccess.ReadWrite)]
        [DataField("inletTwo")]
        private string _inletTwoName = "inletTwo";

        [ViewVariables(VVAccess.ReadWrite)]
        [DataField("outlet")]
        private string _outletName = "outlet";

        [ViewVariables(VVAccess.ReadWrite)]
        private float _targetPressure = Atmospherics.OneAtmosphere;

        [ViewVariables(VVAccess.ReadWrite)]
        private float _inletOneConcentration = 0.5f;

        [ViewVariables(VVAccess.ReadWrite)]
        private float _inletTwoConcentration = 0.5f;

        public void ProcessAtmos(float time, IGridAtmosphereComponent atmosphere)
        {
            // TODO ATMOS: Cache total moles since it's expensive.

            if (!_enabled)
                return;

            if (!Owner.TryGetComponent(out NodeContainerComponent? nodeContainer))
                return;

            if (!nodeContainer.TryGetNode(_inletOneName, out PipeNode? inletOne)
                || !nodeContainer.TryGetNode(_inletTwoName, out PipeNode? inletTwo)
                || !nodeContainer.TryGetNode(_outletName, out PipeNode? outlet))
                return;

            var outputStartingPressure = outlet.Air.Pressure;

            if (outputStartingPressure >= _targetPressure)
                return; // Target reached, no need to mix.

            var generalTransfer = (_targetPressure - outputStartingPressure) * outlet.Air.Volume / Atmospherics.R;

            var transferMolesOne = inletOne.Air.Temperature > 0 ? _inletOneConcentration * generalTransfer / inletOne.Air.Temperature : 0f;
            var transferMolesTwo = inletTwo.Air.Temperature > 0 ? _inletTwoConcentration * generalTransfer / inletTwo.Air.Temperature : 0f;

            if (_inletTwoConcentration <= 0f)
            {
                if (inletOne.Air.Temperature <= 0f)
                    return;

                transferMolesOne = MathF.Min(transferMolesOne, inletOne.Air.TotalMoles);
                transferMolesTwo = 0f;
            }

            else if (_inletOneConcentration <= 0)
            {
                if (inletTwo.Air.Temperature <= 0f)
                    return;

                transferMolesOne = 0f;
                transferMolesTwo = MathF.Min(transferMolesTwo, inletTwo.Air.TotalMoles);
            }
            else
            {
                if (inletOne.Air.Temperature <= 0f || inletTwo.Air.Temperature <= 0f)
                    return;

                if (transferMolesOne <= 0 || transferMolesTwo <= 0)
                    return;

                if (inletOne.Air.TotalMoles < transferMolesOne || inletTwo.Air.TotalMoles < transferMolesTwo)
                {
                    var ratio = MathF.Min(inletOne.Air.TotalMoles / transferMolesOne, inletTwo.Air.TotalMoles / transferMolesTwo);
                    transferMolesOne *= ratio;
                    transferMolesTwo *= ratio;
                }
            }

            // Actually transfer the gas now.

            if (transferMolesOne > 0f)
            {
                var removed = inletOne.Air.Remove(transferMolesOne);
                outlet.Air.Merge(removed);
            }

            if (transferMolesTwo > 0f)
            {
                var removed = inletTwo.Air.Remove(transferMolesTwo);
                outlet.Air.Merge(removed);
            }
        }
    }
}
