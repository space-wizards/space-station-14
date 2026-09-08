using Content.Shared.Damage;

namespace Content.Server.Destructible;

public sealed partial class DestructibleSystem
{
    /// <summary>
    /// Tests all triggers in a DestructibleComponent to see how expensive it is to query them.
    /// </summary>
    public void TestAllTriggers(List<Entity<Shared.Damage.Components.DamageableComponent, DestructibleComponent>> destructibles)
    {
        foreach (var (uid, damageable, destructible) in destructibles)
        {
            foreach (var threshold in destructible.Thresholds)
            {
                // Chances are, none of these triggers will pass!
                Triggered(threshold, (uid, damageable));
            }
        }
    }

    /// <summary>
    /// Tests all behaviours in a DestructibleComponent to see how expensive it is to query them.
    /// </summary>
    public void TestAllBehaviors(List<Entity<Shared.Damage.Components.DamageableComponent, DestructibleComponent>> destructibles)
    {
       foreach (var (uid, damageable, destructible) in destructibles)
       {
           foreach (var threshold in destructible.Thresholds)
           {
               Execute(threshold, uid);
           }
       }
    }
}
