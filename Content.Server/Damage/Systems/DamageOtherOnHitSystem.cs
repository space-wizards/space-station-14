using Content.Server.Administration.Logs;
using Content.Server.Damage.Components;
using Content.Shared.Damage;
using Content.Shared.Database;
using Content.Shared.MobState.Components;
using Content.Shared.Throwing;
using Robust.Shared.Player;

namespace Content.Server.Damage.Systems
{
    public sealed class DamageOtherOnHitSystem : EntitySystem
    {
        [Dependency] private readonly DamageableSystem _damageableSystem = default!;
        [Dependency] private readonly IAdminLogManager _adminLogger= default!;
        [Dependency] private readonly SharedAudioSystem _audio = default!;

        public override void Initialize()
        {
            SubscribeLocalEvent<DamageOtherOnHitComponent, ThrowDoHitEvent>(OnDoHit);
        }

        private void OnDoHit(EntityUid uid, DamageOtherOnHitComponent component, ref ThrowDoHitEvent args)
        {
            var dmg = _damageableSystem.TryChangeDamage(args.Target, component.Damage, component.IgnoreResistances, origin: args.User);

            // Only play hitsound and stop if hitting mobs.
            if (HasComp<MobStateComponent>(args.Target))
            {
                _audio.Play(component.HitSound, Filter.Pvs(args.Target), args.Target);

                // Log nonzero damage only for mobs. Useful for when people throw spears at each other, but also avoids log-spam when explosions send glass shards flying.
                if (dmg != null)
                    _adminLogger.Add(LogType.ThrowHit, $"{ToPrettyString(args.Target):target} received {dmg.Total:damage} damage from collision");

                if (component.StopOnHit)
                {
                    args.StopCollisions = true;
                    args.StopMoving = true;
                }
            }

        }
    }
}
