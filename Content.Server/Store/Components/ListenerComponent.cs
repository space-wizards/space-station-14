
using Robust.Shared.GameObjects;

namespace Content.Server.Store.Components
{
    [RegisterComponent]
    public partial class ListenerComponent : Component
    {
        /// <summary>
        /// The range within which this component can listen for audio or messages.
        /// </summary>
        [DataField("range")]
        public float ListenRange = 5.0f;  // Default range (you can modify it)

        /// <summary>
        /// A list of codewords this component should listen for.
        /// </summary>
        [DataField("codewords")]
        public List<string> Codewords = new();  // Codewords to trigger actions
    }
}
