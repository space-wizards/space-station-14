using System;
using System.Globalization;
using System.Threading;
using Content.Server.Chat.Managers;
using Content.Server.PDA;
using Content.Server.Power.Components;
using Content.Server.RoundEnd;
using Content.Server.UserInterface;
using Content.Shared.Communications;
using Robust.Server.GameObjects;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Timing;
using Robust.Shared.ViewVariables;
using Timer = Robust.Shared.Timing.Timer;

namespace Content.Server.Communications
{
    [RegisterComponent]
    public class CommunicationsConsoleComponent : SharedCommunicationsConsoleComponent
    {
        [Dependency] private readonly IGameTiming _gameTiming = default!;
        [Dependency] private readonly IChatManager _chatManager = default!;
        [Dependency] private readonly IEntityManager _entities = default!;

        private bool Powered => !_entities.TryGetComponent(Owner, out ApcPowerReceiverComponent? receiver) || receiver.Powered;

        private RoundEndSystem RoundEndSystem => EntitySystem.Get<RoundEndSystem>();

        [ViewVariables] private BoundUserInterface? UserInterface => Owner.GetUIOrNull(CommunicationsConsoleUiKey.Key);

        public TimeSpan LastAnnounceTime { get; private set; } = TimeSpan.Zero;
        public TimeSpan AnnounceCooldown { get; } = TimeSpan.FromSeconds(90);
        private CancellationTokenSource _announceCooldownEndedTokenSource = new();

        protected override void Initialize()
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

        protected override void OnRemove()
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
                    RoundEndSystem.RequestRoundEnd(obj.Session.AttachedEntity);
                    break;

                case CommunicationsConsoleRecallEmergencyShuttleMessage _:
                    RoundEndSystem.CancelRoundEndCountdown(obj.Session.AttachedEntity);
                    break;
                case CommunicationsConsoleAnnounceMessage msg:
                    if (!CanAnnounce())
                    {
                        return;
                    }
                    _announceCooldownEndedTokenSource.Cancel();
                    _announceCooldownEndedTokenSource = new CancellationTokenSource();
                    LastAnnounceTime = _gameTiming.CurTime;
                    Timer.Spawn(AnnounceCooldown, UpdateBoundInterface, _announceCooldownEndedTokenSource.Token);
                    UpdateBoundInterface();

                    var message = msg.Message.Length <= 256 ? msg.Message.Trim() : $"{msg.Message.Trim().Substring(0, 256)}...";

                    var author = "Unknown";
                    if (obj.Session.AttachedEntity is {Valid: true} mob && mob.TryGetHeldId(out var id))
                    {
                        author = $"{id.FullName} ({CultureInfo.CurrentCulture.TextInfo.ToTitleCase(id.JobTitle ?? string.Empty)})".Trim();
                    }

                    message += $"\nSent by {author}";
                    _chatManager.DispatchStationAnnouncement(message, "Communications Console");
                    break;
            }
        }
    }
}
