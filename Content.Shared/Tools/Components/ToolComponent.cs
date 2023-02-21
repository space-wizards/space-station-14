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

    public sealed class ToolEventData
    {
        public readonly Object? Ev;
        public readonly Object? CancelledEv;
        public readonly float Fuel;
        public readonly EntityUid? TargetEntity;

        public ToolEventData(Object? ev, float fuel = 0f, Object? cancelledEv = null, EntityUid? targetEntity = null)
        {
            Ev = ev;
            CancelledEv = cancelledEv;
            Fuel = fuel;
            TargetEntity = targetEntity;
        }
    }
}
