namespace Content.Server.Destructible.Thresholds.Behaviors
{
    [Serializable]
    [DataDefinition]
    public sealed partial class DoActsBehavior : IThresholdBehavior
    {
        /// <summary>
        ///     What acts should be triggered upon activation.
        /// </summary>
        [DataField("acts")]
        public ThresholdActs Acts { get; set; }

        public bool HasAct(ThresholdActs act)
        {
            return (Acts & act) != 0;
        }

        public void Execute(EntityUid owner, DestructibleSystem system, EntityUid? cause = null)
        {
            if (HasAct(ThresholdActs.Breakage))
            {
                system.BreakEntity(owner);
            }

            if (HasAct(ThresholdActs.Destruction))
            {
                system.DestroyEntity(owner);
            }
        }
    }
}
