#nullable enable
using Content.Server.GameObjects.EntitySystems;
using Content.Server.GameObjects.EntitySystems.Click;
using Content.Shared.Atmos;
using Robust.Shared.GameObjects;
using Robust.Shared.GameObjects.Systems;
using Robust.Shared.Map;
using Robust.Shared.Utility;

namespace Content.Server.GameObjects.Components.Atmos
{
    [RegisterComponent]
    public class GasAnalyzerComponent : Component, IExamine
    {
        public override string Name => "GasAnalyzer";
        public void Examine(FormattedMessage message, bool inDetailsRange)
        {
            if (!inDetailsRange) return;

            var gam = EntitySystem.Get<AtmosphereSystem>().GetGridAtmosphere(Owner.Transform.GridID);

            var tile = gam?.GetTile(Owner.Transform.GridPosition).Air;

            if (tile == null) return;

            message.AddText($"Pressure: {tile.Pressure}\n");
            message.AddText($"Temperature: {tile.Temperature}\n");

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
