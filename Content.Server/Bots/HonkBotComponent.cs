using Content.Shared.Sound;

namespace Content.Server.Bots
{
    [RegisterComponent]
    public sealed class HonkBotComponent : Component
    {
        [DataField("honkSound")]
        public SoundSpecifier HonkSound = new SoundPathSpecifier("/Audio/Items/bikehorn.ogg");

        public float Accumulator = 0f;

        public float HonkRollInterval = 2f;
    }
}
