using Content.Server.GameObjects.Components.Power.ApcNetComponents;
using Content.Server.GameObjects.Components.Power;
using Content.Server.Interfaces.GameObjects.Components.Interaction;
using Content.Server.Interfaces.GameTicking;
using Content.Server.Utility;
using Content.Shared.GameObjects.Components.Command;
using Robust.Server.GameObjects.Components.UserInterface;
using Robust.Server.Interfaces.GameObjects;
using Robust.Server.Interfaces.Player;
using Robust.Shared.GameObjects;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.IoC;

namespace Content.Server.GameObjects.Components.Command
{
    [RegisterComponent]
    [ComponentReference(typeof(IActivate))]
    public class CommunicationsConsoleComponent : SharedCommunicationsConsoleComponent, IActivate
    {
#pragma warning disable 649
        [Dependency] private IEntitySystemManager _entitySystemManager;
#pragma warning restore 649

        private BoundUserInterface _userInterface;
        private PowerReceiverComponent _powerReceiver;
        private bool Powered => _powerReceiver.Powered;
        private RoundEndSystem RoundEndSystem => _entitySystemManager.GetEntitySystem<RoundEndSystem>();

        public override void Initialize()
        {
            base.Initialize();

            _userInterface = Owner.GetComponent<ServerUserInterfaceComponent>().GetBoundUserInterface(CommunicationsConsoleUiKey.Key);
            _userInterface.OnReceiveMessage += UserInterfaceOnOnReceiveMessage;
            _powerReceiver = Owner.GetComponent<PowerReceiverComponent>();

            RoundEndSystem.OnRoundEndCountdownStarted += UpdateBoundInterface;
            RoundEndSystem.OnRoundEndCountdownCancelled += UpdateBoundInterface;
            RoundEndSystem.OnRoundEndCountdownFinished += UpdateBoundInterface;
        }

        private void UpdateBoundInterface()
        {
            _userInterface.SetState(new CommunicationsConsoleInterfaceState(RoundEndSystem.ExpectedCountdownEnd));
        }

        private void UserInterfaceOnOnReceiveMessage(ServerBoundUserInterfaceMessage obj)
        {
            switch (obj.Message)
            {
                case CommunicationsConsoleCallEmergencyShuttleMessage _:
                    RoundEndSystem.RequestRoundEnd();
                    break;

                case CommunicationsConsoleRecallEmergencyShuttleMessage _:
                    RoundEndSystem.CancelRoundEndCountdown();
                    break;
            }
        }

        public void OpenUserInterface(IPlayerSession session)
        {
            _userInterface.Open(session);
        }

        void IActivate.Activate(ActivateEventArgs eventArgs)
        {
            if (!eventArgs.User.TryGetComponent(out IActorComponent actor))
                return;

            if (!Powered)
            {
                return;
            }
            OpenUserInterface(actor.playerSession);
        }
    }
}
