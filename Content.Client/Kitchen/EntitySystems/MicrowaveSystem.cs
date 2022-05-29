using System;
using Content.Client.Kitchen.Components;
using Robust.Shared.Audio;
using Robust.Shared.GameObjects;
using Robust.Shared.Player;

namespace Content.Client.Kitchen.EntitySystems
{
    public sealed class MicrowaveSystem : EntitySystem
    {
        public void StartSoundLoop(MicrowaveComponent microwave)
        {
            StopSoundLoop(microwave);

            microwave.PlayingStream = SoundSystem.Play(Filter.Local(), microwave.LoopingSound.GetSound(), microwave.Owner,
                AudioParams.Default.WithMaxDistance(5).WithLoop(true));
        }

        public void StopSoundLoop(MicrowaveComponent microwave)
        {
            microwave.PlayingStream?.Stop();
        }
    }
}
