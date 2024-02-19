using Content.Server.Abilities.Mime;
using Content.Server.Chat.Systems;
using Content.Server.Popups;
using Content.Server.Speech.Components;
using Content.Server.Speech.EntitySystems;
using Content.Shared.Chat.Prototypes;
using Content.Shared.Puppet;
using Content.Shared.Speech;

namespace Content.Server.Speech.Muting
{
    public sealed class MutingSystem : EntitySystem
    {
        [Dependency] private readonly PopupSystem _popupSystem = default!;
        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<Shared.Speech.Muting.MutedComponent, SpeakAttemptEvent>(OnSpeakAttempt);
            SubscribeLocalEvent<Shared.Speech.Muting.MutedComponent, EmoteEvent>(OnEmote, before: new[] { typeof(VocalSystem) });
            SubscribeLocalEvent<Shared.Speech.Muting.MutedComponent, ScreamActionEvent>(OnScreamAction, before: new[] { typeof(VocalSystem) });
        }

        private void OnEmote(EntityUid uid, Shared.Speech.Muting.MutedComponent component, ref EmoteEvent args)
        {
            if (args.Handled)
                return;

            //still leaves the text so it looks like they are pantomiming a laugh
            if (args.Emote.Category.HasFlag(EmoteCategory.Vocal))
                args.Handled = true;
        }

        private void OnScreamAction(EntityUid uid, Shared.Speech.Muting.MutedComponent component, ScreamActionEvent args)
        {
            if (args.Handled)
                return;

            if (HasComp<MimePowersComponent>(uid))
                _popupSystem.PopupEntity(Loc.GetString("mime-cant-speak"), uid, uid);

            else
                _popupSystem.PopupEntity(Loc.GetString("speech-muted"), uid, uid);
            args.Handled = true;
        }


        private void OnSpeakAttempt(EntityUid uid, Shared.Speech.Muting.MutedComponent component, SpeakAttemptEvent args)
        {
            // TODO something better than this.

            if (HasComp<MimePowersComponent>(uid))
                _popupSystem.PopupEntity(Loc.GetString("mime-cant-speak"), uid, uid);
            else if (HasComp<VentriloquistPuppetComponent>(uid))
                _popupSystem.PopupEntity(Loc.GetString("ventriloquist-puppet-cant-speak"), uid, uid);
            else
                _popupSystem.PopupEntity(Loc.GetString("speech-muted"), uid, uid);

            args.Cancel();
        }
    }
}
