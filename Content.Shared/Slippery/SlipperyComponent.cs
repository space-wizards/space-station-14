using Content.Shared.StepTrigger.Components;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared.Slippery
{
    /// <summary>
    /// Causes somebody to slip when they walk over this entity.
    /// </summary>
    /// <remarks>
    /// Requires <see cref="StepTriggerComponent"/>, see that component for some additional properties.
    /// </remarks>
    [RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
    public sealed partial class SlipperyComponent : Component
    {
        /// <summary>
        /// Path to the sound to be played when a mob slips.
        /// </summary>
        [DataField, AutoNetworkedField]
        [Access(Other = AccessPermissions.ReadWriteExecute)]
        public SoundSpecifier SlipSound = new SoundPathSpecifier("/Audio/Effects/slip.ogg");

        /// <summary>
        /// Should this component's friction factor into sliding friction?
        /// </summary>
        [DataField, AutoNetworkedField]
        public bool AffectsSliding;

        /// <summary>
        /// How long should this component apply the FrictionStatusComponent?
        /// Note: This does stack with SlidingComponent since they are two separate Components
        /// </summary>
        [DataField, AutoNetworkedField]
        public TimeSpan FrictionStatusTime = TimeSpan.FromSeconds(0.5f);

        /// <summary>
        /// How much stamina damage should this component do on slip?
        /// </summary>
        [DataField, AutoNetworkedField]
        public float StaminaDamage = 25f;

        /// <summary>
        /// Loads the data needed to determine how slippery something is.
        /// </summary>
        [DataField, AutoNetworkedField]
        public SlipperyEffectEntry SlipData = new();
    }
    /// <summary>
    /// Stores the data for slipperiness that way reagents and this component can use it.
    /// </summary>
    [DataDefinition, Serializable, NetSerializable]
    public sealed partial class SlipperyEffectEntry
    {
        /// <summary>
        /// How many seconds the mob will be stunned for.
        /// </summary>
        [DataField]
        public TimeSpan StunTime = TimeSpan.FromSeconds(0.5);

        /// <summary>
        /// How many seconds the mob will be knocked down for.
        /// </summary>
        [DataField]
        public TimeSpan KnockdownTime = TimeSpan.FromSeconds(1.5);

        /// <summary>
        /// Should the slipped entity try to stand up when Knockdown ends?
        /// </summary>
        [DataField]
        public bool AutoStand = true;

        /// <summary>
        /// The entity's speed will be multiplied by this to slip it forwards.
        /// </summary>
        [DataField]
        public float LaunchForwardsMultiplier = 1.5f;

        /// <summary>
        /// Minimum speed entity must be moving to slip.
        /// </summary>
        [DataField]
        public float RequiredSlipSpeed = 3.5f;

        /// <summary>
        /// If this is true, any slipping entity loses its friction until
        /// it's not colliding with any SuperSlippery entities anymore.
        /// They also will fail any attempts to stand up unless they have no-slips.
        /// </summary>
        [DataField]
        public bool SuperSlippery;

        /// <summary>
        /// This is used to store the friction modifier that is used on a sliding entity.
        /// </summary>
        [DataField]
        public float SlipFriction = 0.5f;
    }
}
