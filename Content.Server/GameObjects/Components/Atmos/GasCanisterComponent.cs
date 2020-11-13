#nullable enable annotations
using System;
using Content.Server.Atmos;
using Content.Server.GameObjects.Components.Atmos.Piping;
using Content.Server.Interfaces;
using Robust.Shared.GameObjects;
using Robust.Shared.GameObjects.Components;
using Robust.Shared.GameObjects.Components.Transform;
using Robust.Shared.Serialization;
using Robust.Shared.ViewVariables;
using System.Linq;
using Content.Server.GameObjects.EntitySystems;
using Content.Server.Interfaces.GameObjects.Components.Items;
using Content.Server.Utility;
using Content.Shared.GameObjects.Components.Atmos;
using Content.Shared.GameObjects.EntitySystems;
using Content.Shared.Interfaces;
using Content.Shared.Interfaces.GameObjects.Components;
using Robust.Server.GameObjects;
using Robust.Server.GameObjects.Components.UserInterface;
using Robust.Server.Interfaces.GameObjects;
using Robust.Shared.Containers;
using Robust.Shared.GameObjects.Systems;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Localization;

namespace Content.Server.GameObjects.Components.Atmos
{
    /// <summary>
    /// Component that manages gas mixtures temperature, pressure and output.
    /// </summary>
    [RegisterComponent]
    public class GasCanisterComponent : Component, IGasMixtureHolder, IInteractHand
    {
        public override string Name => "GasCanister";

        [ViewVariables(VVAccess.ReadWrite)]
        public string Label = "Gas Canister";

        [ViewVariables(VVAccess.ReadWrite)]
        public bool ValveOpened = false;

        /// <summary>
        /// What <see cref="GasMixture"/> the canister contains.
        /// </summary>
        [ViewVariables(VVAccess.ReadWrite)]
        public GasMixture? Air { get; set; }

        [ViewVariables]
        public bool Anchored => !Owner.TryGetComponent<IPhysicsComponent>(out var physics) || physics.Anchored;

        /// <summary>
        /// The floor connector port that the canister is attached to.
        /// </summary>
        [ViewVariables]
        public GasCanisterPortComponent? ConnectedPort { get; private set; }

        [ViewVariables]
        public bool ConnectedToPort => ConnectedPort != null;

        private const float DefaultVolume = 10;

        [ViewVariables(VVAccess.ReadWrite)] public float ReleasePressure;

        /// <summary>
        /// The user interface bound to the canister.
        /// </summary>
        private BoundUserInterface? UserInterface => Owner.GetUIOrNull(SharedGasCanisterComponent.GasCanisterUiKey.Key);

        /// <summary>
        /// Stores the last ui state after it's been casted into <see cref="GasCanisterBoundUserInterface"/>
        /// </summary>
        private GasCanisterBoundUserInterfaceState? _lastUiState;

        private IGridAtmosphereComponent? _gridAtmosphere;

        private AppearanceComponent? _appearance;

        public override void ExposeData(ObjectSerializer serializer)
        {
            base.ExposeData(serializer);
            serializer.DataField(this, x => Air, "gasMixture", new GasMixture(DefaultVolume));
        }


        public override void Initialize()
        {
            base.Initialize();
            if (Owner.TryGetComponent<IPhysicsComponent>(out var physics))
            {
                AnchorUpdate();
                physics.AnchoredChanged += AnchorUpdate;
            }
            if (UserInterface != null)
            {
                UserInterface.OnReceiveMessage += OnUiReceiveMessage;
            }

            // Init some variables
            Label = Owner.Name;
            Owner.TryGetComponent(out _appearance);

            // Get the GridAtmosphere
            var gridId = Owner.Transform.Coordinates.GetGridId(Owner.EntityManager);
            var atmosphereSystem = EntitySystem.Get<AtmosphereSystem>();
            _gridAtmosphere = atmosphereSystem.GetGridAtmosphere(gridId);

            UpdateUserInterface();
            UpdateAppearance();
        }

        #region Connector port methods

        public override void OnRemove()
        {
            base.OnRemove();
            if (Owner.TryGetComponent<IPhysicsComponent>(out var physics))
            {
                physics.AnchoredChanged -= AnchorUpdate;
            }
            DisconnectFromPort();
        }

        public void TryConnectToPort()
        {
            if (!Owner.TryGetComponent<SnapGridComponent>(out var snapGrid)) return;
            var port = snapGrid.GetLocal()
                .Select(entity => entity.TryGetComponent<GasCanisterPortComponent>(out var port) ? port : null)
                .Where(port => port != null)
                .Where(port => !port.ConnectedToCanister)
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

        #endregion

        /// <summary>
        /// Manages what happens when an actor interacts with an empty hand on the canister
        /// </summary>
        /// <param name="eventArgs"></param>
        /// <returns>True if the interaction has succeded</returns>
        bool IInteractHand.InteractHand(InteractHandEventArgs eventArgs)
        {
            if (!eventArgs.User.TryGetComponent(out IActorComponent? actor))
            {
                return false;
            }
            // Duplicated code here, not sure how else to get actor inside to make UserInterface happy.

            if (IsValidInteraction(eventArgs))
            {
                UserInterface?.Open(actor.playerSession);
                return true;
            }

            return false;
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
                if (newLabel.Length > 500)
                    newLabel = newLabel.Substring(0, 500);
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
            _appearance?.SetData(GasCanisterVisuals.ConnectedState, ConnectedToPort);
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


        #region Check methods

        /// <summary>
        /// Check if the actor has the ability to do such thing
        /// </summary>
        /// <param name="eventArgs"></param>
        /// <returns>True if the actor can interact</returns>
        bool IsValidInteraction(ITargetedInteractEventArgs eventArgs)
        {
            if (!ActionBlockerSystem.CanInteract(eventArgs.User))
            {
                Owner.PopupMessage(eventArgs.User, Loc.GetString("You can't do that!"));
                return false;
            }

            if (ContainerHelpers.IsInContainer(eventArgs.User))
            {
                Owner.PopupMessage(eventArgs.User, Loc.GetString("You can't reach there!"));
                return false;
            }
            // This popup message doesn't appear on clicks, even when code was seperate. Unsure why.

            if (!eventArgs.User.HasComponent<IHandsComponent>())
            {
                Owner.PopupMessage(eventArgs.User, Loc.GetString("You have no hands!"));
                return false;
            }

            return true;
        }

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
                Air.ReleaseGasTo(tileAtmosphere.Air, ReleasePressure);
                _gridAtmosphere.Invalidate(tileAtmosphere.GridIndices);

                UpdateUserInterface();
            }
        }
    }
}
