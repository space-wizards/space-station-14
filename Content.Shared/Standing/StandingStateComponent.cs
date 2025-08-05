using Robust.Shared.Audio;
using Robust.Shared.GameStates;

namespace Content.Shared.Standing
{
    [RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
    [Access(typeof(StandingStateSystem))]
    public sealed partial class StandingStateComponent : Component
    {
        [ViewVariables(VVAccess.ReadWrite)]
        [DataField]
        public SoundSpecifier? DownSound { get; private set; } = new SoundCollectionSpecifier("BodyFall");

        [DataField, AutoNetworkedField]
        public bool Standing { get; set; } = true;

        /// <summary>
        /// Time it takes us to stand up
        /// </summary>
        [DataField, AutoNetworkedField]
        public TimeSpan StandTime = TimeSpan.FromSeconds(2);

        /// <summary>
        /// Default Friction modifier for knocked down players.
        /// Makes them accelerate and deccelerate slower.
        /// </summary>
        [DataField, AutoNetworkedField]
        public float FrictionModifier = 0.4f;

        /// <summary>
        /// Base modifier to the maximum movement speed of a knocked down mover.
        /// </summary>
        [DataField, AutoNetworkedField]
        public float SpeedModifier = 0.3f;

        /// <summary>
        ///     List of fixtures that had their collision mask changed when the entity was downed.
        ///     Required for re-adding the collision mask.
        /// </summary>
        [DataField, AutoNetworkedField]
        public List<string> ChangedFixtures = new();
    }
}
