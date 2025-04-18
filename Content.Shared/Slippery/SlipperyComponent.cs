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
        public const float DefaultParalyzeTime = 1.5f;
        public const float DefaultLaunchForwardsMultiplier = 1.5f;
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
        [Access(Other = AccessPermissions.ReadWrite)]
        public float ParalyzeTime = DefaultParalyzeTime;

        /// <summary>
        /// The entity's speed will be multiplied by this to slip it forwards.
        /// </summary>
        [DataField, AutoNetworkedField]
        [Access(Other = AccessPermissions.ReadWrite)]
        public float LaunchForwardsMultiplier = DefaultLaunchForwardsMultiplier;

        /// <summary>
        /// If this is true, any slipping entity loses its friction until
        /// it's not colliding with any SuperSlippery entities anymore.
        /// </summary>
        [DataField, AutoNetworkedField]
        [Access(Other = AccessPermissions.ReadWrite)]
        public bool SuperSlippery;
    }
}
