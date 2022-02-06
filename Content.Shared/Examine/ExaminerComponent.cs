using Robust.Shared.GameObjects;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.ViewVariables;

namespace Content.Shared.Examine
{
    /// <summary>
    ///     Component required for a player to be able to examine things.
    /// </summary>
    [RegisterComponent]
    public sealed class ExaminerComponent : Component
    {
        [ViewVariables(VVAccess.ReadWrite)]
        [DataField("DoRangeCheck")]
        private bool _doRangeCheck = true;

        /// <summary>
        ///     Whether to do a distance check on examine.
        ///     If false, the user can theoretically examine from infinitely far away.
        /// </summary>
        public bool DoRangeCheck => _doRangeCheck;
    }
}
