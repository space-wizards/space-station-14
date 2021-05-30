#nullable enable
using Robust.Shared.GameObjects;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.ViewVariables;

namespace Content.Shared.GameObjects.Components.Mobs
{
    /// <summary>
    ///     Component required for a player to be able to examine things.
    /// </summary>
    [RegisterComponent]
    public sealed class ExaminerComponent : Component
    {
        public override string Name => "Examiner";

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
