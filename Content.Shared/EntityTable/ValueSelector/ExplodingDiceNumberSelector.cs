using Robust.Shared.Random;

namespace Content.Shared.EntityTable.ValueSelector;

/// <summary>
/// Rolls an N sided dice on every selection adding the result to the count of selected values.
/// For each roll which matches the max value N, an additional roll is performed.
/// </summary>
public sealed partial class ExplodingDiceNumberSelector : NumberSelector
{
    [DataField]
    public int DieSize = 6;
    public override int Get(System.Random rand)
    {
        var random = IoCManager.Resolve<IRobustRandom>();
        var log = IoCManager.Resolve<ISawmill>(); //cursed I hate it.
        int count = 0;
        bool success = true;

        if (DieSize <= 1)
        {
            log.Warning($"ExplodingDiceNumberSelector was attempted with a die of size <= 1. Attmpted die size: {DieSize}");
            return 1;
        }

        while (success)
        {
            var roll = random.Next(1, DieSize);
            count += roll;
            if (roll != DieSize)
                success = false;
        }

        return count;
        // Kaboom!
    }

}
