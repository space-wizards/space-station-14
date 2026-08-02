// Мёртвый Космос, Licensed under custom terms with restrictions on public hosting and commercial use, full text: https://raw.githubusercontent.com/dead-space-server/space-station-14-fobos/master/LICENSE.TXT

using Content.Shared.Body.Components;
using Content.Shared.Body.Part;
using Content.Shared.Body.Systems;
using Content.Shared.Damage;
using Content.Shared.Damage.Components;
using Content.Shared.Damage.Systems;
using Content.Shared.DeadSpace.Necromorphs.InfectionDead.Components;
using Content.Shared.DeadSpace.Necromorphs.PlasmaCutter;
using Content.Shared.DeadSpace.Necromorphs.CorpseCollector.Components;
using Content.Shared.DeadSpace.Necromorphs.Leviathan.Components;
using Content.Shared.FixedPoint;
using Content.Shared.Humanoid;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using Content.Shared.Mobs.Systems;
using Content.Shared.Movement.Components;
using Content.Shared.Movement.Systems;
using Content.Shared.Speech.Muting;
using Content.Shared.Weapons.Hitscan.Events;
using Robust.Shared.Containers;
using Robust.Shared.Physics.Systems;
using System.Linq;
using System.Numerics;

namespace Content.Server.DeadSpace.Necromorphs.PlasmaCutter;

public sealed class NecromorphPlasmaCutterSystem : EntitySystem
{
    [Dependency] private readonly SharedBodySystem _body = default!;
    [Dependency] private readonly SharedContainerSystem _containers = default!;
    [Dependency] private readonly DamageableSystem _damage = default!;
    [Dependency] private readonly MobStateSystem _mobState = default!;
    [Dependency] private readonly MovementSpeedModifierSystem _movement = default!;
    [Dependency] private readonly SharedPhysicsSystem _physics = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<NecromorphPlasmaCutterComponent, HitscanDamageDealtEvent>(OnHit);
    }

    private void OnHit(Entity<NecromorphPlasmaCutterComponent> ent, ref HitscanDamageDealtEvent args)
    {
        var target = args.Target;
        if (!HasComp<NecromorfComponent>(target))
            return;

        if (HasComp<LeviathanComponent>(target))
            return;

        if (HasComp<CorpseCollectorComponent>(target))
        {
            DamageByPercentage(target, CompOrNull<NecromorphPlasmaCutterReducedDamageComponent>(target));
            return;
        }

        if (!HasComp<HumanoidAppearanceComponent>(target) || !HasComp<BodyComponent>(target))
        {
            DamageByPercentage(target, CompOrNull<NecromorphPlasmaCutterReducedDamageComponent>(target));
            return;
        }

        var wounds = EnsureComp<NecromorphPlasmaCutterWoundsComponent>(target);
        TryComp<MovementSpeedModifierComponent>(target, out var movement);
        var oneLegWalkSpeed = movement?.BaseWalkSpeed ?? 0f;
        var oneLegSprintSpeed = movement?.BaseSprintSpeed ?? 0f;
        var oneLegAcceleration = movement?.BaseAcceleration ?? 0f;
        if (wounds.RemovedLegs < 2 && TryDetachFirstPart(target, BodyPartType.Leg, args.Data.ShotDirection, ent.Comp.DetachedPartImpulse))
        {
            if (wounds.RemovedLegs == 0 && movement != null)
            {
                wounds.OriginalWalkSpeed = movement.BaseWalkSpeed;
                wounds.OriginalSprintSpeed = movement.BaseSprintSpeed;
                wounds.OriginalAcceleration = movement.BaseAcceleration;
            }

            wounds.RemovedLegs++;
            Dirty(target, wounds);

            // The body system supplies 50% with one leg. Preserve 20% of that after losing the second leg.
            if (wounds.RemovedLegs >= 2 && movement != null)
                _movement.ChangeBaseSpeed(target, oneLegWalkSpeed * 0.2f, oneLegSprintSpeed * 0.2f, oneLegAcceleration * 0.2f, movement);

            return;
        }

        if (!TryDetachFirstPart(target, BodyPartType.Head, args.Data.ShotDirection, ent.Comp.DetachedPartImpulse))
            return;

        EnsureComp<NecromorphMissingHeadComponent>(target);
        _mobState.ChangeMobState(target, MobState.Dead);
    }

    private bool TryDetachFirstPart(EntityUid target, BodyPartType type, Vector2 shotDirection, float impulse)
    {
        var part = _body.GetBodyChildrenOfType(target, type).FirstOrDefault();
        if (part.Id == EntityUid.Invalid ||
            !_containers.TryGetContainingContainer((part.Id, null, null), out var container))
        {
            return false;
        }

        if (!_containers.Remove(part.Id, container, force: true))
            return false;

        if (shotDirection.LengthSquared() > 0f)
            _physics.ApplyLinearImpulse(part.Id, shotDirection.Normalized() * impulse);

        return true;
    }

    private void DamageByPercentage(EntityUid target, NecromorphPlasmaCutterReducedDamageComponent? reduced)
    {
        if (_mobState.IsDead(target))
            return;

        if (!TryComp<DamageableComponent>(target, out var damageable) ||
            !TryComp<MobThresholdsComponent>(target, out var thresholds))
        {
            return;
        }

        var state = EnsureComp<NecromorphPlasmaCutterDamageComponent>(target);
        state.Hits++;
        var deathThreshold = thresholds.Thresholds
            .Where(pair => pair.Value == MobState.Dead)
            .Select(pair => pair.Key.Float())
            .DefaultIfEmpty(damageable.TotalDamage.Float())
            .Min();
        var criticalThreshold = thresholds.Thresholds
            .Where(pair => pair.Value == MobState.Critical)
            .Select(pair => pair.Key.Float())
            .DefaultIfEmpty(deathThreshold)
            .Min();

        var hitsToKill = reduced?.HitsToKill ?? 3;
        float damageAmount;

        if (state.Hits >= hitsToKill)
        {
            var toCritical = Math.Max(0f, criticalThreshold - damageable.TotalDamage.Float());
            damageAmount = toCritical + 1f;
        }
        else
        {
            var health = Math.Max(0f, deathThreshold - damageable.TotalDamage.Float());
            var fraction = state.Hits == 1
                ? reduced?.FirstHitFraction ?? 0.5f
                : reduced?.LaterHitFraction ?? 0.35f;
            damageAmount = health * fraction;
        }

        var damage = new DamageSpecifier
        {
            DamageDict = { ["Heat"] = FixedPoint2.New(damageAmount) }
        };

        var wasMutedForDamage = HasComp<MutedComponent>(target);
        EnsureComp<MutedComponent>(target);
        _damage.TryChangeDamage(target, damage, true, false);
        if (!wasMutedForDamage)
            RemCompDeferred<MutedComponent>(target);
    }
}
