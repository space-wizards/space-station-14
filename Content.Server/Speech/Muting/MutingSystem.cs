using Content.Server.Popups;
using Content.Server.Speech.EntitySystems;
using Content.Shared.Abilities.Mime;
using Content.Shared.Chat;
using Content.Shared.Chat.Prototypes;
using Content.Shared.Puppet;
using Content.Shared.Speech;
using Content.Shared.Speech.Muting;
using Robust.Shared.Prototypes;

namespace Content.Server.Speech.Muting
{
    public sealed partial class MutingSystem : EntitySystem
    {
        [Dependency] private PopupSystem _popupSystem = default!;
        [Dependency] private IPrototypeManager _prototypeManager = default!;

        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<MutedComponent, SpeakAttemptEvent>(OnSpeakAttempt);
            SubscribeLocalEvent<MutedComponent, EmoteEvent>(OnEmote, before: new[] { typeof(VocalSystem), typeof(MumbleAccentSystem) });
            SubscribeLocalEvent<MutedComponent, EmoteActionEvent>(OnEmoteAction, before: new[] { typeof(VocalSystem) });
        }

        private void OnEmote(EntityUid uid, MutedComponent component, ref EmoteEvent args)
        {
            if (args.Handled)
                return;

            //still leaves the text so it looks like they are pantomiming a laugh
            if (args.Emote.Category.HasFlag(EmoteCategory.Vocal))
                args.Handled = true;
        }

        private void OnEmoteAction(EntityUid uid, MutedComponent component, EmoteActionEvent args)
        {
            if (args.Handled)
                return;

            if (!_prototypeManager.Resolve(args.Emote, out var emote))
                return;

            if (!emote.Category.HasFlag(EmoteCategory.Vocal))
                return;

            if (HasComp<MimePowersComponent>(uid))
                _popupSystem.PopupEntity(Loc.GetString("mime-cant-speak"), uid, uid);

            else
                _popupSystem.PopupEntity(Loc.GetString("speech-muted"), uid, uid);
            args.Handled = true;
        }


        private void OnSpeakAttempt(EntityUid uid, MutedComponent component, SpeakAttemptEvent args)
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
