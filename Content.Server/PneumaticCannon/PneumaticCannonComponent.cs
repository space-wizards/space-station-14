using Content.Shared.Tools;
using Robust.Shared.Audio;
using Robust.Shared.Containers;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Server.PneumaticCannon
{
    // TODO: ideally, this and most of the actual firing code doesn't need to exist, and guns can be flexible enough
    // to handle shooting things that aren't ammo (just firing any entity)
    [RegisterComponent, Access(typeof(PneumaticCannonSystem))]
    public sealed class PneumaticCannonComponent : Component
    {
        [ViewVariables]
        public ContainerSlot GasTankSlot = default!;

        [ViewVariables(VVAccess.ReadWrite)]
        public PneumaticCannonPower Power = PneumaticCannonPower.Low;

        [ViewVariables(VVAccess.ReadWrite)]
        public PneumaticCannonFireMode Mode = PneumaticCannonFireMode.Single;

        /// <summary>
        ///     Used to fire the pneumatic cannon in intervals rather than all at the same time
        /// </summary>
        public float AccumulatedFrametime;

        public Queue<FireData> FireQueue = new();

        [DataField("fireInterval")]
        public float FireInterval = 0.1f;

        /// <summary>
        ///     Whether the pneumatic cannon should instantly fire once, or whether it should wait for the
        ///     fire interval initially.
        /// </summary>
        [DataField("instantFire")]
        public bool InstantFire = true;

        [DataField("toolModifyPower", customTypeSerializer:typeof(PrototypeIdSerializer<ToolQualityPrototype>))]
        public string ToolModifyPower = "Welding";

        [DataField("toolModifyMode", customTypeSerializer:typeof(PrototypeIdSerializer<ToolQualityPrototype>))]
        public string ToolModifyMode = "Screwing";

        /// <remarks>
        ///     If this value is too high it just straight up stops working for some reason
        /// </remarks>
        [DataField("throwStrength")]
        [ViewVariables(VVAccess.ReadWrite)]
        public float ThrowStrength = 20.0f;

        [DataField("baseThrowRange")]
        [ViewVariables(VVAccess.ReadWrite)]
        public float BaseThrowRange = 8.0f;

        /// <summary>
        ///     How long to stun for if they shoot the pneumatic cannon at high power.
        /// </summary>
        [DataField("highPowerStunTime")]
        [ViewVariables(VVAccess.ReadWrite)]
        public float HighPowerStunTime = 3.0f;

        [DataField("gasTankRequired")]
        [ViewVariables(VVAccess.ReadWrite)]
        public bool GasTankRequired = true;

        [DataField("fireSound")]
        [ViewVariables(VVAccess.ReadWrite)]
        public SoundSpecifier FireSound = new SoundPathSpecifier("/Audio/Effects/thunk.ogg");

        public struct FireData
        {
            public EntityUid User;
            public float Strength;
            public Vector2 Direction;
        }
    }

    /// <summary>
    ///     How strong the pneumatic cannon should be.
    ///     Each tier throws items farther and with more speed, but has drawbacks.
    ///     The highest power knocks the player down for a considerable amount of time.
    /// </summary>
    public enum PneumaticCannonPower : byte
    {
        Low = 0,
        Medium = 1,
        High = 2,
        Len = 3 // used for length calc
    }

    /// <summary>
    ///     Whether to shoot one random item at a time, or all items at the same time.
    /// </summary>
    public enum PneumaticCannonFireMode : byte
    {
        Single = 0,
        All = 1,
        Len = 2 // used for length calc
    }
}
