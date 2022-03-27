using Content.Server.UserInterface;
using Content.Shared.AirlockPainter;
using Content.Shared.Sound;
using Robust.Server.GameObjects;

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

        [DataField("styles")]
        public List<string> Styles = default!;

        [DataField("index")]
        public int Index = default!;
    }
}
