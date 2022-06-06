using Robust.Shared.GameStates;

namespace Content.Shared.Eye.Blinding
{
    [RegisterComponent]
    [NetworkedComponent]
    public sealed class BlindableComponent : Component
    {
        /// <description>
        /// How many sources of blindness are affecting us?
        /// </description>
        [DataField("sources")]
        public int Sources = 0;
    }
}
