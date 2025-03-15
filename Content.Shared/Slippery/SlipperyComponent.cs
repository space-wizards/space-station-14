using Content.Shared.StepTrigger.Components;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;

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
        /// How many seconds the mob will be paralyzed for.
        /// </summary>
        [DataField, AutoNetworkedField]
        public float ParalyzeTime = 1.5f;

        /// <summary>
        /// The entity's speed will be multiplied by this to slip it forwards.
        /// </summary>
        [DataField, AutoNetworkedField]
        public float LaunchForwardsMultiplier = 1.5f;

        /// <summary>
        /// If this is true, any slipping entity loses its friction until
        /// it's not colliding with any SuperSlippery entities anymore.
        /// They also will fail any attempts to stand up unless they have no-slips.
        /// </summary>
        [DataField, AutoNetworkedField]
        public bool SuperSlippery;
    }
    /// <summary>
    /// This data definition only exists for slippery reagents to work without either it being jank or organized like shit.
    /// SlipperyComponent should use this for its data structure but a lot of things would break and I'm already deep in merge conflict hell.
    /// </summary>
    [DataDefinition]
    public sealed partial class SlipperyEffectEntry
    {
        /// <summary>
        /// How many seconds the mob will be paralyzed for.
        /// </summary>
        [DataField]
        public float ParalyzeTime = 1.5f;

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
    }
}
