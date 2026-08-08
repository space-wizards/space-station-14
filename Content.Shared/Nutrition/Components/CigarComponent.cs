using Content.Shared.Nutrition.EntitySystems;
using Content.Shared.FixedPoint;
using Robust.Shared.GameStates;

namespace Content.Shared.Nutrition.Components;

/// <summary>
///     A disposable, single-use smokable.
/// </summary>
[RegisterComponent, NetworkedComponent, Access(typeof(SharedSmokingSystem))]
public sealed partial class CigarComponent : Component
{
    /// <summary>
    ///     Amount of solution drawn from a container when the cigar is dipped, in units.
    /// </summary>
    [DataField]
    public FixedPoint2 DipAmount = FixedPoint2.New(10);
}
