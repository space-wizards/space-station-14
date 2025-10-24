namespace Content.Shared.Damage;

public sealed partial class DamageableSystem
{
    /// <summary>
    /// Applies damage to all entities to see how expensive it is to deal damage.
    /// </summary>
    public void ApplyDamageToAllEntities(DamageSpecifier damage)
    {
        var query = EntityQueryEnumerator<DamageableComponent>();

        while (query.MoveNext(out var uid, out var damageable))
        {
            TryChangeDamage(uid, damage, damageable: damageable);
        }
    }
}
