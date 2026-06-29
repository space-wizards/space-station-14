using Content.Shared.Administration.Logs;
using Content.Shared.Camera;
using Content.Shared.CombatMode.Pacification;
using Content.Shared.Damage.Components;
using Content.Shared.Damage.Events;
using Content.Shared.Database;
using Content.Shared.Effects;
using Content.Shared.Mobs.Components;
using Content.Shared.Throwing;
using Content.Shared.Weapons.Ranged.Systems;
using Robust.Shared.Physics.Components;
using Robust.Shared.Player;

namespace Content.Shared.Damage.Systems;

public abstract partial class SharedDamageOtherOnHitSystem : EntitySystem
{
    [Dependency] private ISharedAdminLogManager _adminLogger = default!;
    [Dependency] private DamageExamineSystem _damageExamine = default!;
    [Dependency] private DamageableSystem _damageable = default!;
    [Dependency] private SharedCameraRecoilSystem _cameraRecoil = default!;
    [Dependency] private SharedGunSystem _guns = default!;
    [Dependency] private SharedColorFlashEffectSystem _color = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<DamageOtherOnHitComponent, DamageExamineEvent>(OnDamageExamine);
        SubscribeLocalEvent<DamageOtherOnHitComponent, AttemptPacifiedThrowEvent>(OnAttemptPacifiedThrow);
        SubscribeLocalEvent<DamageOtherOnHitComponent, ThrowDoHitEvent>(OnDoHit);
    }

    private void OnDamageExamine(Entity<DamageOtherOnHitComponent> ent, ref DamageExamineEvent args)
    {
        _damageExamine.AddDamageExamine(args.Message, _damageable.ApplyUniversalAllModifiers(ent.Comp.Damage * _damageable.UniversalThrownDamageModifier), Loc.GetString("damage-throw"));
    }

    /// <summary>
    /// Prevent players with the Pacified status effect from throwing things that deal damage.
    /// </summary>
    private void OnAttemptPacifiedThrow(Entity<DamageOtherOnHitComponent> ent, ref AttemptPacifiedThrowEvent args)
    {
        args.Cancel("pacified-cannot-throw");
    }

    private void OnDoHit(Entity<DamageOtherOnHitComponent> ent, ref ThrowDoHitEvent args)
    {
        if (TerminatingOrDeleted(args.Target))
            return;

        var dmg = _damageable.ChangeDamage(args.Target, ent.Comp.Damage * _damageable.UniversalThrownDamageModifier, ent.Comp.IgnoreResistances, origin: args.Thrown.Comp.Thrower);

        // Log damage only for mobs. Useful for when people throw spears at each other, but also avoids log-spam when explosions send glass shards flying.
        if (HasComp<MobStateComponent>(args.Target))
            _adminLogger.Add(LogType.ThrowHit, $"{ToPrettyString(args.Target):target} received {dmg.GetTotal():damage} damage from collision");

        if (!dmg.Empty)
        {
            var list = new List<EntityUid>();
            list.Add(args.Target);
            _color.RaisePredictedEffect(Color.Red, list, Filter.Pvs(args.Target, entityManager: EntityManager), args.Thrown.Comp.Thrower);
        }

        _guns.PlayImpactSound(args.Target, dmg, null, false, args.Thrown.Comp.Thrower);

        if (TryComp<PhysicsComponent>(ent, out var body) && body.LinearVelocity.LengthSquared() > 0f)
        {
            var direction = body.LinearVelocity.Normalized();
            _cameraRecoil.KickCamera(args.Target, direction);
        }
    }
}
