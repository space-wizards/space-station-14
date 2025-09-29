using Content.Shared.Crayon;
using Robust.Shared.Audio;

namespace Content.Server.Crayon
{
    [RegisterComponent]
    public sealed partial class CrayonComponent : SharedCrayonComponent
    {
        /// <summary>
        /// Play a sound when using if specified
        /// </summary>
        [DataField]
        public SoundSpecifier? UseSound;

        /// <summary>
        /// Is the color valid for use
        /// </summary>
        [DataField]
        public bool SelectableColor;

        /// <summary>
        /// How many crayon usages are left
        /// </summary>
        public int Charges;

        /// <summary>
        /// Max number of charges
        /// </summary>
        [DataField]
        public int Capacity = 30;

        /// <summary>
        /// Should the crayon be deleted when all charges are consumed
        /// </summary>
        [DataField]
        public bool DeleteEmpty = true;

        /// <summary>
        /// Does the crayon use the battery power system to track charges
        /// </summary>
        [DataField]
        public bool BatteryPowered;
    }
}
