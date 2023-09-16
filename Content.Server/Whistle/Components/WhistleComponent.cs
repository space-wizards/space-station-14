using Robust.Shared.Audio;

namespace Content.Server.Whistle.Components
{
    [RegisterComponent]
    public sealed partial class WhistleComponent : Component
    {
        [DataField("effect")]
        public string? effect = "WhistleExclamation";

        [DataField("sound", required: true)]
        public SoundSpecifier Sound = default!;

        [DataField("distance")]
        public float Distance = 0;
    }
}
