namespace Content.Shared.Damage;

public sealed partial class DamageableSystem
{
    /// <summary>
    /// Applies damage to all entities to see how expensive it is to deal damage.
    /// </summary>
    public void ApplyDamageToAllEntities(List<Entity<DamageableComponent>> damageables, DamageSpecifier damage)
    {
        foreach (var (uid, damageable) in damageables)
        {
            TryChangeDamage(uid, damage, damageable: damageable);
        }
    }
}
