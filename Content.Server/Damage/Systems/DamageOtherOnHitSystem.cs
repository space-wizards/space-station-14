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

            // Log damage only for mobs. Useful for when people throw spears at each other, but also avoids log-spam when explosions send glass shards flying.
            if (dmg != null && HasComp<MobStateComponent>(args.Target))
                _adminLogger.Add(LogType.ThrowHit, $"{ToPrettyString(args.Target):target} received {dmg.Total:damage} damage from collision");

            // Only play hitsound of thrown item and signal to stop when hitting mobs
            if (HasComp<MobStateComponent>(args.Target))
            {
                _audio.Play(component.HitSound, Filter.Pvs(args.Target), args.Target);

                if (component.StopOnHit)
                {
                    args.StopCollisions = true;
                    args.StopMoving = true;
                }
            }

        }
    }
}
