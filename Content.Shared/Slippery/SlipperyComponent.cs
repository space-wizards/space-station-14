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
    [RegisterComponent]
    [NetworkedComponent]
    public sealed partial class SlipperyComponent : Component
    {
        /// <summary>
        /// Path to the sound to be played when a mob slips.
        /// </summary>
        [DataField("slipSound")]
        [Access(Other = AccessPermissions.ReadWriteExecute)]
        public SoundSpecifier SlipSound = new SoundPathSpecifier("/Audio/Effects/slip.ogg");

        /// <summary>
        /// How many seconds the mob will be paralyzed for.
        /// </summary>
        [ViewVariables(VVAccess.ReadWrite)]
        [DataField("paralyzeTime")]
        [Access(Other = AccessPermissions.ReadWrite)]
        public float ParalyzeTime = 3f;

        /// <summary>
        /// The entity's speed will be multiplied by this to slip it forwards.
        /// </summary>
        [ViewVariables(VVAccess.ReadWrite)]
        [DataField("launchForwardsMultiplier")]
        [Access(Other = AccessPermissions.ReadWrite)]
        public float LaunchForwardsMultiplier = 1f;
    }

    [Serializable, NetSerializable]
    public sealed class SlipperyComponentState : ComponentState
    {
        public float ParalyzeTime { get; }
        public float LaunchForwardsMultiplier { get; }
        public string SlipSound { get; }

        public SlipperyComponentState(float paralyzeTime, float launchForwardsMultiplier, string slipSound)
        {
            ParalyzeTime = paralyzeTime;
            LaunchForwardsMultiplier = launchForwardsMultiplier;
            SlipSound = slipSound;
        }
    }
}
