using System;
using Content.Client.Kitchen.Components;
using Robust.Shared.Audio;
using Robust.Shared.GameObjects;
using Robust.Shared.Player;

namespace Content.Client.Kitchen.EntitySystems
{
    public class MicrowaveSystem : EntitySystem
    {
        public void StartSoundLoop(MicrowaveComponent microwave)
        {
            StopSoundLoop(microwave);

            microwave.PlayingStream = SoundSystem.Play(Filter.Local(), microwave.LoopingSound.GetSound(), microwave.Owner,
                AudioParams.Default.WithAttenuation(1).WithMaxDistance(5).WithLoop(true));
        }

        public void StopSoundLoop(MicrowaveComponent microwave)
        {
            try
            {
                microwave.PlayingStream?.Stop();
            }
            catch (Exception _)
            {
                // TODO: HOLY SHIT EXPOSE SOME DISPOSED PROPERTY ON PLAYING STREAM OR SOMETHING.
            }
        }
    }
}
