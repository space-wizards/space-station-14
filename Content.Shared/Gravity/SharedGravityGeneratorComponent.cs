using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared.Gravity
{
    [NetworkedComponent()]
    [Virtual]
    public partial class SharedGravityGeneratorComponent : Component
    {
        /// <summary>
        /// A map of the sprites used by the gravity generator given its status.
        /// </summary>
        [DataField("spriteMap")]
        [Access(typeof(SharedGravitySystem))]
        public Dictionary<GravityGeneratorStatus, string> SpriteMap = new();

        /// <summary>
        /// The sprite used by the core of the gravity generator when the gravity generator is starting up.
        /// </summary>
        [DataField("coreStartupState")]
        [ViewVariables(VVAccess.ReadWrite)]
        public string CoreStartupState = "startup";

        /// <summary>
        /// The sprite used by the core of the gravity generator when the gravity generator is idle.
        /// </summary>
        [DataField("coreIdleState")]
        [ViewVariables(VVAccess.ReadWrite)]
        public string CoreIdleState = "idle";

        /// <summary>
        /// The sprite used by the core of the gravity generator when the gravity generator is activating.
        /// </summary>
        [DataField("coreActivatingState")]
        [ViewVariables(VVAccess.ReadWrite)]
        public string CoreActivatingState = "activating";

        /// <summary>
        /// The sprite used by the core of the gravity generator when the gravity generator is active.
        /// </summary>
        [DataField("coreActivatedState")]
        [ViewVariables(VVAccess.ReadWrite)]
        public string CoreActivatedState = "activated";

        /// <summary>
        ///     Sent to the server to set whether the generator should be on or off
        /// </summary>
        [Serializable, NetSerializable]
        public sealed class SwitchGeneratorMessage : BoundUserInterfaceMessage
        {
            public bool On;

            public SwitchGeneratorMessage(bool on)
            {
                On = on;
            }
        }

        [Serializable, NetSerializable]
        public sealed class GeneratorState : BoundUserInterfaceState
        {
            public bool On;
            // 0 -> 255
            public byte Charge;
            public GravityGeneratorPowerStatus PowerStatus;
            public short PowerDraw;
            public short PowerDrawMax;
            public short EtaSeconds;

            public GeneratorState(
                bool on,
                byte charge,
                GravityGeneratorPowerStatus powerStatus,
                short powerDraw,
                short powerDrawMax,
                short etaSeconds)
            {
                On = on;
                Charge = charge;
                PowerStatus = powerStatus;
                PowerDraw = powerDraw;
                PowerDrawMax = powerDrawMax;
                EtaSeconds = etaSeconds;
            }
        }

        [Serializable, NetSerializable]
        public enum GravityGeneratorUiKey
        {
            Key
        }
    }

    [Serializable, NetSerializable]
    public enum GravityGeneratorVisuals
    {
        State,
        Charge
    }

    [Serializable, NetSerializable]
    public enum GravityGeneratorStatus
    {
        Broken,
        Unpowered,
        Off,
        On
    }

    [Serializable, NetSerializable]
    public enum GravityGeneratorPowerStatus : byte
    {
        Off,
        Discharging,
        Charging,
        FullyCharged
    }
}
