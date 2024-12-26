using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Robust.Shared.Player;
using Robust.Shared.Random;

namespace Content.Server.Antag;

public sealed class AntagSelectionPlayerPool (List<List<ICommonSession>> orderedPools)
{
    public bool TryPickAndTake(IRobustRandom random, [NotNullWhen(true)] out ICommonSession? session)
    {
        session = null;

        foreach (var pool in orderedPools)
        {
            if (pool.Count == 0)
                continue;

            session = random.PickAndTake(pool);
            break;
        }

        return session != null;
    }

    public int Count => orderedPools.Sum(p => p.Count);
}
