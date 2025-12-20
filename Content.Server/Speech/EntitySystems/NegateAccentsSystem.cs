using Content.Server.Speech.Components;
using Content.Shared.Chat;
using Content.Shared.Speech;
using Content.Shared.StatusEffectNew;

namespace Content.Server.Speech.EntitySystems
{
    public sealed class NegateAccentsSystem : EntitySystem
    {
        public override void Initialize()
        {
            SubscribeLocalEvent<NegateAccentsComponent, TransformSpeechEvent>(OnTransformSpeech, before: [typeof(AccentSystem)]);
        }

        private void OnTransformSpeech(Entity<NegateAccentsComponent> entity, ref TransformSpeechEvent args)
        {
            if (entity.Comp.CancelAccent)
                args.Cancel();
        }
    }
}
