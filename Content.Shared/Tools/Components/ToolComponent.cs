using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Utility;

namespace Content.Shared.Tools.Components
{
    [RegisterComponent, NetworkedComponent] // TODO move tool system to shared, and make it a friend.
    public sealed class ToolComponent : Component
    {
        [DataField("qualities")]
        public PrototypeFlags<ToolQualityPrototype> Qualities { get; set; } = new();

        /// <summary>
        ///     For tool interactions that have a delay before action this will modify the rate, time to wait is divided by this value
        /// </summary>
        [ViewVariables(VVAccess.ReadWrite)]
        [DataField("speed")]
        public float SpeedModifier { get; set; } = 1;

        [DataField("useSound")]
        public SoundSpecifier? UseSound { get; set; }
    }
}
