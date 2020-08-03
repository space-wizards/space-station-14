#nullable enable
using Content.Server.GameObjects.EntitySystems;
using Content.Server.Interfaces;
using Content.Shared.Atmos;
using Content.Shared.GameObjects.Components;
using Content.Shared.GameObjects.EntitySystems;
using Content.Shared.Interfaces;
using Content.Shared.Interfaces.GameObjects.Components;
using Content.Shared.Utility;
using Robust.Server.GameObjects.Components.UserInterface;
using Robust.Server.Interfaces.GameObjects;
using Robust.Server.Interfaces.Player;
using Robust.Shared.GameObjects;
using Robust.Shared.GameObjects.Systems;
using Robust.Shared.IoC;
using Robust.Shared.Localization;
using Robust.Shared.Map;
using Robust.Shared.Utility;
using System;

namespace Content.Server.GameObjects.Components.Atmos
{
    [RegisterComponent]
    public class GasAnalyzerComponent : SharedGasAnalyzerComponent, IAfterInteract
    {
#pragma warning disable 649
        [Dependency] private IServerNotifyManager _notifyManager = default!;
#pragma warning restore 649

        private BoundUserInterface _userInterface = default!;

        public override void Initialize()
        {
            base.Initialize();
            _userInterface = Owner.GetComponent<ServerUserInterfaceComponent>()
                .GetBoundUserInterface(GasAnalyzerUiKey.Key);
            _userInterface.OnReceiveMessage += UserInterfaceOnReceiveMessage;
        }

        /// <summary>
        /// Call this from other components to open the wires UI.
        /// </summary>
        public void OpenInterface(IPlayerSession session)
        {
            _userInterface.Open(session);
        }

        private void UserInterfaceOnReceiveMessage(ServerBoundUserInterfaceMessage obj)
        {
            throw new NotImplementedException();
        }

        void IAfterInteract.AfterInteract(AfterInteractEventArgs eventArgs)
        {
            if (eventArgs.User.TryGetComponent(out IActorComponent actor))
            {
                OpenInterface(actor.playerSession);
                return;
            }
            

            if (!eventArgs.CanReach)
            {
                _notifyManager.PopupMessage(eventArgs.ClickLocation, eventArgs.User, Loc.GetString("You can't reach there!"));
                return;
            }

            var gam = EntitySystem.Get<AtmosphereSystem>().GetGridAtmosphere(Owner.Transform.GridID);

            var tile = gam?.GetTile(eventArgs.ClickLocation).Air;

            if (tile == null)
            {
                _notifyManager.PopupMessage(eventArgs.ClickLocation, eventArgs.User, Loc.GetString("No atmosphere there!"));
                return;
            }

            string message = "";
            message += Loc.GetString("Pressure: {0:0.##} kPa\n", tile.Pressure);
            message += Loc.GetString("Temperature: {0}K ({1}°C)", tile.Temperature, TemperatureHelpers.KelvinToCelsius(tile.Temperature));

            for (int i = 0; i < Atmospherics.TotalNumberOfGases; i++)
            {
                var gas = Atmospherics.GetGas(i);

                if (tile.Gases[i] <= Atmospherics.GasMinMoles) continue;

                message += Loc.GetString("\n{0}: {1} mol", gas.Name, tile.Gases[i]);
            }

            _notifyManager.PopupMessage(eventArgs.ClickLocation, eventArgs.User, message);
        }
    }
}
