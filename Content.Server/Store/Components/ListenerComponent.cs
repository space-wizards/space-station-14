using Robust.Shared.GameObjects;

namespace Content.Shared.Components
{
    [RegisterComponent]
    public sealed class ListenerComponent : Component
    {

        /// <summary>
        /// The range within which this component can listen for audio.
        /// </summary>
        [DataField("range")]
        public float ListenRange = 10.0f;

        /// <summary>
        /// A list of codewords this component should listen for.
        /// </summary>
        [DataField("codewords")]
        public List<string> Codewords = new();
    }
}
