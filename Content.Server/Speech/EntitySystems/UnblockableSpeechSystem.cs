using Content.Server.Chat.Systems;
using Content.Server.Speech.Components;

namespace Content.Server.Speech.EntitySystems
{
    public sealed class UnblockableSpeechSystem : EntitySystem
    {
        public override void Initialize()
        {
            SubscribeLocalEvent<UnblockableSpeechComponent, CheckIgnoreSpeechBlockerEvent>(OnCheck);
        }

        private void OnCheck(EntityUid uid, UnblockableSpeechComponent component, CheckIgnoreSpeechBlockerEvent args)
        {
            args.IgnoreBlocker = true;
        }
    }
}
