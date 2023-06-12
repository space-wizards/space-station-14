using Content.Server.Body.Systems;
using Content.Server.Popups;
using Content.Server.Weapons.Ranged.Systems;
using Content.Shared.Body.Components;
using Content.Shared.Examine;
using Content.Shared.Popups;
using Robust.Shared.Audio;
using Robust.Shared.Map;
using Robust.Shared.Physics.Components;
using Robust.Shared.Physics.Events;
using Robust.Shared.Physics.Systems;
using Robust.Shared.Player;
using Robust.Shared.Random;

namespace Content.Server.ImmovableRod;

public sealed class ImmovableRodSystem : EntitySystem
{
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly IMapManager _map = default!;

    [Dependency] private readonly BodySystem _bodySystem = default!;
    [Dependency] private readonly PopupSystem _popup = default!;
    [Dependency] private readonly SharedPhysicsSystem _physics = default!;
    [Dependency] private readonly GunSystem _gun = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        // we are deliberately including paused entities. rod hungers for all
        foreach (var (rod, trans) in EntityManager.EntityQuery<ImmovableRodComponent, TransformComponent>(true))
        {
            if (!rod.DestroyTiles)
                continue;

            if (!_map.TryGetGrid(trans.GridUid, out var grid))
                continue;

            grid.SetTile(trans.Coordinates, Tile.Empty);
        }
    }

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ImmovableRodComponent, StartCollideEvent>(OnCollide);
        SubscribeLocalEvent<ImmovableRodComponent, ExaminedEvent>(OnExamined);
    }

    public EntityUid SpawnAndLaunch(MapCoordinates coordinates, Vector2 direction, float speed)
    {
        var uid = Spawn("ImmovableRod", coordinates);

        if (TryComp<PhysicsComponent>(uid, out var physics) && TryComp<ImmovableRodComponent>(uid, out var rod))
        {
            _physics.SetLinearDamping(physics, 0f);
            _physics.SetFriction(physics, 0f);
            _physics.SetBodyStatus(physics, BodyStatus.InAir);
            _gun.ShootProjectile(uid, direction, Vector2.Zero, uid, speed: speed);
        }

        return uid;
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

        // gib em
        if (TryComp<BodyComponent>(ent, out var body))
        {
            component.MobCount++;

            _popup.PopupEntity(Loc.GetString("immovable-rod-penetrated-mob", ("rod", uid), ("mob", ent)), uid, PopupType.LargeCaution);
            _bodySystem.GibBody(ent, body: body);
        }

        QueueDel(ent);
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
