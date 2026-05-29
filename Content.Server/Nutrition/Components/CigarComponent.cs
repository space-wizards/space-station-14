using Content.Server.Nutrition.EntitySystems;
using Content.Shared.FixedPoint;

namespace Content.Server.Nutrition.Components
{
    /// <summary>
    ///     A disposable, single-use smokable.
    /// </summary>
    [RegisterComponent, Access(typeof(SmokingSystem))]
    public sealed partial class CigarComponent : Component
    {
        /// <summary>
        ///     Amount of solution drawn from a container when the cigar is dipped, in units.
        /// </summary>
        [DataField]
        public FixedPoint2 DipAmount = FixedPoint2.New(10);
    }
}
