using Robust.Shared.Random;

namespace Content.Shared.Antag.Selectors;

/// <summary>
/// Always spawns this many antags.
/// </summary>
public sealed partial class FixedAntagCount : AntagCountSelector
{
    [DataField]
    public int Count = 1;

    public override int GetTargetAntagCount(IRobustRandom random, int playerCount)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(Count);
        return Count;
    }
}
