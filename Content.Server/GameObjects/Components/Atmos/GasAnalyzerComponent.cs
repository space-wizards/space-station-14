#nullable enable
using Content.Server.GameObjects.EntitySystems;
using Content.Server.Interfaces;
using Content.Server.Interfaces.GameObjects.Components.Items;
using Content.Shared.Atmos;
using Content.Shared.GameObjects.Components;
using Content.Shared.Interfaces.GameObjects.Components;
using Robust.Server.GameObjects.Components.UserInterface;
using Robust.Server.Interfaces.GameObjects;
using Robust.Server.Interfaces.Player;
using Robust.Shared.GameObjects;
using Robust.Shared.GameObjects.Systems;
using Robust.Shared.IoC;
using Robust.Shared.Localization;
using Robust.Shared.Maths;
using System.Collections.Generic;

namespace Content.Server.GameObjects.Components.Atmos
{
    [RegisterComponent]
    public class GasAnalyzerComponent : SharedGasAnalyzerComponent, IAfterInteract, IDropped, IUse
    {
#pragma warning disable 649
        [Dependency] private IServerNotifyManager _notifyManager = default!;
#pragma warning restore 649

        private BoundUserInterface _userInterface = default!;
        private GasAnalyzerDanger _pressureDanger;
        private float _timeSinceSync;
        private const float TimeBetweenSyncs = 2f;
        private bool _checkPlayer = false; // Check at the player pos or at some other tile?
        private Vector2 _offset; // The direction in which you're holding the analyzer

        public override void Initialize()
        {
            base.Initialize();
            _userInterface = Owner.GetComponent<ServerUserInterfaceComponent>()
                .GetBoundUserInterface(GasAnalyzerUiKey.Key);
            _userInterface.OnReceiveMessage += UserInterfaceOnReceiveMessage;
        }

        public override ComponentState GetComponentState()
        {
            return new GasAnalyzerComponentState(_pressureDanger);
        }

        /// <summary>
        /// Call this from other components to open the gas analyzer UI.
        /// </summary>
        public void OpenInterface(IPlayerSession session)
        {
            _userInterface.Open(session);
            UpdateUserInterface();
            Resync();
        }

        public void CloseInterface(IPlayerSession session)
        {
            _userInterface.Close(session);
            Resync();
        }

        public void Update(float frameTime)
        {
            _timeSinceSync += frameTime;
            if (_timeSinceSync > TimeBetweenSyncs)
            {
                Resync();
                UpdateUserInterface();
            }
        }

        private void Resync()
        {
            // Already get the pressure before Dirty(), because we can't get the EntitySystem in that thread or smth
            var pressure = 0f;
            var gam = EntitySystem.Get<AtmosphereSystem>().GetGridAtmosphere(Owner.Transform.GridID);
            var tile = gam?.GetTile(Owner.Transform.GridPosition).Air;
            if (tile != null)
            {
                pressure = tile.Pressure;
            }

            if (pressure >= Atmospherics.HazardHighPressure || pressure <= Atmospherics.HazardLowPressure)
            {
                _pressureDanger = GasAnalyzerDanger.Hazard;
            }
            else if (pressure >= Atmospherics.WarningHighPressure || pressure <= Atmospherics.WarningLowPressure)
            {
                _pressureDanger = GasAnalyzerDanger.Warning;
            }
            else
            {
                _pressureDanger = GasAnalyzerDanger.Nominal;
            }
            
            Dirty();
            _timeSinceSync = 0f;
        }

        private void UpdateUserInterface()
        {
            string? error = null;

            // Check if the player is still holding the gas analyzer => if not, don't update
            foreach (var session in _userInterface.SubscribedSessions)
            {
                if (session.AttachedEntity == null)
                    return;

                if (!session.AttachedEntity.TryGetComponent(out IHandsComponent handsComponent))
                    return;

                var activeHandEntity = handsComponent?.GetActiveHand?.Owner;
                if (activeHandEntity == null || !activeHandEntity.TryGetComponent(out GasAnalyzerComponent gasAnalyzer))
                {
                    return;
                }
            }

            var pos = Owner.Transform.GridPosition;
            if (!_checkPlayer)
            {
                pos.Offset(_offset);
            }

            var gam = EntitySystem.Get<AtmosphereSystem>().GetGridAtmosphere(pos.GridID);
            var tile = gam?.GetTile(pos).Air;
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

            var gases = new List<GasEntry>();
            for (int i = 0; i < Atmospherics.TotalNumberOfGases; i++)
            {
                var gas = Atmospherics.GetGas(i);

                if (tile.Gases[i] <= Atmospherics.GasMinMoles) continue;

                gases.Add(new GasEntry(gas.Name, tile.Gases[i], gas.Color));
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
                    Resync();
                    break;
            }
        }

        void IAfterInteract.AfterInteract(AfterInteractEventArgs eventArgs)
        {
            if (!eventArgs.CanReach)
            {
                _notifyManager.PopupMessage(eventArgs.User, eventArgs.User, Loc.GetString("You can't reach there!"));
                return;
            }

            if (eventArgs.User.TryGetComponent(out IActorComponent actor))
            {
                _checkPlayer = false;
                _offset = eventArgs.ClickLocation.Position - eventArgs.User.Transform.GridPosition.Position;
                OpenInterface(actor.playerSession);
                //TODO: show other sprite when ui open?
            }
        }



        void IDropped.Dropped(DroppedEventArgs eventArgs)
        {
            if (eventArgs.User.TryGetComponent(out IActorComponent actor))
            {
                CloseInterface(actor.playerSession);
                //TODO: if other sprite is shown, change again
            }
        }

        bool IUse.UseEntity(UseEntityEventArgs eventArgs)
        {
            if (eventArgs.User.TryGetComponent(out IActorComponent actor))
            {
                _checkPlayer = true;
                OpenInterface(actor.playerSession);
                //TODO: show other sprite when ui open?
                return true;
            }
            return false;
        }
    }
}
