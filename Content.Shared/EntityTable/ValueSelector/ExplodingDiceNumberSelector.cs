using Robust.Shared.Random;

namespace Content.Shared.EntityTable.ValueSelector;

/// <summary>
/// Rolls an N sided dice on every selection adding the result to the count of selected values.
/// For each roll which matches the max value N, an additional roll is performed.
/// </summary>
public sealed partial class ExplodingDiceNumberSelector : NumberSelector
{
    /// <summary>
    /// The max value of the die being rolled
    /// </summary>
    [DataField]
    public int DieSize = 6;

    /// <summary>
    /// Whether our die includes a zero value.
    /// </summary>
    /// <remarks>
    /// Look, I know. but the standard formula for exploding dice is meant for tabeltop games where a value of zero does not exist.
    /// However, exploding dice with no zero is a very spikey distribution because you can never get a value of exacly a multiple of DieSize.
    /// Including zero is better for this reason, but is not *strictly expected*
    /// </remarks>
    [DataField]
    public bool ZeroInclusive = true;

    public override int Get(System.Random rand)
    {
        var random = IoCManager.Resolve<IRobustRandom>();

        int lowerBound = ZeroInclusive ? 0 : 1;
        bool firstRoll = true;

        int count = 0;
        bool success = true;

        if (DieSize <= lowerBound)
        {
            var log = IoCManager.Resolve<ISawmill>(); //cursed I hate it.
            log.Warning($"ExplodingDiceNumberSelector was attempted with a die of size <= {lowerBound}. Attempted die size: {DieSize}");
            return 1;
        }

        while (success)
        {
            var firstRollShift = firstRoll ? 1 : 0; // guaruntees at least one entry returns. Use Prob for zero returns.

            var roll = random.Next(lowerBound + firstRollShift, DieSize + 1);
            count += roll;
            if (roll != DieSize)
                success = false;

            firstRoll = false;
        }

        return count;
        // Kaboom!
    }

}
