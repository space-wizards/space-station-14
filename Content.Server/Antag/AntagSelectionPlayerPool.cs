using Robust.Shared.Player;
using Robust.Shared.Random;

namespace Content.Server.Antag;

public sealed class AntagSelectionPlayerPool(
    List<ICommonSession> primaryPool,
    List<ICommonSession> secondaryPool,
    List<ICommonSession> fallbackPool,
    List<ICommonSession> rawPool)
{
    /// <summary>
    /// The sessions of players who are valid candidates who have selected preference for the antagonist.
    /// </summary>
    private readonly List<ICommonSession> _primaryPool = primaryPool;

    /// <summary>
    /// The sessions of players who are valid candidates but do not have the primary selected preference.
    /// </summary>
    private readonly List<ICommonSession> _secondaryPool = secondaryPool;

    /// <summary>
    /// The sessions of players who do not have any of the proper selected preferences yet still pass the other qualifications.
    /// </summary>
    private readonly List<ICommonSession> _fallbackPool = fallbackPool;

    /// <summary>
    /// All players not filtered into any of the other lists. This is very rarely being
    /// </summary>
    private readonly List<ICommonSession> _rawPool = rawPool;

    //todo a version of this that doesn't throw would be nice
    public ICommonSession PickAndTake(IRobustRandom random)
    {
        if (_primaryPool.Count != 0)
            return random.PickAndTake(_primaryPool);
        if (_secondaryPool.Count != 0)
            return random.PickAndTake(_secondaryPool);
        if (_fallbackPool.Count != 0)
            return random.PickAndTake(_fallbackPool);
        if (_rawPool.Count != 0)
            return random.PickAndTake(_rawPool);

        throw new InvalidOperationException("No players left to select from!");
    }

    public int Count => _primaryPool.Count + _secondaryPool.Count + _fallbackPool.Count + _rawPool.Count;
}
