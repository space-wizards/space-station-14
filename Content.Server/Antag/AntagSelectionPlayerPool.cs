using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Robust.Shared.Player;
using Robust.Shared.Random;

namespace Content.Server.Antag;

// DS14-start
public sealed class AntagSelectionPlayerPool
{
    private readonly List<List<ICommonSession>> _orderedPools;
    private readonly List<ICommonSession>? _sponsorPool;
    private readonly List<ICommonSession>? _regularPool;
    private readonly List<ICommonSession>? _fallbackPool;
    private readonly bool _strictRegularSlots;
    private int _sponsorSlotsRemaining;
    private int _regularSlotsRemaining;

    public AntagSelectionPlayerPool(List<List<ICommonSession>> orderedPools)
    {
        _orderedPools = orderedPools;
    }

    public AntagSelectionPlayerPool(
        List<ICommonSession> sponsorPool,
        List<ICommonSession> regularPool,
        List<ICommonSession> fallbackPool,
        int sponsorSlots,
        int totalSlots,
        bool strictRegularSlots = false)
    {
        _sponsorPool = sponsorPool;
        _regularPool = regularPool;
        _fallbackPool = fallbackPool;
        _strictRegularSlots = strictRegularSlots;
        _orderedPools = new() { sponsorPool, regularPool, fallbackPool };
        var clampedTotalSlots = Math.Max(totalSlots, 0);
        _sponsorSlotsRemaining = Math.Clamp(sponsorSlots, 0, clampedTotalSlots);
        _regularSlotsRemaining = clampedTotalSlots - _sponsorSlotsRemaining;
    }
    // DS14-end

    public bool TryPickAndTake(IRobustRandom random, [NotNullWhen(true)] out ICommonSession? session)
    {
        session = null;

        // DS14-start
        if (_sponsorPool != null && _regularPool != null && _fallbackPool != null)
        {
            if (_strictRegularSlots && _regularSlotsRemaining > 0)
            {
                _regularSlotsRemaining--;
                return TryPickAndTake(random, out session, _regularPool);
            }

            if (_sponsorSlotsRemaining > 0)
            {
                _sponsorSlotsRemaining--;
                return TryPickAndTake(random, out session, _sponsorPool, _regularPool, _fallbackPool);
            }

            if (_regularSlotsRemaining > 0)
            {
                _regularSlotsRemaining--;
                return TryPickAndTake(random, out session, _regularPool, _sponsorPool, _fallbackPool);
            }
        }

        return TryPickAndTake(random, out session, _orderedPools.ToArray());
    }

    private static bool TryPickAndTake(
        IRobustRandom random,
        [NotNullWhen(true)] out ICommonSession? session,
        params List<ICommonSession>[] orderedPools)
    {
        session = null;
        // DS14-end

        foreach (var pool in orderedPools)
        {
            if (pool.Count == 0)
                continue;

            session = random.PickAndTake(pool);
            break;
        }

        return session != null;
    }

    public int Count => _orderedPools.Sum(p => p.Count); // DS14
}
