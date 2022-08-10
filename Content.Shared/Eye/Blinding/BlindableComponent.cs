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

        /// <description>
        /// Used to ensure that this doesn't break with sandbox or admin tools.
        /// This is not "enabled/disabled".
        /// </description>
        public bool LightSetup = false;

        /// <description>
        /// Gives an extra frame of blindness to reenable light manager during
        /// </description>
        public bool GraceFrame = false;
    }
}
