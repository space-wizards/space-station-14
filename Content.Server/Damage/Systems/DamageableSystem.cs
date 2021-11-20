using Content.Server.Administration.Logs;
using Content.Server.Database;
using Content.Shared.Administration.Logs;
using Content.Shared.Damage;
using Content.Shared.Damage.Systems;
using Content.Shared.FixedPoint;
using Robust.Shared.IoC;

namespace Content.Server.Damage.Systems;

public class DamageableSystem : SharedDamageableSystem
{
    [Dependency] private readonly AdminLogSystem _logs = default!;

    protected override void SetTotalDamage(DamageableComponent damageable, FixedPoint2 @new)
    {
        var owner = damageable.Owner;
        var old = damageable.TotalDamage;
        var change = @new - old;

        _logs.Add(LogType.DamageChange, $"{owner} received {change} damage. Old: {old} | New: {@new}");

        base.SetTotalDamage(damageable, @new);
    }
}
