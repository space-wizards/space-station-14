using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared.Gravity
{
    [NetworkedComponent()]
    [Virtual]
    public class SharedGravityGeneratorComponent : Component
    {
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
