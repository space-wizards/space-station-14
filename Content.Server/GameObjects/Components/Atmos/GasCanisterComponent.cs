#nullable enable
using System;
using System.Linq;
using Content.Server.Atmos;
using Content.Server.GameObjects.Components.Atmos.Piping;
using Content.Server.Interfaces;
using Content.Server.Utility;
using Content.Shared.Atmos;
using Content.Shared.GameObjects.Components.Atmos;
using Content.Shared.GameObjects.EntitySystems.ActionBlocker;
using Content.Shared.Interfaces.GameObjects.Components;
using Robust.Server.GameObjects;
using Robust.Shared.GameObjects;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.ViewVariables;
using Robust.Shared.Physics;

namespace Content.Server.GameObjects.Components.Atmos
{
    /// <summary>
    /// Component that manages gas mixtures temperature, pressure and output.
    /// </summary>
    [RegisterComponent]
    [ComponentReference(typeof(IActivate))]
    public class GasCanisterComponent : Component, IGasMixtureHolder, IActivate
    {
        public override string Name => "GasCanister";

        private const int MaxLabelLength = 32;

        [ViewVariables(VVAccess.ReadWrite)]
        public string Label { get; set; } = "Gas Canister";

        [ViewVariables(VVAccess.ReadWrite)]
        public bool ValveOpened { get; set; } = false;

        /// <summary>
        /// What <see cref="GasMixture"/> the canister contains.
        /// </summary>
        [ViewVariables(VVAccess.ReadWrite)]
        [DataField("gasMixture")]
        public GasMixture Air { get; set; } = new (DefaultVolume);

        [ViewVariables]
        public bool Anchored => !Owner.TryGetComponent<IPhysBody>(out var physics) || physics.BodyType == BodyType.Static;

        /// <summary>
        /// The floor connector port that the canister is attached to.
        /// </summary>
        //[ViewVariables]
        // TODO ATMOS: this pls
        //public GasCanisterPortComponent? ConnectedPort { get; private set; }

        //[ViewVariables]
        //public bool ConnectedToPort => ConnectedPort != null;

        public const float DefaultVolume = 10;

        [ViewVariables(VVAccess.ReadWrite)] public float ReleasePressure { get; set; }

        /// <summary>
        /// The user interface bound to the canister.
        /// </summary>
        private BoundUserInterface? UserInterface => Owner.GetUIOrNull(SharedGasCanisterComponent.GasCanisterUiKey.Key);

        /// <summary>
        /// Stores the last ui state after it's been casted into <see cref="GasCanisterBoundUserInterface"/>
        /// </summary>
        private GasCanisterBoundUserInterfaceState? _lastUiState;

        private AppearanceComponent? _appearance;

        public override void Initialize()
        {
            base.Initialize();
            if (Owner.TryGetComponent<IPhysBody>(out var physics))
            {
                //AnchorUpdate();
            }
            if (UserInterface != null)
            {
                UserInterface.OnReceiveMessage += OnUiReceiveMessage;
            }

            // Init some variables
            Label = Owner.Name;
            Owner.TryGetComponent(out _appearance);

            UpdateUserInterface();
            UpdateAppearance();
        }

        /*public override void HandleMessage(ComponentMessage message, IComponent? component)
        {
            base.HandleMessage(message, component);
            switch (message)
            {
                case AnchoredChangedMessage:
                    AnchorUpdate();
                    break;
            }
        }

        #region Connector port methods

        public override void OnRemove()
        {
            base.OnRemove();
            if (UserInterface != null)
            {
                UserInterface.OnReceiveMessage -= OnUiReceiveMessage;
            }
            DisconnectFromPort();
        }

        public void TryConnectToPort()
        {
            if (!Owner.TryGetComponent<SnapGridComponent>(out var snapGrid)) return;
            var port = snapGrid.GetLocal()
                .Select(entity => entity.TryGetComponent<GasCanisterPortComponent>(out var port) ? port : null)
                .Where(port => port != null)
                .Where(port => !port!.ConnectedToCanister)
                .FirstOrDefault();
            if (port == null) return;
            ConnectedPort = port;
            ConnectedPort.ConnectGasCanister(this);
        }


        public void DisconnectFromPort()
        {
            ConnectedPort?.DisconnectGasCanister();
            ConnectedPort = null;
        }

        private void AnchorUpdate()
        {
            if (Anchored)
            {
                TryConnectToPort();
            }
            else
            {
                DisconnectFromPort();
            }
            UpdateAppearance();
        }

        #endregion*/

