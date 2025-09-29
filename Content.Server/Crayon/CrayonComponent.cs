using Content.Shared.Crayon;
using Robust.Shared.Audio;

namespace Content.Server.Crayon
{
    [RegisterComponent]
    public sealed partial class CrayonComponent : SharedCrayonComponent
    {
        [DataField]
        public SoundSpecifier? UseSound;

        [DataField]
        public bool SelectableColor;

        public int Charges { get; set; }

        [DataField]
        public int Capacity { get; set; } = 30;

        [DataField]
        public bool DeleteEmpty = true;

        /// <summary>
        /// Does the crayon use the battery power system to track charges
        /// </summary>
        [DataField]
        public bool BatteryPowered;
    }
}
