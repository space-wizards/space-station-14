using Content.Server.Administration.Logs;
using Content.Shared.Damage;
using Content.Shared.Damage.Systems;
using Content.Shared.FixedPoint;
using Robust.Shared.IoC;

namespace Content.Server.Damage.Systems;

public class DamageableSystem : SharedDamageableSystem
{
    [Dependency] private readonly AdminLogSystem _logs = default!;

    protected override void SetTotalDamage(DamageableComponent damageable, FixedPoint2 amount)
    {
        var owner = damageable.Owner;
        var old = damageable.TotalDamage;
        var change = amount - old;

        _logs.Add(LogType.DamageChange, $"Entity {owner:Owner} received {change:DamageChange} damage. Old: {old:OldDamage} | New: {amount:NewDamage}");

        base.SetTotalDamage(damageable, amount);
    }
}
