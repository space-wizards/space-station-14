using Content.Server.Interfaces.GameObjects.Components.Interaction;
using Content.Server.GameObjects.Components.Power.ApcNetComponents;
using Content.Server.GameObjects.Components.Power;
using Content.Server.GameObjects.EntitySystems;
using Content.Server.Interfaces.GameTicking;
using Content.Shared.GameObjects.Components.Power;
using Robust.Server.GameObjects.Components.UserInterface;
using Robust.Server.Interfaces.GameObjects;
using Robust.Shared.GameObjects;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.IoC;

namespace Content.Server.GameObjects.Components.Power
{
    [RegisterComponent]
    [ComponentReference(typeof(IActivate))]
    public class SolarControlConsoleComponent : SharedSolarControlConsoleComponent, IActivate
    {
#pragma warning disable 649
        [Dependency] private IEntitySystemManager _entitySystemManager;
#pragma warning restore 649

        private BoundUserInterface _userInterface;
        private PowerReceiverComponent _powerReceiver;
        private PowerSolarSystem _powerSolarSystem;
        private bool Powered => _powerReceiver.Powered;

        public override void Initialize()
        {
            base.Initialize();

            _userInterface = Owner.GetComponent<ServerUserInterfaceComponent>().GetBoundUserInterface(SolarControlConsoleUiKey.Key);
            _userInterface.OnReceiveMessage += UserInterfaceOnReceiveMessage;
            _powerReceiver = Owner.GetComponent<PowerReceiverComponent>();
            _powerSolarSystem = _entitySystemManager.GetEntitySystem<PowerSolarSystem>();
        }

        public void UpdateUIState()
        {
            _userInterface.SetState(new SolarControlConsoleBoundInterfaceState(_powerSolarSystem.TargetPanelRotation, _powerSolarSystem.TargetPanelVelocity, _powerSolarSystem.TotalPanelPower, _powerSolarSystem.TowardsSun));
        }

        private void UserInterfaceOnReceiveMessage(ServerBoundUserInterfaceMessage obj)
        {
            switch (obj.Message)
            {
                case SolarControlConsoleAdjustMessage msg:
                    if (double.IsFinite(msg.Rotation))
                    {
                        _powerSolarSystem.TargetPanelRotation = msg.Rotation.Reduced();
                    }
                    if (double.IsFinite(msg.AngularVelocity))
                    {
                        _powerSolarSystem.TargetPanelVelocity = msg.AngularVelocity.Reduced();
                    }
                    break;
            }
        }

        void IActivate.Activate(ActivateEventArgs eventArgs)
        {
            if (!eventArgs.User.TryGetComponent(out IActorComponent actor))
            {
                return;
            }

            if (!Powered)
            {
                return;
            }

            // always update the UI immediately before opening, just in case
            UpdateUIState();
            _userInterface.Open(actor.playerSession);
        }
    }
}
