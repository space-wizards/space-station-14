#nullable enable
using System.Collections.Generic;
using System.Threading.Tasks;
using Content.Server.GameObjects.EntitySystems;
using Content.Server.Interfaces.GameObjects.Components.Items;
using Content.Server.Utility;
using Content.Shared.Atmos;
using Content.Shared.GameObjects.Components;
using Content.Shared.GameObjects.Components.Atmos;
using Content.Shared.GameObjects.EntitySystems;
using Content.Shared.Interfaces;
using Content.Shared.Interfaces.GameObjects.Components;
using Robust.Server.GameObjects;
using Robust.Server.Player;
using Robust.Shared.GameObjects;
using Robust.Shared.Localization;
using Robust.Shared.Map;
using Robust.Shared.Players;
using Robust.Shared.ViewVariables;

namespace Content.Server.GameObjects.Components.Atmos
{
    [RegisterComponent]
    public class GasAnalyzerComponent : SharedGasAnalyzerComponent, IAfterInteract, IDropped, IUse
    {
        private GasAnalyzerDanger _pressureDanger;
        private float _timeSinceSync;
        private const float TimeBetweenSyncs = 2f;
        private bool _checkPlayer = false; // Check at the player pos or at some other tile?
        private EntityCoordinates? _position; // The tile that we scanned
        private AppearanceComponent? _appearance;

        [ViewVariables] private BoundUserInterface? UserInterface => Owner.GetUIOrNull(GasAnalyzerUiKey.Key);

        public override void Initialize()
        {
            base.Initialize();

            if (UserInterface != null)
            {
                UserInterface.OnReceiveMessage += UserInterfaceOnReceiveMessage;
                UserInterface.OnClosed += UserInterfaceOnClose;
            }

            Owner.TryGetComponent(out _appearance);
        }

        public override ComponentState GetComponentState(ICommonSession player)
        {
            return new GasAnalyzerComponentState(_pressureDanger);
        }

        /// <summary>
        /// Call this from other components to open the gas analyzer UI.
        /// Uses the player position.
        /// </summary>
        /// <param name="session">The session to open the ui for</param>
        public void OpenInterface(IPlayerSession session)
        {
            _checkPlayer = true;
            _position = null;
            UserInterface?.Open(session);
            UpdateUserInterface();
            UpdateAppearance(true);
            Resync();
        }

        /// <summary>
        /// Call this from other components to open the gas analyzer UI.
        /// Uses a given position.
        /// </summary>
        /// <param name="session">The session to open the ui for</param>
        /// <param name="pos">The position to analyze the gas</param>
        public void OpenInterface(IPlayerSession session, EntityCoordinates pos)
        {
            _checkPlayer = false;
            _position = pos;
            UserInterface?.Open(session);
            UpdateUserInterface();
            UpdateAppearance(true);
            Resync();
        }

        public void ToggleInterface(IPlayerSession session)
        {
            if (UserInterface == null)
                return;

            if (UserInterface.SessionHasOpen(session))
                CloseInterface(session);
            else
                OpenInterface(session);
        }

        public void CloseInterface(IPlayerSession session)
        {
            _position = null;
            UserInterface?.Close(session);
            // Our OnClose will do the appearance stuff
            Resync();
        }

        private void UserInterfaceOnClose(IPlayerSession obj)
        {
            UpdateAppearance(false);
        }

        private void UpdateAppearance(bool open)
        {
            _appearance?.SetData(GasAnalyzerVisuals.VisualState,
                open ? GasAnalyzerVisualState.Working : GasAnalyzerVisualState.Off);
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
            var gam = EntitySystem.Get<AtmosphereSystem>().GetGridAtmosphere(Owner.Transform.Coordinates);
            var tile = gam?.GetTile(Owner.Transform.Coordinates)?.Air;
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
            if (UserInterface == null)
            {
                return;
            }

            string? error = null;

            // Check if the player is still holding the gas analyzer => if not, don't update
            foreach (var session in UserInterface.SubscribedSessions)
            {
                if (session.AttachedEntity == null)
                    return;

                if (!session.AttachedEntity.TryGetComponent(out IHandsComponent? handsComponent))
                    return;

                var activeHandEntity = handsComponent?.GetActiveHand?.Owner;
                if (activeHandEntity == null || !activeHandEntity.TryGetComponent(out GasAnalyzerComponent? gasAnalyzer))
                {
                    return;
                }
            }

            var pos = Owner.Transform.Coordinates;
            if (!_checkPlayer && _position.HasValue)
            {
                // Check if position is out of range => don't update
                if (!_position.Value.InRange(Owner.EntityManager, pos, SharedInteractionSystem.InteractionRange))
                    return;

                pos = _position.Value;
            }

            var atmosSystem = EntitySystem.Get<AtmosphereSystem>();
            var gam = atmosSystem.GetGridAtmosphere(pos);
            var tile = gam.GetTile(pos)?.Air;
            if (tile == null)
            {
                error = "No Atmosphere!";
                UserInterface.SetState(
                new GasAnalyzerBoundUserInterfaceState(
                    0,
                    0,
                    null,
                    error));
                return;
            }

            var gases = new List<GasEntry>();

            for (var i = 0; i < Atmospherics.TotalNumberOfGases; i++)
            {
                var gas = atmosSystem.GetGas(i);

                if (tile.Gases[i] <= Atmospherics.GasMinMoles) continue;

                gases.Add(new GasEntry(gas.Name, tile.Gases[i], gas.Color));
            }

            UserInterface.SetState(
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

                    if (!player.TryGetComponent(out IHandsComponent? handsComponent))
                    {
                        Owner.PopupMessage(player, Loc.GetString("You have no hands."));
                        return;
                    }

                    var activeHandEntity = handsComponent.GetActiveHand?.Owner;
                    if (activeHandEntity == null || !activeHandEntity.TryGetComponent(out GasAnalyzerComponent? gasAnalyzer))
                    {
                        serverMsg.Session.AttachedEntity?.PopupMessage(Loc.GetString("You need a Gas Analyzer in your hand!"));
                        return;
                    }

                    UpdateUserInterface();
                    Resync();
                    break;
            }
        }

        async Task<bool> IAfterInteract.AfterInteract(AfterInteractEventArgs eventArgs)
        {
            if (!eventArgs.CanReach)
            {
                eventArgs.User.PopupMessage(Loc.GetString("You can't reach there!"));
                return true;
            }

            if (eventArgs.User.TryGetComponent(out IActorComponent? actor))
            {
                OpenInterface(actor.playerSession, eventArgs.ClickLocation);
            }

            return true;
        }



        void IDropped.Dropped(DroppedEventArgs eventArgs)
        {
            if (eventArgs.User.TryGetComponent(out IActorComponent? actor))
            {
                CloseInterface(actor.playerSession);
            }
        }

        bool IUse.UseEntity(UseEntityEventArgs eventArgs)
        {
            if (eventArgs.User.TryGetComponent(out IActorComponent? actor))
            {
                ToggleInterface(actor.playerSession);
                return true;
            }
            return false;
        }
    }
}
