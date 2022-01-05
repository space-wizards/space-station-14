using System.Collections.Generic;
using System.Threading.Tasks;
using Content.Server.Atmos.EntitySystems;
using Content.Server.Hands.Components;
using Content.Server.UserInterface;
using Content.Shared.Atmos;
using Content.Shared.Atmos.Components;
using Content.Shared.Interaction;
using Content.Shared.Popups;
using Robust.Server.GameObjects;
using Robust.Server.Player;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Localization;
using Robust.Shared.Map;
using Robust.Shared.ViewVariables;

namespace Content.Server.Atmos.Components
{
    [RegisterComponent]
    [ComponentReference(typeof(IActivate))]
    [ComponentReference(typeof(SharedGasAnalyzerComponent))]
    public class GasAnalyzerComponent : SharedGasAnalyzerComponent, IAfterInteract, IDropped, IActivate
    {
        [Dependency] private readonly IEntityManager _entities = default!;

        private GasAnalyzerDanger _pressureDanger;
        private float _timeSinceSync;
        private const float TimeBetweenSyncs = 2f;
        private bool _checkPlayer = false; // Check at the player pos or at some other tile?
        private EntityCoordinates? _position; // The tile that we scanned
        private AppearanceComponent? _appearance;

        [ViewVariables] private BoundUserInterface? UserInterface => Owner.GetUIOrNull(GasAnalyzerUiKey.Key);

        protected override void Initialize()
        {
            base.Initialize();

            if (UserInterface != null)
            {
                UserInterface.OnReceiveMessage += UserInterfaceOnReceiveMessage;
                UserInterface.OnClosed += UserInterfaceOnClose;
            }

            _entities.TryGetComponent(Owner, out _appearance);
        }

        public override ComponentState GetComponentState()
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
            var tile = EntitySystem.Get<AtmosphereSystem>().GetTileMixture(_entities.GetComponent<TransformComponent>(Owner).Coordinates);
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
                if (session.AttachedEntity is not {Valid: true} playerEntity)
                    return;

                if (!_entities.TryGetComponent(playerEntity, out HandsComponent? handsComponent))
                    return;

                if (handsComponent?.GetActiveHand?.Owner is not {Valid: true} activeHandEntity ||
                    !_entities.TryGetComponent(activeHandEntity, out GasAnalyzerComponent? gasAnalyzer))
                {
                    return;
                }
            }

            var pos = _entities.GetComponent<TransformComponent>(Owner).Coordinates;
            if (!_checkPlayer && _position.HasValue)
            {
                // Check if position is out of range => don't update
                if (!_position.Value.InRange(_entities, pos, SharedInteractionSystem.InteractionRange))
                    return;

                pos = _position.Value;
            }

            var atmosphereSystem = EntitySystem.Get<AtmosphereSystem>();
            var tile = atmosphereSystem.GetTileMixture(pos);
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
                var gas = atmosphereSystem.GetGas(i);

                if (tile.Moles[i] <= Atmospherics.GasMinMoles) continue;

                gases.Add(new GasEntry(gas.Name, tile.Moles[i], gas.Color));
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
                    if (serverMsg.Session.AttachedEntity is not {Valid: true} player)
                    {
                        return;
                    }

                    if (!_entities.TryGetComponent(player, out HandsComponent? handsComponent))
                    {
                        Owner.PopupMessage(player, Loc.GetString("gas-analyzer-component-player-has-no-hands-message"));
                        return;
                    }

                    if (handsComponent.GetActiveHand?.Owner is not {Valid: true} activeHandEntity ||
                        !_entities.TryGetComponent(activeHandEntity, out GasAnalyzerComponent? gasAnalyzer))
                    {
                        serverMsg.Session.AttachedEntity.Value.PopupMessage(Loc.GetString("gas-analyzer-component-need-gas-analyzer-in-hand-message"));
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
                eventArgs.User.PopupMessage(Loc.GetString("gas-analyzer-component-player-cannot-reach-message"));
                return true;
            }

            if (_entities.TryGetComponent(eventArgs.User, out ActorComponent? actor))
            {
                OpenInterface(actor.PlayerSession, eventArgs.ClickLocation);
            }

            return true;
        }



        void IDropped.Dropped(DroppedEventArgs eventArgs)
        {
            if (_entities.TryGetComponent(eventArgs.User, out ActorComponent? actor))
            {
                CloseInterface(actor.PlayerSession);
            }
        }

        void IActivate.Activate(ActivateEventArgs eventArgs)
        {
            if (_entities.TryGetComponent(eventArgs.User, out ActorComponent? actor))
            {
                ToggleInterface(actor.PlayerSession);
                return;
            }
            return;
        }
    }
}
