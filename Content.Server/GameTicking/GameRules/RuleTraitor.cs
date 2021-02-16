using Content.Server.Interfaces.Chat;
using Content.Server.Mobs.Roles.Traitor;
using Content.Server.Players;
using Robust.Server.GameObjects;
using Robust.Server.Player;
using Robust.Shared.Audio;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Localization;

namespace Content.Server.GameTicking.GameRules
{
    public class RuleTraitor : GameRule
    {
        [Dependency] private readonly IChatManager _chatManager = default!;

        public override void Added()
        {
            _chatManager.DispatchServerAnnouncement(Loc.GetString("Hello crew! Have a good shift!"));

            bool Predicate(IPlayerSession session) => session.ContentData()?.Mind?.HasRole<TraitorRole>() ?? false;

            EntitySystem.Get<AudioSystem>().PlayGlobal("/Audio/Misc/tatoralert.ogg", AudioParams.Default, Predicate);
        }
    }
}
