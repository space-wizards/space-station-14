using Content.Shared.Damage;
using Content.Shared.Examine;
using Content.Shared.Mobs.Components;
using Content.Shared.Popups;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Physics.Components;
using Robust.Shared.Physics.Events;
using Robust.Shared.Physics.Systems;
using Robust.Shared.Random;
using Content.Shared.Damage.Systems;

namespace Content.Shared.ImmovableRod;

public abstract class SharedImmovableRodSystem : EntitySystem
{
    [Dependency] private readonly IRobustRandom _random = default!;

    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly SharedPhysicsSystem _physics = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;

    [Dependency] private readonly SharedTransformSystem _transform = default!;

    [Dependency] private readonly DamageableSystem _damageable = default!;
    [Dependency] private readonly SharedStaminaSystem _stamina = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ImmovableRodComponent, StartCollideEvent>(OnCollide);
        SubscribeLocalEvent<ImmovableRodComponent, ComponentStartup>(OnMapInit);
        SubscribeLocalEvent<ImmovableRodComponent, ExaminedEvent>(OnExamined);
    }

    private void OnMapInit(EntityUid uid, ImmovableRodComponent component, ComponentStartup args)
    {
        if (!TryComp<PhysicsComponent>(uid, out var physics))
            return;

        var (worldPos, worldRot) = _transform.GetWorldPositionRotation(uid);
        var vel = worldRot.ToWorldVec() * component.MaxSpeed;

        if (component.RandomizeVelocity)
        {
            vel = component.DirectionOverride.Degrees switch
            {
                0f => _random.NextVector2(component.MinSpeed, component.MaxSpeed),
                _ => worldRot.RotateVec(component.DirectionOverride.ToVec()) *
                     _random.NextFloat(component.MinSpeed, component.MaxSpeed),
            };
        }

        _physics.ApplyLinearImpulse(uid, vel, body: physics);
        Transform(uid).LocalRotation = (vel - worldPos).ToWorldAngle() + MathHelper.PiOver2;
    }

    protected virtual void OnCollide(Entity<ImmovableRodComponent> ent, ref StartCollideEvent args)
    {
        /*
         * Note for future maintainers: it used to be the case that Immovable Rods (and wizards that polymorphed into
         * one) literally _deleted_ things they hit. As in, they called QueueDel() on them.
         *
         * Do not make the Project Lead write threatening mald PRs:
         * https://github.com/space-wizards/space-station-14/pull/37004
         */

        // Clang!
        if (_random.Prob(ent.Comp.HitSoundProbability))
            _audio.PlayPvs(ent.Comp.HitSound, ent);

        if (TryComp<ImmovableRodComponent>(args.OtherEntity, out var othComp))
            HandleRodCollision(ent, (args.OtherEntity, othComp));
        else if (HasComp<MobStateComponent>(args.OtherEntity))
            HandleMobCollision(ent, args);
        else if (ent.Comp.Damage != null)
            _damageable.TryChangeDamage(args.OtherEntity, ent.Comp.Damage, ignoreResistances: true);
    }

    private void HandleRodCollision(Entity<ImmovableRodComponent> rod, Entity<ImmovableRodComponent> target)
    {
        if (rod.Comp is { SpawnOnRodCollision: not null, HasCollidedWithRod: false } && !target.Comp.HasCollidedWithRod)
        {
            var coords = Transform(target).Coordinates;
            var popup = Loc.GetString(rod.Comp.OnRodCollisionPopup);

            _popup.PopupCoordinates(popup, coords, PopupType.LargeCaution);

            PredictedSpawnAtPosition(rod.Comp.SpawnOnRodCollision, coords);
        }

        rod.Comp.HasCollidedWithRod = true;
        target.Comp.HasCollidedWithRod = true;

        QueueDel(rod);
        QueueDel(target);
    }

    private void HandleMobCollision(Entity<ImmovableRodComponent> ent, StartCollideEvent args)
    {
        /*
         * TODO: Immovable rods are weird. Ergo, always only collide with one fixture on a mob.
         */
        if (args.OtherFixtureId != ent.Comp.MobCollisionFixtureId)
            return;

        var entityHitByRod = args.OtherEntity;

        var popup = Loc.GetString(ent.Comp.OnMobCollisionPopup, ("rod", ent), ("mob", entityHitByRod));
        _popup.PopupEntity(popup, ent, PopupType.LargeCaution);

        ent.Comp.MobCount++;

        if (ent.Comp.Damage != null)
            _damageable.TryChangeDamage(entityHitByRod, ent.Comp.Damage, ignoreResistances: true);

        if (ent.Comp.StaminaDamage > 0)
            _stamina.TakeStaminaDamage(entityHitByRod, ent.Comp.StaminaDamage, ignoreResist: true);
    }

    private void OnExamined(EntityUid uid, ImmovableRodComponent component, ExaminedEvent args)
    {
        args.PushText(
            component.MobCount > 0
            ? Loc.GetString("immovable-rod-consumed-souls", ("rod", uid), ("amount", component.MobCount))
            : Loc.GetString("immovable-rod-consumed-none", ("rod", uid))
        );
    }
}