        void IActivate.Activate(ActivateEventArgs eventArgs)
        {
            if (!eventArgs.User.TryGetComponent(out IActorComponent? actor))
                return;

            UserInterface?.Open(actor.playerSession);
        }

        private void OnUiReceiveMessage(ServerBoundUserInterfaceMessage obj)
        {
            if (obj.Session.AttachedEntity == null)
            {
                return;
            }

            if (!PlayerCanUse(obj.Session.AttachedEntity))
            {
                return;
            }

            // If the label has been changed by a client
            if (obj.Message is CanisterLabelChangedMessage canLabelMessage)
            {
                var newLabel = canLabelMessage.NewLabel;
                if (newLabel.Length > MaxLabelLength)
                    newLabel = newLabel.Substring(0, MaxLabelLength);
                Label = newLabel;
                Owner.Name = Label;
                UpdateUserInterface();
                return;
            }

            // If the release pressure has been adjusted by the client on the gas canister
            if (obj.Message is ReleasePressureButtonPressedMessage rPMessage)
            {
                ReleasePressure += rPMessage.ReleasePressure;
                ReleasePressure = Math.Clamp(ReleasePressure, 0, 1000);
                ReleasePressure = MathF.Round(ReleasePressure, 2);
                UpdateUserInterface();
                return;
            }


            if (obj.Message is UiButtonPressedMessage btnPressedMessage)
            {
                switch (btnPressedMessage.Button)
                {
                    case UiButton.ValveToggle:
                        ToggleValve();
                        break;
                }
            }
        }

        /// <summary>
        /// Update the user interface if relevant
        /// </summary>
        private void UpdateUserInterface()
        {
            var state = GetUserInterfaceState();

            if (_lastUiState != null && _lastUiState.Equals(state))
            {
                return;
            }

            _lastUiState = state;
            UserInterface?.SetState(state);
        }

        /// <summary>
        /// Update the canister's sprite
        /// </summary>
        private void UpdateAppearance()
        {
            //_appearance?.SetData(GasCanisterVisuals.ConnectedState, ConnectedToPort);
            // The Eris canisters are being used, so best to use the Eris light logic unless someone else has a better idea.
            // https://github.com/discordia-space/CEV-Eris/blob/fdd6ee7012f46838a6711adb1737cd90c48ae448/code/game/machinery/atmoalter/canister.dm#L129
            if (Air.Pressure < 10)
            {
	            _appearance?.SetData(GasCanisterVisuals.PressureState, 0);
            }
            else if (Air.Pressure < Atmospherics.OneAtmosphere)
            {
	            _appearance?.SetData(GasCanisterVisuals.PressureState, 1);
            }
            else if (Air.Pressure < (15 * Atmospherics.OneAtmosphere))
            {
	            _appearance?.SetData(GasCanisterVisuals.PressureState, 2);
            }
            else
            {
	            _appearance?.SetData(GasCanisterVisuals.PressureState, 3);
            }
        }

        /// <summary>
        /// Get the current interface state from server data
        /// </summary>
        /// <returns>The state</returns>
        private GasCanisterBoundUserInterfaceState GetUserInterfaceState()
        {
            // We round the pressure for ease of reading
            return new GasCanisterBoundUserInterfaceState(Label,
                MathF.Round(Air.Pressure, 2),
                ReleasePressure,
                ValveOpened);
        }

        public void AirWasUpdated()
        {
            UpdateUserInterface();
            UpdateAppearance();
        }

        #region Check methods

        private bool PlayerCanUse(IEntity? player)
        {
            if (player == null)
            {
                return false;
            }

            if (!ActionBlockerSystem.CanInteract(player) ||
                !ActionBlockerSystem.CanUse(player))
            {
                return false;
            }

            return true;
        }

        #endregion


        /// <summary>
        /// Called when the canister's valve is toggled
        /// </summary>
        private void ToggleValve()
        {
            ValveOpened = !ValveOpened;
            UpdateUserInterface();
        }

        /// <summary>
        /// Called every frame
        /// </summary>
        /// <param name="frameTime"></param>
        public void Update(in float frameTime)
        {
            if (ValveOpened)
            {
                var tileAtmosphere = Owner.Transform.Coordinates.GetTileAtmosphere();
                if (tileAtmosphere != null)
                {
                    // If tileAtmosphere.Air is null, then we're airblocked, so DON'T release
                    if (tileAtmosphere.Air != null)
                    {
                        Air.ReleaseGasTo(tileAtmosphere.Air, ReleasePressure);
                        tileAtmosphere.Invalidate();
                    }
                }
                else
                {
                    Air.ReleaseGasTo(null, ReleasePressure);
                }

                AirWasUpdated();
            }
        }
    }
}
