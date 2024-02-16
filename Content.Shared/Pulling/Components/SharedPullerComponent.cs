using Robust.Shared.GameStates;

namespace Content.Shared.Pulling.Components
{
    [RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
    [Access(typeof(SharedPullingStateManagementSystem))]
    public sealed partial class SharedPullerComponent : Component
    {
        // Before changing how this is updated, please see SharedPullerSystem.RefreshMovementSpeed
        public float WalkSpeedModifier => Pulling == default ? 1.0f : 0.95f;

        public float SprintSpeedModifier => Pulling == default ? 1.0f : 0.95f;

        [DataField, AutoNetworkedField]
        public EntityUid? Pulling { get; set; }

        /// <summary>
        ///     Does this entity need hands to be able to pull something?
        /// </summary>
        [DataField("needsHands")]
        public bool NeedsHands = true;
    }
}
