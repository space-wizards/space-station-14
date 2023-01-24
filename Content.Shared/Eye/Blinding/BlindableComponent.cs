using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

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

        /// <summary>
        /// How many seconds will be subtracted from each attempt to add blindness to us?
        /// </summary>
        [DataField("blindResistance")]
        public float BlindResistance = 0;

        /// <summary>
        /// Replace with actual eye damage after bobby I guess
        /// </summary>
        [ViewVariables]
        public int EyeDamage = 0;

        /// <summary>
        /// Whether eye damage has accumulated enough to blind them.
        /// </summary>
        [ViewVariables]
        public bool EyeTooDamaged = false;

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

    [Serializable, NetSerializable]
    public sealed class BlindableComponentState : ComponentState
    {
        public readonly int Sources;

        public BlindableComponentState(int sources)
        {
            Sources = sources;
        }
    }
}
