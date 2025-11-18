using Content.Server.Body.Systems;
using Content.Server.Destructible;
using Content.Server.Polymorph.Components;
using Content.Server.Popups;
using Content.Shared.Body.Components;
using Content.Shared.Damage.Systems;
using Content.Shared.Examine;
using Content.Shared.Popups;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Physics.Components;
using Robust.Shared.Physics.Events;
using Robust.Shared.Physics.Systems;
using Robust.Shared.Random;

namespace Content.Server.ImmovableRod;

public sealed class ImmovableRodSystem : EntitySystem
{
    [Dependency] private readonly IRobustRandom _random = default!;

    [Dependency] private readonly BodySystem _bodySystem = default!;
    [Dependency] private readonly PopupSystem _popup = default!;
    [Dependency] private readonly SharedPhysicsSystem _physics = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly DamageableSystem _damageable = default!;
    [Dependency] private readonly DestructibleSystem _destructible = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly SharedMapSystem _map = default!;

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        // we are deliberately including paused entities. rod hungers for all
        foreach (var (rod, trans) in EntityQuery<ImmovableRodComponent, TransformComponent>(true))
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
        if (TryComp(uid, out PhysicsComponent? phys))
        {
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
    }

    private void OnCollide(EntityUid uid, ImmovableRodComponent component, ref StartCollideEvent args)
    {
        var ent = args.OtherEntity;

        if (_random.Prob(component.HitSoundProbability))
        {
            _audio.PlayPvs(component.Sound, uid);
        }

        if (HasComp<ImmovableRodComponent>(ent))
        {
            // oh god.
            var coords = Transform(uid).Coordinates;
            _popup.PopupCoordinates(Loc.GetString("immovable-rod-collided-rod-not-good"), coords, PopupType.LargeCaution);

            Del(uid);
            Del(ent);
            Spawn("Singularity", coords);

            return;
        }

        // dont delete/hurt self if polymoprhed into a rod
        if (TryComp<PolymorphedEntityComponent>(uid, out var polymorphed))
        {
            if (polymorphed.Parent == ent)
                return;
        }

        // gib or damage em
        if (TryComp<BodyComponent>(ent, out var body))
        {
            component.MobCount++;
            _popup.PopupEntity(Loc.GetString("immovable-rod-penetrated-mob", ("rod", uid), ("mob", ent)), uid, PopupType.LargeCaution);

            if (!component.ShouldGib)
            {
                if (component.Damage == null)
                    return;

                _damageable.TryChangeDamage(ent, component.Damage, ignoreResistances: true);
                return;
            }

            _bodySystem.GibBody(ent, body: body);
            return;
        }

        _destructible.DestroyEntity(ent);
    }

    private void OnExamined(EntityUid uid, ImmovableRodComponent component, ExaminedEvent args)
    {
        if (component.MobCount == 0)
        {
            args.PushText(Loc.GetString("immovable-rod-consumed-none", ("rod", uid)));
        }
        else
        {
            args.PushText(Loc.GetString("immovable-rod-consumed-souls", ("rod", uid), ("amount", component.MobCount)));
        }
    }
}
