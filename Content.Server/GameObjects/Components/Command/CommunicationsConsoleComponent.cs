#nullable enable
using System;
using System.Globalization;
using System.Threading;
using Content.Server.GameObjects.Components.PDA;
using Content.Server.GameObjects.Components.Power.ApcNetComponents;
using Content.Server.GameObjects.EntitySystems;
using Content.Server.Interfaces.Chat;
using Content.Server.Utility;
using Content.Shared.GameObjects.Components.Command;
using Content.Shared.Interfaces.GameObjects.Components;
using Robust.Server.GameObjects;
using Robust.Server.Player;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Timing;
using Robust.Shared.ViewVariables;
using Timer = Robust.Shared.Timing.Timer;

namespace Content.Server.GameObjects.Components.Command
{
    [RegisterComponent]
    [ComponentReference(typeof(IActivate))]
    public class CommunicationsConsoleComponent : SharedCommunicationsConsoleComponent, IActivate
    {
        [Dependency] private readonly IGameTiming _gameTiming = default!;
        [Dependency] private readonly IChatManager _chatManager = default!;
        private bool Powered => !Owner.TryGetComponent(out PowerReceiverComponent? receiver) || receiver.Powered;

        private RoundEndSystem RoundEndSystem => EntitySystem.Get<RoundEndSystem>();

        [ViewVariables] private BoundUserInterface? UserInterface => Owner.GetUIOrNull(CommunicationsConsoleUiKey.Key);

        public TimeSpan LastAnnounceTime { get; private set; } = TimeSpan.Zero;
        public TimeSpan AnnounceCooldown { get; } = TimeSpan.FromSeconds(90);
        private CancellationTokenSource _announceCooldownEndedTokenSource = new();

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

                UserInterface?.SetState(new CommunicationsConsoleInterfaceState(CanAnnounce(), system.CanCall(), system.ExpectedCountdownEnd));
            }
        }

        public bool CanAnnounce()
        {
            if (LastAnnounceTime == TimeSpan.Zero)
            {
                return true;
            }
            return _gameTiming.CurTime >= LastAnnounceTime + AnnounceCooldown;
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
                case CommunicationsConsoleAnnounceMessage msg:
                    if (!CanAnnounce())
                    {
                        return;
                    }
                    _announceCooldownEndedTokenSource.Cancel();
                    _announceCooldownEndedTokenSource = new CancellationTokenSource();
                    LastAnnounceTime = _gameTiming.CurTime;
                    Timer.Spawn(AnnounceCooldown, () => UpdateBoundInterface(), _announceCooldownEndedTokenSource.Token);
                    UpdateBoundInterface();

                    var message = msg.Message.Length <= 256 ? msg.Message.Trim() : $"{msg.Message.Trim().Substring(0, 256)}...";

                    var author = "Unknown";
                    var mob = obj.Session.AttachedEntity;
                    if (mob != null && mob.TryGetHeldId(out var id))
                    {
                        author = $"{id.FullName} ({CultureInfo.CurrentCulture.TextInfo.ToTitleCase(id.JobTitle ?? string.Empty)})".Trim();
                    }

                    message += $"\nSent by {author}";
                    _chatManager.DispatchStationAnnouncement(message, "Communications Console");
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
