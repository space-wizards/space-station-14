using System.Numerics;
using Content.Shared.Body;
using Content.Shared.Disposal.Components;
using Content.Shared.Disposal.Holder;
using Content.Shared.Disposal.Tube;
using Content.Shared.Disposal.Unit;
using Content.Shared.Item;
using Content.Shared.Movement.Events;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Containers;
using Robust.Shared.Network;
using Robust.Shared.Physics.Components;
using Robust.Shared.Physics.Systems;
using Robust.Shared.Timing;

namespace Content.Shared.Disposal.Traversal;

/// <summary>
/// Shared movement logic for player-controlled traversal through disposal-style networks.
/// </summary>
public sealed partial class DisposalTraversalSystem : EntitySystem
{
    [Dependency] private IGameTiming _gameTiming = default!;
    [Dependency] private INetManager _net = default!;
    [Dependency] private SharedAudioSystem _audio = default!;
    [Dependency] private SharedContainerSystem _container = default!;
    [Dependency] private SharedDisposalHolderSystem _disposalHolder = default!;
    [Dependency] private SharedPhysicsSystem _physics = default!;
    [Dependency] private SharedTransformSystem _xform = default!;
    [Dependency] private DisposalTubeSystem _tube = default!;

    /// <inheritdoc/>
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<BeingDisposedComponent, MoveInputEvent>(OnMoveInput);
        SubscribeLocalEvent<DisposalTraversalHolderComponent, EntityTerminatingEvent>(OnHolderTerminating);
    }

    private void OnHolderTerminating(Entity<DisposalTraversalHolderComponent> ent, ref EntityTerminatingEvent args)
    {
        ExitTraversal(ent.AsNullable());
    }

    private void OnMoveInput(Entity<BeingDisposedComponent> ent, ref MoveInputEvent args)
    {
        if (!TryComp<DisposalTraversalHolderComponent>(ent.Comp.Holder, out var holderComp))
            return;

        var holder = ent.Comp.Holder;

        if (!Exists(holderComp.CurrentTube))
        {
            ExitTraversal(holder);
            return;
        }

        var dir = args.Dir;
        if (dir != Direction.Invalid)
        {
            var cameraAngle = args.Entity.Comp.TargetRelativeRotation;
            if (cameraAngle != Angle.Zero)
                dir = (dir.ToAngle() + cameraAngle).GetCardinalDir();
        }

        if (dir != Direction.Invalid && holderComp.CurrentDirection != dir)
            holderComp.NextTube = null;

        holderComp.IsMoving = args.HasDirectionalMovement;

        if (dir != Direction.Invalid)
            holderComp.CurrentDirection = dir;
        else if (!holderComp.IsMoving)
            holderComp.CurrentDirection = Direction.Invalid;

        Dirty(holder, holderComp);
    }

    /// <summary>
    /// Inserts an entity into a traversal holder and enters a traversable segment.
    /// </summary>
    public void Insert(EntityUid entry, EntityUid toInsert, string holderPrototypeId)
    {
        if (_net.IsClient)
            return;

        var tubeCoords = Transform(entry).Coordinates;
        var holder = PredictedSpawnAttachedTo(holderPrototypeId, tubeCoords);

        if (!TryInsert(holder, toInsert))
        {
            PredictedDel(holder);
            return;
        }

        if (!EnterTube(holder, entry))
            ExitTraversal(holder);
    }

    /// <summary>
    /// Attempts to insert an entity into a traversal holder.
    /// </summary>
    public bool TryInsert(EntityUid uid, EntityUid toInsert)
    {
        if (_net.IsClient)
            return false;

        if (!CanInsert(uid, toInsert))
            return false;

        if (!_container.Insert(toInsert, GetOrEnsureContainer(uid)))
            return false;

        if (TryComp<PhysicsComponent>(toInsert, out var physBody))
            _physics.SetCanCollide(toInsert, false, body: physBody);

        return true;
    }

    private bool CanInsert(EntityUid uid, EntityUid toInsert)
    {
        return _container.CanInsert(toInsert, GetOrEnsureContainer(uid)) &&
               (HasComp<ItemComponent>(toInsert) || HasComp<BodyComponent>(toInsert));
    }

    private Container GetOrEnsureContainer(EntityUid uid)
    {
        return _container.EnsureContainer<Container>(uid, nameof(DisposalTraversalHolderComponent));
    }

    /// <summary>
    /// Places a traversal holder into the specified traversable segment.
    /// </summary>
    public bool EnterTube(Entity<DisposalTraversalHolderComponent?> holder, EntityUid to)
    {
        if (!Exists(holder))
            return false;

        if (!Resolve(holder, ref holder.Comp))
            return false;

        if (!HasComp<DisposalTubeComponent>(to))
        {
            Log.Error("Entity without DisposalTubeComponent tried entering a traversal network.");
            return false;
        }

        var container = GetOrEnsureContainer(holder.Owner);
        foreach (var contained in container.ContainedEntities)
        {
            _disposalHolder.AttachEntity(holder.Owner, contained);
        }

        if (TryComp<PhysicsComponent>(holder, out var physBody))
            _physics.SetCanCollide(holder, false, body: physBody);

        holder.Comp.CurrentTube = to;
        holder.Comp.NextTube = null;
        Dirty(holder);

        RaiseArrived((holder.Owner, holder.Comp), to);
        SnapToTube((holder.Owner, holder.Comp), to);
        return true;
    }

    /// <summary>
    /// Removes a traversal holder and releases all contained entities.
    /// </summary>
    public void ExitTraversal(Entity<DisposalTraversalHolderComponent?> ent)
    {
        if (_net.IsClient)
            return;

        if (!Resolve(ent, ref ent.Comp))
            return;

        var container = GetOrEnsureContainer(ent.Owner);
        var containedList = new List<EntityUid>(container.ContainedEntities);
        foreach (var entity in containedList)
        {
            _container.Remove(entity, container, reparent: false, force: true);

            var xform = Transform(entity);
            if (xform.ParentUid == ent.Owner)
                _xform.AttachToGridOrMap(entity, xform);

            _disposalHolder.DetachEntity(entity);

            if (TryComp<PhysicsComponent>(entity, out var physics))
                _physics.WakeBody(entity, body: physics);
        }

        QueueDel(ent);
    }

    public override void Update(float frameTime)
    {
        var query = EntityQueryEnumerator<DisposalTraversalHolderComponent>();
        while (query.MoveNext(out var uid, out var holder))
        {
            if (holder.CurrentTube == null)
                continue;

            UpdateHolderMovement((uid, holder), frameTime);
        }
    }

    private void UpdateHolderMovement(Entity<DisposalTraversalHolderComponent> holder, float frameTime)
    {
        var currentTube = holder.Comp.CurrentTube!.Value;
        var holderEnt = holder.Owner;

        if (holder.Comp.CurrentDirection == Direction.Invalid || !holder.Comp.IsMoving)
        {
            _physics.SetLinearVelocity(holderEnt, Vector2.Zero);
            return;
        }

        var beforeMove = new BeforeDisposalTraversalMoveEvent(holder);
        RaiseLocalEvent(currentTube, ref beforeMove);
        if (beforeMove.Handled)
            return;

        var nextTube = holder.Comp.NextTube ?? FindNextTube(holder, currentTube, holder.Comp.CurrentDirection);
        if (nextTube == null)
        {
            _physics.SetLinearVelocity(holderEnt, Vector2.Zero);
            return;
        }

        var destPos = GetTubeWorldPosition(holder, nextTube.Value);
        var entPos = _xform.GetWorldPosition(holderEnt);
        var diff = destPos - entPos;
        var step = holder.Comp.TraversalSpeed * frameTime;

        if (diff.LengthSquared() <= step * step)
        {
            AdvanceTube(holder, nextTube.Value);
            return;
        }

        _physics.SetLinearVelocity(holderEnt, diff.Normalized() * holder.Comp.TraversalSpeed);
    }

    private EntityUid? FindNextTube(Entity<DisposalTraversalHolderComponent> holder, EntityUid currentTube, Direction direction)
    {
        var next = NextTubeFor(holder.AsNullable(), currentTube, direction);
        if (next == null)
            return null;

        holder.Comp.NextTube = next;
        Dirty(holder);
        return next;
    }

    private void AdvanceTube(Entity<DisposalTraversalHolderComponent> holder, EntityUid to)
    {
        if (_gameTiming.CurTime > holder.Comp.LastTraversalSound + holder.Comp.TraversalSoundDelay)
        {
            holder.Comp.LastTraversalSound = _gameTiming.CurTime;
            _audio.PlayPredicted(holder.Comp.TraversalSound, holder, holder);
        }

        holder.Comp.CurrentTube = to;
        holder.Comp.NextTube = null;
        Dirty(holder);

        RaiseArrived(holder, to);
        SnapToTube(holder, to);
        _physics.SetLinearVelocity(holder, Vector2.Zero);
    }

    private void RaiseArrived(Entity<DisposalTraversalHolderComponent> holder, EntityUid tube)
    {
        var ev = new DisposalTraversalArrivedEvent(holder);
        RaiseLocalEvent(tube, ref ev);
    }

    private void SnapToTube(Entity<DisposalTraversalHolderComponent> holder, EntityUid to)
    {
        var tubePos = Transform(to).Coordinates;
        _xform.SetCoordinates(holder, _xform.WithEntityId(tubePos.Offset(GetTubeOffset(holder, to)), to));
    }

    private Vector2 GetTubeWorldPosition(Entity<DisposalTraversalHolderComponent> holder, EntityUid tube)
    {
        return _xform.GetWorldPosition(tube) + GetTubeOffset(holder, tube);
    }

    private Vector2 GetTubeOffset(Entity<DisposalTraversalHolderComponent> holder, EntityUid tube)
    {
        var ev = new GetDisposalTraversalOffsetEvent(holder);
        RaiseLocalEvent(tube, ref ev);
        return ev.Offset;
    }

    /// <summary>
    /// Finds the next connected traversal segment in the specified direction.
    /// </summary>
    public EntityUid? NextTubeFor(Entity<DisposalTraversalHolderComponent?> holder, Entity<DisposalTubeComponent?> currentTube, Direction direction)
    {
        if (!Resolve(holder, ref holder.Comp) || !Resolve(currentTube, ref currentTube.Comp))
            return null;

        if (!_tube.CanConnect((currentTube.Owner, currentTube.Comp), direction))
            return null;

        foreach (var result in _tube.GetTubesInDirection((currentTube.Owner, currentTube.Comp), direction))
        {
            if (!TryComp<DisposalTubeComponent>(result, out var resultTube))
                continue;

            var ev = new CanDisposalTraverseEvent((holder.Owner, holder.Comp), currentTube, (result, resultTube), direction);
            RaiseLocalEvent(result, ref ev);

            if (!ev.Cancelled)
                return result;
        }

        return null;
    }
}
