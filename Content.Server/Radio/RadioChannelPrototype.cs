using Robust.Shared.Maths;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.Manager.Attributes;

namespace Content.Server.Radio
{
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
