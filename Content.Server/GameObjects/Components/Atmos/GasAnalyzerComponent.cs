#nullable enable
using Content.Server.GameObjects.EntitySystems;
using Content.Server.Interfaces;
using Content.Server.Interfaces.GameObjects.Components.Items;
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
using System.Collections.Generic;

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
            UpdateUserInterface();
        }

        private void UpdateUserInterface()
        {
            string? error = null;
            var gam = EntitySystem.Get<AtmosphereSystem>().GetGridAtmosphere(Owner.Transform.GridID);

            var tile = gam?.GetTile(Owner.Transform.GridPosition).Air;
            if (tile == null)
            {
                error = "No Atmosphere!";
                _userInterface.SetState(
                new GasAnalyzerBoundUserInterfaceState(
                    0,
                    0,
                    null,
                    error));
                return;
            }

            var gases = new List<StatusEntry>();
            for (int i = 0; i < Atmospherics.TotalNumberOfGases; i++)
            {
                var gas = Atmospherics.GetGas(i);

                if (tile.Gases[i] <= Atmospherics.GasMinMoles) continue;

                gases.Add(new StatusEntry(gas.Name, tile.Gases[i]));
            }

            _userInterface.SetState(
                new GasAnalyzerBoundUserInterfaceState(
                    tile.Pressure,
                    tile.Temperature,
                    gases.ToArray(),
                    error));
        }

        private void UserInterfaceOnReceiveMessage(ServerBoundUserInterfaceMessage serverMsg)
        {
            var message = serverMsg.Message;
            switch (message)
            {
                case GasAnalyzerRefreshMessage msg:
                    var player = serverMsg.Session.AttachedEntity;
                    if (player == null)
                    {
                        return;
                    }

                    if (!player.TryGetComponent(out IHandsComponent handsComponent))
                    {
                        _notifyManager.PopupMessage(Owner.Transform.GridPosition, player,
                            Loc.GetString("You have no hands."));
                        return;
                    }

                    var activeHandEntity = handsComponent.GetActiveHand?.Owner;
                    if (activeHandEntity == null || !activeHandEntity.TryGetComponent(out GasAnalyzerComponent gasAnalyzer))
                    {
                        _notifyManager.PopupMessage(serverMsg.Session.AttachedEntity,
                            serverMsg.Session.AttachedEntity,
                            Loc.GetString("You need a Gas Analyzer in your hand!"));
                        return;
                    }

                    UpdateUserInterface();
                    break;
            }
        }

        void IAfterInteract.AfterInteract(AfterInteractEventArgs eventArgs)
        {
            if (eventArgs.User.TryGetComponent(out IActorComponent actor))
            {
                OpenInterface(actor.playerSession);
                //TODO: show other sprite when ui open?
            }
        }
    }
}
