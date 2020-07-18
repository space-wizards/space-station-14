using Content.Server.GameObjects.EntitySystems.Click;
using Content.Server.Interfaces.Atmos;
using Content.Shared.Atmos;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Map;
using Robust.Shared.Utility;

namespace Content.Server.GameObjects.Components.Atmos
{
    [RegisterComponent]
    public class GasAnalyzerComponent : Component, IExamine
    {
        [Dependency] private IAtmosphereMap _atmos = default!;

        public override string Name => "GasAnalyzer";
        public void Examine(FormattedMessage message, bool inDetailsRange)
        {
            if (!inDetailsRange) return;

            var gam = _atmos.GetGridAtmosphereManager(Owner.Transform.GridID);
            if (gam == null) return;

            var position = Owner.Transform.GridPosition;
            var tile = gam.GetTile(new MapIndices((int) position.X, (int) position.Y)).Air;

            if (tile == null) return;

            message.AddText($"Pressure: {tile.Pressure}\n");

            for (int i = 0; i < Atmospherics.TotalNumberOfGases; i++)
            {
                var gas = Atmospherics.GetGas(i);

                if (tile.Gases[i] <= Atmospherics.GasMinMoles) continue;

                message.AddText(gas.Name);
                message.AddText($"\n Moles: {tile.Gases[i]}\n");
            }
        }
    }
}
