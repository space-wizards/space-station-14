using Robust.Shared.Random;
using Robust.Shared.Serialization;

namespace Content.Shared.Antag.Selectors;

/// <summary>
/// Spawns a constrained number of antags that scales linearly.
/// </summary>
[Serializable, NetSerializable]
public sealed partial class LinearAntagCount : MinMaxAntagCountSelector
{
    public override int GetTargetAntagCount(IRobustRandom random, int playerCount)
    {
        return Math.Clamp(playerCount / PlayerRatio, (int) Range.Min, (int) Range.Max);
    }
}
