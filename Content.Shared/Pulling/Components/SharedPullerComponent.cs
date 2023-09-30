namespace Content.Shared.Pulling.Components
{
    [RegisterComponent]
    [Access(typeof(SharedPullingStateManagementSystem))]
    public sealed partial class SharedPullerComponent : Component
    {
        // Before changing how this is updated, please see SharedPullerSystem.RefreshMovementSpeed
        public float WalkSpeedModifier => Pulling == default ? 1.0f : 0.95f;

        public float SprintSpeedModifier => Pulling == default ? 1.0f : 0.95f;

        [ViewVariables]
        public EntityUid? Pulling { get; set; }

        /// <summary>
        ///     Does this entity need hands to be able to pull something?
        /// </summary>
        [DataField("needsHands")]
        public bool NeedsHands = true;

        protected override void OnRemove()
        {
            if (Pulling != default)
            {
                // This is absolute paranoia but it's also absolutely necessary. Too many puller state bugs. - 20kdc
                Logger.ErrorS("c.go.c.pulling", "PULLING STATE CORRUPTION IMMINENT IN PULLER {0} - OnRemove called when Pulling is set!", Owner);
            }
            base.OnRemove();
        }
    }
}
