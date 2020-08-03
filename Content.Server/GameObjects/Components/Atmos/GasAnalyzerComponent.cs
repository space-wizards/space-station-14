#nullable enable
using Content.Server.GameObjects.EntitySystems;
using Content.Server.Interfaces;
using Content.Shared.Atmos;
using Content.Shared.GameObjects.EntitySystems;
using Content.Shared.Interfaces;
using Content.Shared.Interfaces.GameObjects.Components;
using Content.Shared.Utility;
using Robust.Shared.GameObjects;
using Robust.Shared.GameObjects.Systems;
using Robust.Shared.IoC;
using Robust.Shared.Map;
using Robust.Shared.Utility;

namespace Content.Server.GameObjects.Components.Atmos
{
    [RegisterComponent]
    public class GasAnalyzerComponent : Component, IExamine, IAfterInteract
    {
#pragma warning disable 649
        [Dependency] private IServerNotifyManager _notifyManager = default!;
#pragma warning restore 649
        public override string Name => "GasAnalyzer";

        void IAfterInteract.AfterInteract(AfterInteractEventArgs eventArgs)
        {
            if (!eventArgs.CanReach) return;

            var gam = EntitySystem.Get<AtmosphereSystem>().GetGridAtmosphere(Owner.Transform.GridID);

            var tile = gam?.GetTile(eventArgs.ClickLocation).Air;

            if (tile == null) return;

            string message = "";
            message += $"Pressure: {tile.Pressure} kPa\n";
            message += $"Temperature: {tile.Temperature} K/{TemperatureHelpers.KelvinToCelsius(tile.Temperature)} °C\n";

            for (int i = 0; i < Atmospherics.TotalNumberOfGases; i++)
            {
                var gas = Atmospherics.GetGas(i);

                if (tile.Gases[i] <= Atmospherics.GasMinMoles) continue;

                message += $"{tile.Gases[i]} mol {gas.Name}\n";
            }
            _notifyManager.PopupMessageCursor(eventArgs.User, message);
        }

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
