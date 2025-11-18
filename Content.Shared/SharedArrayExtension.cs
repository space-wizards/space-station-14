using Robust.Shared.Random;

namespace Content.Shared;

public static class SharedArrayExtension
{
    /// <summary>
    /// Randomizes the array mutating it in the process
    /// </summary>
    /// <param name="array">array being randomized</param>
    /// <param name="random">source of randomization</param>
    /// <typeparam name="T">type of array element</typeparam>
    public static void Shuffle<T>(this Span<T> array, IRobustRandom? random = null)
    {
        var n = array.Length;
        if (n <= 1)
            return;
        IoCManager.Resolve(ref random);

        while (n > 1)
        {
            n--;
            var k = random.Next(n + 1);
            (array[k], array[n]) =
                (array[n], array[k]);
        }
    }
}
