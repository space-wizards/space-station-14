using Robust.Shared.Maths;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.Manager.Attributes;

namespace Content.Shared.Radio
{
    /// <summary>
    ///     Used to define a static radio channel with a certain name, color, frequency, etc..
    ///     The client uses this to determine what the color and prefix for a certain chat message should be.
    /// </summary>
    [Prototype("radioChannel")]
    public class RadioChannelPrototype : IPrototype
    {
        /// <summary>
        ///     Also used as the name for the channel when shown in chat.
        /// </summary>
        [DataField("id", required: true)]
        public string ID { get;  } = default!;

        [DataField("secure")]
        public bool Secure;

        [DataField("channel")]
        public int Channel;

        [DataField("color")]
        public Color Color;
    }
}
