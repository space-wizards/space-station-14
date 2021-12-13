using Content.Server.Administration.Logs;
using Content.Server.Damage.Components;
using Content.Shared.Administration.Logs;
using Content.Shared.Damage;
using Content.Shared.Database;
using Content.Shared.Throwing;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;

namespace Content.Server.Damage.Systems
{
    public class DamageOtherOnHitSystem : EntitySystem
    {
        [Dependency] private readonly DamageableSystem _damageableSystem = default!;
        [Dependency] private readonly AdminLogSystem _logSystem = default!;

        public override void Initialize()
        {
            SubscribeLocalEvent<DamageOtherOnHitComponent, ThrowDoHitEvent>(OnDoHit);
        }

        private void OnDoHit(EntityUid uid, DamageOtherOnHitComponent component, ThrowDoHitEvent args)
        {
            var dmg = _damageableSystem.TryChangeDamage(args.Target, component.Damage, component.IgnoreResistances);
            if (dmg != null)
                _logSystem.Add(LogType.ThrowHit, $"{ToPrettyString(args.Target):target} received {dmg.Total:damage} damage from collision");
        }
    }
}
