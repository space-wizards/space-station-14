namespace Content.Shared.Examine
{
    /// <summary>
    ///     Component required for a player to be able to examine things.
    /// </summary>
    [RegisterComponent]
    public sealed partial class ExaminerComponent : Component
    {
        /// <summary>
        /// If true, skip all checks if an examiner can examine something.
        /// </summary>
        [ViewVariables(VVAccess.ReadWrite)]
        [DataField("skipChecks")]
        public bool SkipChecks = false;

        /// <summary>
        /// Must examiner have line of sight?
        /// </summary>
        [ViewVariables(VVAccess.ReadWrite)]
        [DataField("checkInRangeUnOccluded")]
        public bool CheckInRangeUnOccluded = true;
    }
}
