using Content.Server.Abilities.Mime;
using Content.Server.Chat.Systems;
using Content.Server.Popups;
using Content.Server.Speech.EntitySystems;
using Content.Shared.Chat.Prototypes;
using Content.Shared.Puppet;
using Content.Shared.Speech;
using Content.Shared.Speech.Muting;
using Content.Shared._Harmony.Speech.Hypophonia;

namespace Content.Server._Harmony.Speech.Hypophonia
{
    public sealed class HypophoniaSystem : EntitySystem
    {
        [Dependency] private readonly PopupSystem _popupSystem = default!;
        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<HypophoniaComponent, SpeakAttemptEvent>(OnSpeakAttempt);
            SubscribeLocalEvent<HypophoniaComponent, EmoteEvent>(OnEmote, before: new[] { typeof(VocalSystem) });
            SubscribeLocalEvent<HypophoniaComponent, ScreamActionEvent>(OnScreamAction, before: new[] { typeof(VocalSystem) });
        }

        private void OnEmote(EntityUid uid, HypophoniaComponent component, ref EmoteEvent args)
        {
            if (args.Handled)
                return;

            // Let MutingSystem handle the event for muted characters (mimes included)
            if (HasComp<MutedComponent>(uid))
                return;

            //still leaves the text so it looks like they are pantomiming a laugh
            if (args.Emote.Category.HasFlag(EmoteCategory.Vocal))
                args.Handled = true;
        }

        private void OnScreamAction(EntityUid uid, HypophoniaComponent component, ScreamActionEvent args)
        {
            if (args.Handled)
                return;

            // Let MutingSystem handle the event muted characters (mimes included)
            if (HasComp<MutedComponent>(uid))
                return;

            // Mark the event as handled and show the popup
            _popupSystem.PopupEntity(Loc.GetString("speech-hypophonia"), uid, uid);
            args.Handled = true;
        }


        private void OnSpeakAttempt(EntityUid uid, HypophoniaComponent component, SpeakAttemptEvent args)
        {
            // Let MutingSystem handle the event for puppets and muted characters (mimes included)
            if (HasComp<VentriloquistPuppetComponent>(uid) || HasComp<MutedComponent>(uid))
                return;

            // If the entity is whispering, let them speak
            if (args.Whisper)
                return;

            // Cancel the event and show the popup
            _popupSystem.PopupEntity(Loc.GetString("speech-hypophonia"), uid, uid);
            args.Cancel();
        }
    }
}
