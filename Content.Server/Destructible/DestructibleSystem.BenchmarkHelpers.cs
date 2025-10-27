using Content.Shared.Damage;

namespace Content.Server.Destructible;

public sealed partial class DestructibleSystem
{
    /// <summary>
    /// Tests all triggers in a DestructibleComponent to see how expensive it is to query them.
    /// </summary>
    public void TestAllTriggers(List<Entity<DamageableComponent, DestructibleComponent>> destructibles)
    {
        foreach (var (uid, damageable, destructible) in destructibles)
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
    }

    public void TestAllBehaviors(List<Entity<DamageableComponent, DestructibleComponent>> destructibles)
    {
       foreach (var (uid, damageable, destructible) in destructibles)
       {
           foreach (var threshold in destructible.Thresholds)
           {
               RaiseLocalEvent(uid, new DamageThresholdReached(destructible, threshold), true);
               Execute(threshold, uid);
           }
       }
    }
}
