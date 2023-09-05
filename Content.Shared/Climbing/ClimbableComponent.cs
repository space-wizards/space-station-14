using Content.Shared.CCVar;
using Content.Shared.Damage;
using Content.Shared.Interaction;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;

namespace Content.Shared.Climbing
{
    [RegisterComponent, NetworkedComponent]
    public sealed partial class ClimbableComponent : Component
    {
        /// <summary>
        ///     The range from which this entity can be climbed.
        /// </summary>
        [DataField("range")] public float Range = SharedInteractionSystem.InteractionRange / 1.4f;

        /// <summary>
        ///     The time it takes to climb onto the entity.
        /// </summary>
        [DataField("delay")]
        public float ClimbDelay = 0.8f;

        /// <summary>
        ///     Sound to be played when a climb is started.
        /// </summary>
        [DataField("startClimbSound")]
        public SoundSpecifier? StartClimbSound = null;

        /// <summary>
        ///     Sound to be played when a climb finishes.
        /// </summary>
        [DataField("finishClimbSound")]
        public SoundSpecifier? FinishClimbSound = null;
    }
}
