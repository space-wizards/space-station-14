using Content.Shared.Kitchen.Components;
using Robust.Shared.Audio;

namespace Content.Client.Kitchen.Components
{
    [RegisterComponent]
    public sealed class MicrowaveComponent : SharedMicrowaveComponent
    {
        public IPlayingAudioStream? PlayingStream { get; set; }

        [DataField("loopingSound")]
        public SoundSpecifier LoopingSound = new SoundPathSpecifier("/Audio/Machines/microwave_loop.ogg");
    }
}
