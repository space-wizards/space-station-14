using Robust.Shared.Random;

namespace Content.Server.Antag.Selectors;

/// <summary>
/// Spawns a constrained number of antags that scales linearly.
/// </summary>
public sealed partial class LinearAntagCount : MinMaxAntagCountSelector
{
    public override int GetTargetAntagCount(IRobustRandom random, int playerCount)
    {
        return Math.Clamp(playerCount / PlayerRatio, Range.Min, Range.Max);
    }
}
