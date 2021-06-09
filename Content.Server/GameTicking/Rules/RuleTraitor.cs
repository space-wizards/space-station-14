using Content.Server.Chat.Managers;
using Content.Server.Players;
using Content.Server.Traitor;
using Robust.Server.Player;
using Robust.Shared.Audio;
using Robust.Shared.IoC;
using Robust.Shared.Localization;
using Robust.Shared.Player;

namespace Content.Server.GameTicking.Rules
{
    public class RuleTraitor : GameRule
    {
        [Dependency] private readonly IChatManager _chatManager = default!;

        public override void Added()
        {
            _chatManager.DispatchServerAnnouncement(Loc.GetString("Hello crew! Have a good shift!"));

            var filter = Filter.Empty()
                .AddWhere(session => ((IPlayerSession)session).ContentData()?.Mind?.HasRole<TraitorRole>() ?? false);
            SoundSystem.Play(filter, "/Audio/Misc/tatoralert.ogg", AudioParams.Default);
        }
    }
}
