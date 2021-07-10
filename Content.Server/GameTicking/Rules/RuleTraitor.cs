using Content.Server.Chat.Managers;
using Content.Server.Players;
using Content.Server.Traitor;
using Content.Shared.Sound;
using Robust.Server.Player;
using Robust.Shared.Audio;
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

            var filter = Filter.Empty()
                .AddWhere(session => ((IPlayerSession)session).ContentData()?.Mind?.HasRole<TraitorRole>() ?? false);

            if(_addedSound.TryGetSound(out var addedSound))
                SoundSystem.Play(filter, addedSound, AudioParams.Default);
        }
    }
}
