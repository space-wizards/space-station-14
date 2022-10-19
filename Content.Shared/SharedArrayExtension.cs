using Robust.Shared.Random;

namespace Content.Shared;

public static class SharedArrayExtension
{
    /// <summary>
    /// Randomizes the array mutating it in the process
    /// </summary>
    /// <param name="array">array being randomized</param>
    /// <param name="random">source of randomization</param>
    /// <typeparam name="T">type of array ellement</typeparam>
    public static void Shuffle<T>(this Span<T> array, IRobustRandom? random = null)
    {
        var n = array.Length;
        if (n <= 1)
            return;

        var robustRandom = random ?? IoCManager.Resolve<IRobustRandom>();

        while (n > 1)
        {
            n--;
            var k = robustRandom.Next(n + 1);
            (array[k], array[n]) =
                (array[n], array[k]);
        }
    }
}
