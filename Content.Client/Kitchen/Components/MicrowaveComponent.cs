using Content.Shared.Kitchen.Components;
using Content.Shared.Sound;
using Robust.Shared.Audio;
using Robust.Shared.GameObjects;
using Robust.Shared.Serialization.Manager.Attributes;

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
