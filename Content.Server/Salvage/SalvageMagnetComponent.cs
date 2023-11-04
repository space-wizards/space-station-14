using Content.Shared.Radio;
using Content.Shared.Random;
using Content.Shared.Salvage;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Server.Salvage
{
    /// <summary>
    /// A salvage magnet.
    /// </summary>
    [NetworkedComponent, RegisterComponent]
    [Access(typeof(SalvageSystem))]
    public sealed partial class SalvageMagnetComponent : SharedSalvageMagnetComponent
    {
        /// <summary>
        /// Maximum distance from the offset position that will be used as a salvage's spawnpoint.
        /// </summary>
        [ViewVariables(VVAccess.ReadWrite)]
        [DataField("offsetRadiusMax")]
        public float OffsetRadiusMax = 32;

        /// <summary>
        /// The entity attached to the magnet
        /// </summary>
        [ViewVariables(VVAccess.ReadOnly)]
        [DataField("attachedEntity")]
        public EntityUid? AttachedEntity;

        /// <summary>
        /// Current state of this magnet
        /// </summary>
        [ViewVariables(VVAccess.ReadOnly)]
        [DataField("magnetState")]
        public MagnetState MagnetState = MagnetState.Inactive;

        /// <summary>
        /// How long it takes for the magnet to pull in the debris
        /// </summary>
        [ViewVariables(VVAccess.ReadWrite)]
        [DataField("baseAttachingTime")]
        public TimeSpan BaseAttachingTime = TimeSpan.FromSeconds(30);

        /// <summary>
        /// How long it actually takes for the magnet to pull in the debris
        /// </summary>
        [ViewVariables(VVAccess.ReadWrite)]
        [DataField("attachingTime")]
        public TimeSpan AttachingTime = TimeSpan.FromSeconds(30);

        /// <summary>
        /// How long the magnet can hold the debris until it starts losing the lock
        /// </summary>
        [ViewVariables(VVAccess.ReadWrite)]
        [DataField("holdTime")]
        public TimeSpan HoldTime = TimeSpan.FromSeconds(240);

        /// <summary>
        /// How long the magnet can hold the debris while losing the lock
        /// </summary>
        [ViewVariables(VVAccess.ReadWrite)]
        [DataField("detachingTime")]
        public TimeSpan DetachingTime = TimeSpan.FromSeconds(30);

        /// <summary>
        /// How long the magnet has to cool down for after use
        /// </summary>
        [ViewVariables(VVAccess.ReadWrite)]
        [DataField("baseCooldownTime")]
        public TimeSpan BaseCooldownTime = TimeSpan.FromSeconds(60);

        /// <summary>
        /// How long the magnet actually has to cool down for after use
        /// </summary>
        [ViewVariables(VVAccess.ReadWrite)]
        [DataField("cooldownTime")]
        public TimeSpan CooldownTime = TimeSpan.FromSeconds(60);

        [DataField("salvageChannel", customTypeSerializer: typeof(PrototypeIdSerializer<RadioChannelPrototype>))]
        public string SalvageChannel = "Supply";

        /// <summary>
        /// Current how much charge the magnet currently has
        /// </summary>
        [DataField("chargeRemaining")]
        public int ChargeRemaining = 5;

        /// <summary>
        /// How much capacity the magnet can hold
        /// </summary>
        [DataField("chargeCapacity")]
        public int ChargeCapacity = 5;

        /// <summary>
        /// Used as a guard to prevent spamming the appearance system
        /// </summary>
        [DataField("previousCharge")]
        public int PreviousCharge = 5;

        /// <summary>
        /// The chance that a random procgen asteroid will be
        /// generated rather than a static salvage prototype.
        /// </summary>
        [DataField("asteroidChance"), ViewVariables(VVAccess.ReadWrite)]
        public float AsteroidChance = 0.6f;

        /// <summary>
        /// A weighted random prototype corresponding to
        /// what asteroid entities will be generated.
        /// </summary>
        [DataField("asteroidPool", customTypeSerializer: typeof(PrototypeIdSerializer<WeightedRandomEntityPrototype>)), ViewVariables(VVAccess.ReadWrite)]
        public string AsteroidPool = "RandomAsteroidPool";
    }

    [CopyByRef, DataRecord]
    public record struct MagnetState(MagnetStateType StateType, TimeSpan Until)
    {
        public static readonly MagnetState Inactive = new (MagnetStateType.Inactive, TimeSpan.Zero);
    };

    public sealed class SalvageMagnetActivatedEvent : EntityEventArgs
    {
        public EntityUid Magnet;

        public SalvageMagnetActivatedEvent(EntityUid magnet)
        {
            Magnet = magnet;
        }
    }
    public enum MagnetStateType
    {
        Inactive,
        Attaching,
        Holding,
        Detaching,
        CoolingDown,
    }
}
