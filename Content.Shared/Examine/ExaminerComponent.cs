namespace Content.Shared.Examine
{
    /// <summary>
    ///     Component required for a player to be able to examine things.
    /// </summary>
    [RegisterComponent]
    public sealed partial class ExaminerComponent : Component
    {
        [ViewVariables(VVAccess.ReadWrite)]
        [DataField("skipChecks")]
        public bool SkipChecks = false;

        [ViewVariables(VVAccess.ReadWrite)]
        [DataField("checkInRangeUnOccluded")]
        public bool CheckInRangeUnOccluded = true;
    }
}
