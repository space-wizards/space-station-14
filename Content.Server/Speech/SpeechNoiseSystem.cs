using Robust.Shared.GameObjects;
using Robust.Shared.Audio;
using Content.Server.Chat;
using Content.Shared.Speech;
using Content.Shared.Sound;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;

namespace Content.Server.Speech
{
    public sealed class SpeechSoundSystem : EntitySystem
    {

        [Dependency] private readonly IPrototypeManager _proto = default!;

        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<SharedSpeechComponent, EntitySpokeEvent>(OnEntitySpoke);
            SubscribeLocalEvent<SharedSpeechComponent, ComponentStartup>(OnStartup);
        }

        private void OnStartup(EntityUid uid, SharedSpeechComponent component, ComponentStartup args)
        {
            //Cache prototype on component startup.
            if (_proto.TryIndex(component.SpeechSoundsId, out SpeechSoundsPrototype? speechsnds))
            {
                if (speechsnds != null)
                {
                    component.SpeechSoundsCache = speechsnds;
                }
            }
        }


        private void OnEntitySpoke(EntityUid uid, SharedSpeechComponent component, EntitySpokeEvent args)
        {
            if (!component.PlaySpeechSound) return;

            //Play speech sound

            if (component.SpeechSoundsCache != null)
            {
                //Re-Index sounds prototype if cached proto ID is outdated, allows VV and changing the voice.
                if (component.SpeechSoundsCache.ID != component.SpeechSoundsId)
                {
                    if (_proto.TryIndex(component.SpeechSoundsId, out SpeechSoundsPrototype? speechsnds))
                    {
                        if (speechsnds != null)
                        {
                            component.SpeechSoundsCache = speechsnds;
                        }
                    }
                }
                var contextSound = component.SpeechSoundsCache.SaySound;
                //Different sounds for ask/exclaim based on last character
                switch (args.Message[args.Message.Length-1])
                {
                    case '?':
                        contextSound = component.SpeechSoundsCache.AskSound;
                        break;
                    case '!':
                        contextSound = component.SpeechSoundsCache.ExclaimSound;
                        break;

                }
                SoundSystem.Play(Filter.Pvs(uid), contextSound.GetSound(), uid, component.AudioParams);
            }
        }

    }
}
