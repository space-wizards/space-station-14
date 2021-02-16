#nullable enable
using Content.Server.GameObjects.Components.Power.ApcNetComponents;
using Content.Server.GameObjects.EntitySystems;
using Content.Server.Utility;
using Content.Shared.GameObjects.Components.Command;
using Content.Shared.Interfaces.GameObjects.Components;
using Robust.Server.GameObjects;
using Robust.Server.Player;
using Robust.Shared.GameObjects;
using Robust.Shared.ViewVariables;

namespace Content.Server.GameObjects.Components.Command
{
    [RegisterComponent]
    [ComponentReference(typeof(IActivate))]
    public class CommunicationsConsoleComponent : SharedCommunicationsConsoleComponent, IActivate
    {
        private bool Powered => !Owner.TryGetComponent(out PowerReceiverComponent? receiver) || receiver.Powered;

        private RoundEndSystem RoundEndSystem => EntitySystem.Get<RoundEndSystem>();

        [ViewVariables] private BoundUserInterface? UserInterface => Owner.GetUIOrNull(CommunicationsConsoleUiKey.Key);

        public override void Initialize()
        {
            base.Initialize();

            if (UserInterface != null)
            {
                UserInterface.OnReceiveMessage += UserInterfaceOnOnReceiveMessage;
            }

            RoundEndSystem.OnRoundEndCountdownStarted += UpdateBoundInterface;
            RoundEndSystem.OnRoundEndCountdownCancelled += UpdateBoundInterface;
            RoundEndSystem.OnRoundEndCountdownFinished += UpdateBoundInterface;
            RoundEndSystem.OnCallCooldownEnded += UpdateBoundInterface;
        }

        protected override void Startup()
        {
            base.Startup();

            UpdateBoundInterface();
        }

        private void UpdateBoundInterface()
        {
            if (!Deleted)
            {
                var system = RoundEndSystem;

                UserInterface?.SetState(new CommunicationsConsoleInterfaceState(system.CanCall(), system.ExpectedCountdownEnd));
            }
        }

        public override void OnRemove()
        {
            RoundEndSystem.OnRoundEndCountdownStarted -= UpdateBoundInterface;
            RoundEndSystem.OnRoundEndCountdownCancelled -= UpdateBoundInterface;
            RoundEndSystem.OnRoundEndCountdownFinished -= UpdateBoundInterface;
            base.OnRemove();
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
            UserInterface?.Open(session);
        }

        void IActivate.Activate(ActivateEventArgs eventArgs)
        {
            if (!eventArgs.User.TryGetComponent(out IActorComponent? actor))
                return;
/*
            if (!Powered)
            {
                return;
            }
*/
            OpenUserInterface(actor.playerSession);
        }
    }
}
