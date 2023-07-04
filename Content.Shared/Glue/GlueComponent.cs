using Robust.Shared.Audio;
using Robust.Shared.GameStates;

namespace Content.Shared.Glue
{
    [RegisterComponent, NetworkedComponent]
    public sealed class GlueComponent : Component
    {
        /// <summary>
        /// Noise made when glue applied.
        /// </summary>
        [DataField("squeeze")]
        public SoundSpecifier Squeeze = new SoundPathSpecifier("/Audio/Items/squeezebottle.ogg");
    }
}
