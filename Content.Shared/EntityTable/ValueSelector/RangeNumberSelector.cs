using System.Numerics;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Shared.EntityTable.ValueSelector;

/// <summary>
/// Gives a value between the two numbers specified, inclusive.
/// </summary>
public sealed partial class RangeNumberSelector : NumberSelector
{
    [DataField]
    public Vector2 Range = new(1, 1);

    public override float Get(System.Random rand, IEntityManager entMan, IPrototypeManager proto)
    {
        return rand.NextFloat(Range.X, Range.Y + 1);
    }
}
