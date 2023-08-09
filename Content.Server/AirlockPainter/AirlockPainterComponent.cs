using Robust.Shared.Audio;

namespace Content.Server.AirlockPainter
{
    [RegisterComponent]
    public sealed class AirlockPainterComponent : Component
    {
        [DataField("spraySound")]
        public SoundSpecifier SpraySound = new SoundPathSpecifier("/Audio/Effects/spray2.ogg");

        [DataField("sprayTime")]
        public float SprayTime = 3.0f;

        [DataField("isSpraying")]
        public bool IsSpraying = false;

        public int Index = default!;
    }
}
