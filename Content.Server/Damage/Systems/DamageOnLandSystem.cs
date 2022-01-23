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
    public sealed class DamageOnLandSystem : EntitySystem
    {
        [Dependency] private readonly DamageableSystem _damageableSystem = default!;
        [Dependency] private readonly AdminLogSystem _logSystem = default!;

        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<DamageOnLandComponent, LandEvent>(DamageOnLand);
        }

        private void DamageOnLand(EntityUid uid, DamageOnLandComponent component, LandEvent args)
        {
            var dmg = _damageableSystem.TryChangeDamage(uid, component.Damage, component.IgnoreResistances);
            if (dmg == null)
                return;

            _logSystem.Add(LogType.Landed, $"{ToPrettyString(component.Owner):entity} received {dmg.Total:damage} damage from landing");
        }
    }
}
