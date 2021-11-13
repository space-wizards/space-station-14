using Content.Server.Chat.Managers;
using Content.Server.Mind.Systems;
using Content.Server.Players;
using Content.Server.Traitor;
using Content.Shared.Sound;
using Robust.Server.Player;
using Robust.Shared.Audio;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Localization;
using Robust.Shared.Player;
using Robust.Shared.Serialization.Manager.Attributes;

namespace Content.Server.GameTicking.Rules
{
    public class RuleTraitor : GameRule
    {
        [Dependency] private readonly IChatManager _chatManager = default!;

        [DataField("addedSound")] private SoundSpecifier _addedSound = new SoundPathSpecifier("/Audio/Misc/tatoralert.ogg");

        public override void Added()
        {
            _chatManager.DispatchServerAnnouncement(Loc.GetString("rule-traitor-added-announcement"));

            var roleSys = EntitySystem.Get<RolesSystem>();
            var filter = Filter.Empty()
                .AddWhere(session =>
                {
                    var mind = ((IPlayerSession) session).ContentData()?.Mind;
                    return mind != null && roleSys.HasRole<TraitorRole>(mind);
                });

            SoundSystem.Play(filter, _addedSound.GetSound(), AudioParams.Default);
        }
    }
}
