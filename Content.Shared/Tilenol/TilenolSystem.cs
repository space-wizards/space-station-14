using System.Numerics;
using Content.Shared.Doors.Components;
using Content.Shared.Doors.Systems;
using Content.Shared.Interaction;
using Content.Shared.Maps;
using Content.Shared.Movement.Components;
using Content.Shared.Movement.Systems;
using Content.Shared.Physics;
using Content.Shared.Tag;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Components;
using Robust.Shared.Physics.Systems;
using Robust.Shared.Timing;
using Robust.Shared.Utility;

namespace Content.Shared.Tilenol;

public sealed class TilenolSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly SharedMoverController _mover = default!;
    [Dependency] private readonly SharedPhysicsSystem _physics = default!;
    [Dependency] private readonly SharedInteractionSystem _interaction = default!;
    [Dependency] private readonly EntityLookupSystem _lookup = default!;
    [Dependency] private readonly SharedMapSystem _map = default!;
    [Dependency] private readonly SharedDoorSystem _door = default!;
    [Dependency] private readonly TagSystem _tags = default!;

    private EntityQuery<MobMoverComponent> _mobQ;
    private EntityQuery<NoRotateOnMoveComponent> _rotQ;
    private EntityQuery<Tilenol.TilenolComponent> _tileQ;
    private EntityQuery<MovementSpeedModifierComponent> _speedQ;
    private EntityQuery<MapGridComponent> _gridQ;
    private EntityQuery<DoorComponent> _doorQ;
    private EntityQuery<FixturesComponent> _fixtureQ;
    private EntityQuery<PhysicsComponent> _physicsQ;

    private TimeSpan CurTime => _physics.EffectiveCurTime ?? _timing.CurTime;

    public override void Initialize()
    {
        base.Initialize();

        _mobQ = GetEntityQuery<MobMoverComponent>();
        _speedQ = GetEntityQuery<MovementSpeedModifierComponent>();
        _rotQ = GetEntityQuery<NoRotateOnMoveComponent>();
        _tileQ = GetEntityQuery<Tilenol.TilenolComponent>();
        _gridQ = GetEntityQuery<MapGridComponent>();
        _doorQ = GetEntityQuery<DoorComponent>();
        _fixtureQ = GetEntityQuery<FixturesComponent>();
        _physicsQ = GetEntityQuery<PhysicsComponent>();

        SubscribeLocalEvent<Tilenol.TilenolComponent, EntParentChangedMessage>(OnParentChange);
    }

    private void OnParentChange(EntityUid uid, Tilenol.TilenolComponent component, ref EntParentChangedMessage args)
    {
        if (TryComp(uid, out InputMoverComponent? mover))
            EndSlide(component, uid, mover);
        else
        {
            component.LastSlideEnd = CurTime;
            component.SlideStart = null;
            component.Origin = default;
            component.Destination = default;
            Dirty(uid, component);
        }
    }

    public bool HandleTilenol(EntityUid uid,
        EntityUid physicsUid,
        PhysicsComponent physicsComponent,
        TransformComponent xform,
        InputMoverComponent mover,
        ContentTileDefinition? tileDef,
        MovementRelayTargetComponent? relayTarget)
    {
        if (!_tileQ.TryComp(physicsUid, out var tilenol))
            return false;

        var immediateDir = _mover.DirVecForButtons(mover.HeldMoveButtons);
        var (walkDir, sprintDir) = mover.Sprinting ? (Vector2.Zero, immediateDir) : (immediateDir, Vector2.Zero);
        var moveSpeedComponent = _speedQ.CompOrNull(uid);
        var walkSpeed = moveSpeedComponent?.CurrentWalkSpeed ?? MovementSpeedModifierComponent.DefaultBaseWalkSpeed;
        var sprintSpeed = moveSpeedComponent?.CurrentSprintSpeed ?? MovementSpeedModifierComponent.DefaultBaseSprintSpeed;
        var total = walkDir * walkSpeed + sprintDir * sprintSpeed;

        _physics.SetLinearVelocity(physicsUid, Vector2.Zero, body: physicsComponent);
        _physics.SetAngularVelocity(physicsUid, 0, body: physicsComponent);
        if (total == Vector2.Zero && !tilenol.IsSliding)
        {
            TilenolSnap(physicsUid, mover);
            return true;
        }

        if (!_rotQ.HasComponent(uid))
        {
            if (!tilenol.IsSliding)
            {
                var parentRotation = _mover.GetParentGridAngle(mover);
                var worldTotal = _mover._relativeMovement ? parentRotation.RotateVec(total) : total;
                var worldRot = _transform.GetWorldRotation(xform);
                _transform.SetLocalRotation(xform, xform.LocalRotation + worldTotal.ToWorldAngle() - worldRot);
            }
            else if (TryComp(mover.RelativeEntity, out TransformComponent? rel))
            {
                var delta = tilenol.Destination - tilenol.Origin.Position;
                var worldRot = _transform.GetWorldRotation(rel).RotateVec(delta).ToWorldAngle();
                _transform.SetWorldRotation(xform, worldRot);
            }
        }

        if (_mobQ.TryGetComponent(uid, out var mobMover) &&
            _mover.TryGetSound(false, uid, mover, mobMover, xform, out var sound, tileDef: tileDef))
        {
            var soundModifier = mover.Sprinting ? 3.5f : 1.5f;
            var audioParams = sound.Params
                .WithVolume(sound.Params.Volume + soundModifier)
                .WithVariation(sound.Params.Variation ?? SharedMoverController.FootstepVariation);
            _audio.PlayPredicted(sound, uid, relayTarget?.Source ?? uid, audioParams);
        }

        if (tilenol.IsSliding)
        {
            UpdateSlide(tilenol, physicsUid, mover);
            return  true;
        }

        StartSlide(tilenol, physicsUid, total, mover);
        return  true;
    }

    private void TilenolSnap(EntityUid uid, InputMoverComponent mover)
    {
        if (TryComp(mover.RelativeEntity, out TransformComponent? rel))
            TilenolSnap((uid, Transform(uid)), (mover.RelativeEntity.Value, rel));
    }

    private EntityCoordinates TilenolSnap(Entity<TransformComponent> ent, Entity<TransformComponent> grid)
    {
        var local = ent.Comp.Coordinates.WithEntityId(grid.Owner, _transform, EntityManager);
        var pos = local.Position;
        var x = (int)Math.Floor(pos.X) + 0.5f;
        var y = (int)Math.Floor(pos.Y) + 0.5f;
        var newPos = new EntityCoordinates(local.EntityId, x, y);

        if (!CanBeOnTile(ent, grid, newPos))
            newPos = Fuck(ent, grid, newPos);

        if (!pos.EqualsApprox(newPos.Position))
            SetCoordinates(ent.Owner, newPos);

        _physics.WakeBody(ent); // no velocity --> have to manually wake body for collisions
        return newPos;
    }

    private void SetCoordinates(EntityUid uid, EntityCoordinates newPos)
    {
        // Fun fact, SetCoordiantes in TransformSystem does not do any lerping
        // AAAAAAAAAAAHHHHHHHHHHHHHHHHHHHHHHHHAAAAAAAAAAAAAAAUUUUUUUUUUUUGHEYYYYYYYY

        var xform = Transform(uid);

        if (!xform.ParentUid.IsValid())
            return;

        var local = newPos.WithEntityId(xform.ParentUid, _transform, EntityManager).Position;
        _transform.SetLocalPosition(uid, local, xform);
    }

    private bool CanBeOnTile(Entity<TransformComponent> mover, Entity<TransformComponent> grid, EntityCoordinates newPos)
    {
        if (!_gridQ.TryGetComponent(grid.Owner, out var gridComp))
            return true;

        DebugTools.Assert(grid.Owner == newPos.EntityId);
        var tile = _map.TileIndicesFor(newPos.EntityId, gridComp, newPos);
        var enumerator = _map.GetAnchoredEntitiesEnumerator(newPos.EntityId, gridComp, tile);
        int moverLayer = 0, moverMask = 0;

        if (_physicsQ.TryComp(mover, out var phys) && phys.CanCollide)
            (moverLayer, moverMask) = _physics.GetHardCollision(mover);

        while (enumerator.MoveNext(out var anchored))
        {
            if (_tags.HasTag(anchored.Value, "Wall"))
                return false;

            if (!_physicsQ.TryComp(anchored, out var anchoredPhys) || !anchoredPhys.CanCollide)
                continue;

            if (!_fixtureQ.TryComp(anchored, out var fixture))
                continue;

            var (anchoredLayer, anchoredMask) = _physics.GetHardCollision(anchored.Value, fixture);

            if ((anchoredLayer & moverMask) != 0)
                return false;

            if ((anchoredMask & moverLayer) != 0)
                return false;
        }

        return true;
    }

    private static Direction[] _panicDirections = new Direction[]
    {
        Direction.North,
        Direction.East,
        Direction.South,
        Direction.West,
        Direction.NorthEast,
        Direction.SouthEast,
        Direction.SouthWest,
        Direction.NorthWest,
    };

    // Aka: the door closed, move out of the way.
    private EntityCoordinates Fuck(Entity<TransformComponent> mover, Entity<TransformComponent> grid, EntityCoordinates pos)
    {
        foreach (var dir in _panicDirections)
        {
            var offset = dir.ToIntVec();
            var newPos = new EntityCoordinates(pos.EntityId, pos.Position + offset);
            if (CanBeOnTile(mover, grid, newPos))
                return newPos;
        }

        // Shit.. just shuffle them constantly northwards I guess
        var aaaaa = new EntityCoordinates(pos.EntityId, pos.Position + new Vector2(1,0));
        return Fuck(mover, grid, aaaaa);
    }

    private void StartSlide(Tilenol.TilenolComponent tilenol, EntityUid uid, Vector2 total, InputMoverComponent mover)
    {
        if (!_timing.InSimulation)
            return;

        if (!TryComp(mover.RelativeEntity, out TransformComponent? rel))
            return;

        var curPos = TilenolSnap((uid, Transform(uid)), (mover.RelativeEntity.Value, rel));

        if (tilenol.LastSlideEnd is { } end && CurTime - end < tilenol.SlideDelay)
            return;

        if (_mover._relativeMovement)
            total = mover.RelativeRotation.RotateVec(total);

        var speed = MathF.Max(0.1f, total.Length() * tilenol.SlideSpeed);
        var dir = Angle.FromWorldVec(total).GetDir();
        var offset = dir.ToIntVec();
        var time = offset.Length / speed;

        tilenol.LastSlideEnd = null;
        Dirty(uid, tilenol);

        if (!CanSlideInThatDirection(uid, curPos, offset))
        {
            TryDoorBump(uid, curPos, offset);
            return;
        }

        tilenol.Origin = curPos;
        tilenol.Destination = curPos.Position + offset;
        tilenol.SlideDuration = TimeSpan.FromSeconds(time);
        tilenol.SlideStart = CurTime;
    }

    private void TryDoorBump(EntityUid uid, EntityCoordinates curPos, Vector2i offset)
    {
        if (!_tags.HasTag(uid, SharedDoorSystem.DoorBumpTag))
            return;

        if (!_gridQ.TryGetComponent(curPos.EntityId, out var grid))
            return;

        var newPos = new EntityCoordinates(curPos.EntityId, curPos.Position + offset);

        var tile = _map.TileIndicesFor(curPos.EntityId, grid, newPos);
        var enumerator = _map.GetAnchoredEntitiesEnumerator(curPos.EntityId, grid, tile);
        while (enumerator.MoveNext(out var anchored))
        {
            if (_doorQ.TryGetComponent(anchored, out var door))
                _door.TryOpen(anchored.Value, door, uid, quiet: door.State == DoorState.Denying);
        }
    }

    private bool CanSlideInThatDirection(EntityUid uid, EntityCoordinates curPos, Vector2i offset)
    {
        var newPos = new EntityCoordinates(curPos.EntityId, curPos.Position + offset);
        var mask = (CollisionGroup) _physics.GetHardCollision(uid).Mask;
        return _interaction.InRangeUnobstructed(uid, newPos, collisionMask:mask);
    }

    private void UpdateSlide(Tilenol.TilenolComponent tilenol, EntityUid uid, InputMoverComponent mover)
    {
        var progress = (float)((CurTime - tilenol.SlideStart!.Value)/tilenol.SlideDuration);
        progress = Math.Clamp(progress, 0f, 1f);
        var pos = LerpSlide(tilenol.Origin, tilenol.Destination, progress, tilenol.LinearInterp);
        SetCoordinates(uid, pos);

        _physics.WakeBody(uid); // no velocity --> have to manually wake body for collisions
        if (progress >= 0.99f && _timing.InSimulation)
            EndSlide(tilenol, uid, mover);
    }

    private void EndSlide(Tilenol.TilenolComponent tilenol, EntityUid uid, InputMoverComponent mover)
    {
        tilenol.LastSlideEnd = CurTime;
        tilenol.SlideStart = null;
        tilenol.Origin = default;
        tilenol.Destination = default;
        TilenolSnap(uid, mover);
        Dirty(uid, tilenol);
    }

    private EntityCoordinates LerpSlide(EntityCoordinates a, Vector2 b, float progress, bool linear)
    {
        if (!linear)
        {
            // Speed goes like (1-cos(2*pi*x)
            // I.e., start slow, then speed up, before slowing down again.
            // Get position by integrating function
            progress = progress - MathF.Sin(MathF.Tau * progress) / MathF.Tau;
        }
        var c = (b - a.Position)*progress + a.Position;
        return new(a.EntityId, c);
    }
}
