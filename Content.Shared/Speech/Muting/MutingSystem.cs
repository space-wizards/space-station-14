using Content.Shared.Popups;
using Robust.Shared.Player;

namespace Content.Shared.Speech.Muting
{
    public sealed class MutingSystem : EntitySystem
    {
        [Dependency] private readonly SharedPopupSystem _popupSystem = default!;
        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<MutedComponent, SpeakAttemptEvent>(OnSpeakAttempt);
        }

        private void OnSpeakAttempt(EntityUid uid, MutedComponent component, SpeakAttemptEvent args)
        {
            _popupSystem.PopupEntity(Loc.GetString("speech-muted"), uid, uid);
            args.Cancel();
        }
    }
}
