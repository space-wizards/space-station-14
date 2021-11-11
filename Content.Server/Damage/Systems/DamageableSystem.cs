using Content.Server.Administration.Logs;
using Content.Shared.Damage;
using Content.Shared.Damage.Logs;
using Content.Shared.Damage.Systems;
using Content.Shared.FixedPoint;
using Robust.Server.GameObjects;
using Robust.Shared.IoC;

namespace Content.Server.Damage.Systems;

public class DamageableSystem : SharedDamageableSystem
{
    [Dependency] private readonly AdminLogSystem _logs = default!;

    protected override void SetTotalDamage(DamageableComponent damageable, FixedPoint2 amount)
    {
        var log = new TotalDamageAdminLog(
            damageable.OwnerUid,
            damageable.TotalDamage.UnShiftedInt(),
            amount.UnShiftedInt());

        if (EntityManager.TryGetComponent(damageable.OwnerUid, out ActorComponent? actor))
        {
            _logs.Add(log, actor.PlayerSession.UserId);
        }
        else
        {
            _logs.Add(log);
        }

        base.SetTotalDamage(damageable, amount);
    }
}
