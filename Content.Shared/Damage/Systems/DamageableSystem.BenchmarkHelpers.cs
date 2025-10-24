namespace Content.Shared.Damage;

/// <summary>
/// This contains methods used for DestructibleBenchmark.
/// </summary>
public sealed partial class DamageableSystem
{
    public void ApplyDamageToAllEntities(DamageSpecifier damage)
    {
        var query = EntityQueryEnumerator<DamageableComponent>();

        while (query.MoveNext(out var uid, out var damageable))
        {
            TryChangeDamage(uid, damage, damageable: damageable);
        }
    }
}
