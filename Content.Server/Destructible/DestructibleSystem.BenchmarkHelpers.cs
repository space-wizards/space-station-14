using Content.Shared.Damage;

namespace Content.Server.Destructible;

/// <summary>
/// This contains methods used for DestructibleBenchmark.
/// </summary>
public sealed partial class DestructibleSystem
{
    public bool TestAllTriggers()
    {
        var query = EntityQueryEnumerator<DestructibleComponent, DamageableComponent>();

        while (query.MoveNext(out var uid, out var destructible, out var damageable))
        {
            foreach (var threshold in destructible.Thresholds)
            {
                // Chances are, none of these triggers will pass! But we have the extra code just in case!
                if (Triggered(threshold, (uid, damageable)))
                {
                    RaiseLocalEvent(uid, new DamageThresholdReached(destructible, threshold), true);
                    Execute(threshold, uid);
                }
            }
        }

        return true;
    }
}
