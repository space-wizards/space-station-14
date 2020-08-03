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
using Robust.Shared.Localization;
using Robust.Shared.Map;
using Robust.Shared.Utility;

namespace Content.Server.GameObjects.Components.Atmos
{
    [RegisterComponent]
    public class GasAnalyzerComponent : Component, IAfterInteract
    {
#pragma warning disable 649
        [Dependency] private IServerNotifyManager _notifyManager = default!;
        [Dependency] private ILocalizationManager _loc = default!;
#pragma warning restore 649
        public override string Name => "GasAnalyzer";

        void IAfterInteract.AfterInteract(AfterInteractEventArgs eventArgs)
        {
            if (!eventArgs.CanReach)
            {
                _notifyManager.PopupMessage(eventArgs.ClickLocation, eventArgs.User, _loc.GetString("You can't reach there!"));
                return;
            }

            var gam = EntitySystem.Get<AtmosphereSystem>().GetGridAtmosphere(Owner.Transform.GridID);

            var tile = gam?.GetTile(eventArgs.ClickLocation).Air;

            if (tile == null)
            {
                _notifyManager.PopupMessage(eventArgs.ClickLocation, eventArgs.User, _loc.GetString("No atmosphere there!"));
                return;
            }

            string message = "";
            message += _loc.GetString("Pressure: {0:0.##} kPa\n", tile.Pressure);
            message += _loc.GetString("Temperature: {0}K ({1}°C)", tile.Temperature, TemperatureHelpers.KelvinToCelsius(tile.Temperature));

            for (int i = 0; i < Atmospherics.TotalNumberOfGases; i++)
            {
                var gas = Atmospherics.GetGas(i);

                if (tile.Gases[i] <= Atmospherics.GasMinMoles) continue;

                message += _loc.GetString("\n{0}: {1} mol", gas.Name, tile.Gases[i]);
            }

            _notifyManager.PopupMessage(eventArgs.ClickLocation, eventArgs.User, message);
        }
    }
}
