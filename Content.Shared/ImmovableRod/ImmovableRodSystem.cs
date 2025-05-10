using Content.Shared.Body.Systems;
using Content.Shared.Body.Components;
using Content.Shared.Damage;
using Content.Shared.Examine;
using Content.Shared.Mobs.Components;
using Content.Shared.Popups;
using Content.Shared.Whitelist;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Physics.Components;
using Robust.Shared.Physics.Events;
using Robust.Shared.Physics.Systems;
using Robust.Shared.Random;
using Content.Shared.Movement.Systems;
using Content.Shared.Damage.Components;
using Content.Shared.Damage.Systems;

namespace Content.Shared.ImmovableRod;

public abstract class SharedImmovableRodSystem : EntitySystem
{
    [Dependency] private readonly IRobustRandom _random = default!;

    [Dependency] private readonly SharedBodySystem _bodySystem = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly SharedPhysicsSystem _physics = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly DamageableSystem _damageable = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly SharedMapSystem _map = default!;
    [Dependency] private readonly EntityWhitelistSystem _whitelist = default!;

    [Dependency] private readonly StaminaSystem _stamina = default!;

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        // we are deliberately including paused entities. rod hungers for all
        foreach (var (rod, trans) in EntityManager.EntityQuery<ImmovableRodComponent, TransformComponent>(true))
        {
            if (!rod.DestroyTiles)
                continue;

            if (!TryComp<MapGridComponent>(trans.GridUid, out var grid))
                continue;

            _map.SetTile(trans.GridUid.Value, grid, trans.Coordinates, Tile.Empty);
        }
    }

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ImmovableRodComponent, StartCollideEvent>(OnCollide);
        SubscribeLocalEvent<ImmovableRodComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<ImmovableRodComponent, ExaminedEvent>(OnExamined);
    }

    private void OnMapInit(EntityUid uid, ImmovableRodComponent component, MapInitEvent args)
    {
        if (!EntityManager.TryGetComponent(uid, out PhysicsComponent? phys))
            return;

        _physics.SetLinearDamping(uid, phys, 0f);
        _physics.SetFriction(uid, phys, 0f);
        _physics.SetBodyStatus(uid, phys, BodyStatus.InAir);

        var xform = Transform(uid);
        var (worldPos, worldRot) = _transform.GetWorldPositionRotation(uid);
        var vel = worldRot.ToWorldVec() * component.MaxSpeed;

        if (component.RandomizeVelocity)
        {
            vel = component.DirectionOverride.Degrees switch
            {
                0f => _random.NextVector2(component.MinSpeed, component.MaxSpeed),
                _ => worldRot.RotateVec(component.DirectionOverride.ToVec()) * _random.NextFloat(component.MinSpeed, component.MaxSpeed)
            };
        }

        _physics.ApplyLinearImpulse(uid, vel, body: phys);
        xform.LocalRotation = (vel - worldPos).ToWorldAngle() + MathHelper.PiOver2;
    }

    protected virtual void OnCollide(EntityUid uid, ImmovableRodComponent component, ref StartCollideEvent args)
    {
        var ent = args.OtherEntity;

        // Clang!
        if (_random.Prob(component.HitSoundProbability))
            _audio.PlayPvs(component.Sound, uid);

        // Oh nyo.
        if (HasComp<ImmovableRodComponent>(ent))
            HandleRodRodCollision(uid, ent);
        // Mobs never automatically get deleted by Rodney, because being instantly deleted is not any fun.
        else if (TryComp<MobStateComponent>(ent, out var mob))
            HandleRodMobCollision(uid, component, args);
        // Admiral Roddington XIV may have been limited in his desire to delete everythng.
        // What's the point of an immovable rod if it deletes all those organs it's just splattered?
        else if (component.ShouldDelete && !_whitelist.IsWhitelistPass(component.DeletionBlacklist, ent))
            QueueDel(ent);
        // The Amulet of Yendor has powers some may deem... unnatural. Clang!
        else if (component.Damage != null)
            _damageable.TryChangeDamage(ent, component.Damage, ignoreResistances: true);
    }

    private void HandleRodRodCollision(EntityUid uid, EntityUid ent)
    {
        // :panik:
        var coords = Transform(uid).Coordinates;
        _popup.PopupCoordinates(Loc.GetString("immovable-rod-collided-rod-not-good"), coords, PopupType.LargeCaution);

        // :kalm:
        Del(uid);
        Del(ent);

        // :panik:
        Spawn("Singularity", coords);
    }

    private void HandleRodMobCollision(EntityUid uid, ImmovableRodComponent component, StartCollideEvent args)
    {
        var ent = args.OtherEntity;

        // TODO: Immovable rods are weird because they're projectiles that don't get consumed after they collide.
        // We could store who the projectile has already hit, but prediction makes that non-trivial, and I've
        // not found a good example in the codebase that mimics the rod's desired behaviour.
        // Temporary hack: use the same collision layer that MobCollision uses.
        if (args.OtherFixtureId != "flammable")
            return;

        // Ma'am, this is a Christian Minecraft server.
        _popup.PopupEntity(Loc.GetString("immovable-rod-penetrated-mob", ("rod", uid), ("mob", ent)), uid, PopupType.LargeCaution);
        component.MobCount++;

        if (component.ShouldGib && TryComp<BodyComponent>(ent, out var body))
        {
            _bodySystem.GibBody(ent, body: body);

            return;
        }

        if (component.Damage != null)
            _damageable.TryChangeDamage(ent, component.Damage, ignoreResistances: true);

        if (component.StaminaDamage > 0 && TryComp<StaminaComponent>(ent, out var stamComp))
            _stamina.TakeStaminaDamage(ent, component.StaminaDamage, stamComp, ignoreResist: true);
    }

    private void OnExamined(EntityUid uid, ImmovableRodComponent component, ExaminedEvent args)
    {
        if (component.MobCount == 0)
            args.PushText(Loc.GetString("immovable-rod-consumed-none", ("rod", uid)));
        else
            args.PushText(Loc.GetString("immovable-rod-consumed-souls", ("rod", uid), ("amount", component.MobCount)));
    }
}
