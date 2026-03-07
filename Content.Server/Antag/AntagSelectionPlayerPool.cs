using System.Diagnostics.CodeAnalysis;
using Robust.Shared.Player;
using Robust.Shared.Random;

namespace Content.Server.Antag;

public sealed class AntagSelectionPlayerPool (List<ICommonSession> pool)
{
    public bool TryPickAndTake(IRobustRandom random, [NotNullWhen(true)] out ICommonSession? session)
    {
        session = null;
        if (pool.Count == 0)
            return false;

        session = random.PickAndTake(pool);
        return true;
    }

    public int Count => pool.Count;
}
