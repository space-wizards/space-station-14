using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Robust.Shared.Player;
using Robust.Shared.Random;

namespace Content.Server.Antag;

public sealed class AntagSelectionPlayerPool (List<List<ICommonSession>> orderedPools, bool onePerPool = false)
{
    public bool TryPickAndTake(IRobustRandom random, [NotNullWhen(true)] out ICommonSession? session)
    {
        session = null;

        foreach (var pool in orderedPools)
        {
            if (pool.Count == 0)
                continue;

            session = random.PickAndTake(pool);

            // for antags that can only use 1 player from each pool, clear the pool after picking from it
            if (onePerPool)
                pool.Clear();

            break;
        }

        return session != null;
    }

    /// <summary>
    /// Consume the last session in the last pool until there are none left.
    /// Will completely clear the selection pool once it returns false.
    /// </summary>
    /// <remarks>
    /// Intended for use in grouping.
    /// </remarks>
    public bool MoveNext([NotNullWhen(true)] out ICommonSession? session)
    {
        session = null;
        while (orderedPools.Count > 0)
        {
            var pool = orderedPools[orderedPools.Count - 1];
            if (pool.Count == 0)
            {
                orderedPools.RemoveAt(orderedPools.Count - 1);
                continue;
            }

            session = pool[pool.Count - 1];
            pool.RemoveAt(pool.Count - 1);
            return true;
        }

        return false;
    }

    public int Count => orderedPools.Sum(p => p.Count);
}
