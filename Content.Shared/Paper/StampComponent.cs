using Robust.Shared.Serialization;

namespace Content.Shared.Paper
{
    [Serializable, NetSerializable]
    public struct StampInfo
    {
        public string StampName;
        public Color StampColor;
    };

    [RegisterComponent]
    public sealed class StampComponent : Component
    {
        /// <summary>
        ///     The loc string name that will be stamped to the piece of paper on examine.
        /// </summary>
        [DataField("stampedName")]
        public string StampedName { get; set; } = "stamp-component-stamped-name-default";
        /// <summary>
        ///     Tne sprite state of the stamp to display on the paper from bureacracy.rsi.
        /// </summary>
        [DataField("stampState")]
        public string StampState { get; set; } = "paper_stamp-generic";
        /// <summary>
        /// The color of the ink used by the stamp in UIs
        /// </summary>
        [DataField("stampedColor")]
        public Color StampedColor { get; set; } = Color.FromHex("#BB3232"); // StyleNano.DangerousRedFore
    }
}
