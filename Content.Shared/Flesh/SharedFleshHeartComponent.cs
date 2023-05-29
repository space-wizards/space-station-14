using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared.Flesh
{
    [NetworkedComponent()]
    [Virtual]
    public class SharedFleshHeartComponent : Component
    {
        /// <summary>
        /// The visual state that is set when the emitter doesn't have enough power.
        /// </summary>
        [DataField("finalState")]
        public string? FinalState = "underpowered";
    }
}
