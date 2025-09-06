using Content.Shared.Atmos;
using Content.Shared.Atmos.EntitySystems;
using Content.Shared.Body.Components;
using Content.Shared.Damage;
using Content.Shared.Disposal.Components;
using Content.Shared.Disposal.Tube;
using Content.Shared.Item;
using Content.Shared.Throwing;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Containers;
using Robust.Shared.Map.Components;
using Robust.Shared.Physics.Components;
using Robust.Shared.Physics.Systems;

namespace Content.Shared.Disposal.Unit;

public abstract partial class SharedDisposableSystem : EntitySystem
{
    [Dependency] private readonly ThrowingSystem _throwing = default!;
    [Dependency] private readonly SharedAtmosphereSystem _atmosphereSystem = default!;
    [Dependency] private readonly DamageableSystem _damageable = default!;
    [Dependency] private readonly SharedDisposalUnitSystem _disposalUnitSystem = default!;
    [Dependency] private readonly SharedDisposalTubeSystem _disposalTubeSystem = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedContainerSystem _containerSystem = default!;
    [Dependency] private readonly SharedMapSystem _maps = default!;
    [Dependency] private readonly SharedPhysicsSystem _physicsSystem = default!;
    [Dependency] private readonly SharedTransformSystem _xformSystem = default!;
    [Dependency] private readonly SharedAppearanceSystem _appearanceSystem = default!;

    private EntityQuery<DisposalTubeComponent> _disposalTubeQuery;
    private EntityQuery<DisposalUnitComponent> _disposalUnitQuery;
    private EntityQuery<MetaDataComponent> _metaQuery;
    private EntityQuery<PhysicsComponent> _physicsQuery;
    private EntityQuery<TransformComponent> _xformQuery;

    public override void Initialize()
    {
        base.Initialize();

        _disposalTubeQuery = GetEntityQuery<DisposalTubeComponent>();
        _disposalUnitQuery = GetEntityQuery<DisposalUnitComponent>();
        _metaQuery = GetEntityQuery<MetaDataComponent>();
        _physicsQuery = GetEntityQuery<PhysicsComponent>();
        _xformQuery = GetEntityQuery<TransformComponent>();

        SubscribeLocalEvent<DisposalHolderComponent, ComponentStartup>(OnComponentStartup);
        SubscribeLocalEvent<DisposalHolderComponent, ContainerIsInsertingAttemptEvent>(CanInsert);
        SubscribeLocalEvent<DisposalHolderComponent, EntInsertedIntoContainerMessage>(OnInsert);
    }

    protected virtual void MergeAtmos(Entity<DisposalHolderComponent> ent, GasMixture gasMix)
    {

    }

    private void OnComponentStartup(EntityUid uid, DisposalHolderComponent holder, ComponentStartup args)
    {
        holder.Container = _containerSystem.EnsureContainer<Container>(uid, nameof(DisposalHolderComponent));
    }

    private void CanInsert(Entity<DisposalHolderComponent> ent, ref ContainerIsInsertingAttemptEvent args)
    {
        if (!HasComp<ItemComponent>(args.EntityUid) && !HasComp<BodyComponent>(args.EntityUid))
            args.Cancel();
    }

    private void OnInsert(Entity<DisposalHolderComponent> ent, ref EntInsertedIntoContainerMessage args)
    {
        if (_physicsQuery.TryGetComponent(args.Entity, out var physBody))
            _physicsSystem.SetCanCollide(args.Entity, false, body: physBody);
    }

    public void ExitDisposals(EntityUid uid, DisposalHolderComponent? holder = null, TransformComponent? holderTransform = null)
    {
        if (Terminating(uid))
            return;

        if (!Resolve(uid, ref holder, ref holderTransform))
            return;

        if (holder.IsExitingDisposals)
        {
            //Log.Error("Tried exiting disposals twice. This should never happen.");
            return;
        }

        holder.IsExitingDisposals = true;
        Dirty(uid, holder);

        // Check for a disposal unit to throw them into and then eject them from it.
        // *This ejection also makes the target not collide with the unit.*
        // *This is on purpose.*

        EntityUid? disposalId = null;
        DisposalUnitComponent? duc = null;
        var gridUid = holderTransform.GridUid;
        if (TryComp<MapGridComponent>(gridUid, out var grid))
        {
            foreach (var contentUid in _maps.GetLocal(gridUid.Value, grid, holderTransform.Coordinates))
            {
                if (_disposalUnitQuery.TryGetComponent(contentUid, out duc))
                {
                    disposalId = contentUid;
                    break;
                }
            }
        }

        // We're purposely iterating over all the holder's children
        // because the holder might have something teleported into it,
        // outside the usual container insertion logic.
        var children = holderTransform.ChildEnumerator;
        while (children.MoveNext(out var entity))
        {
            RemComp<BeingDisposedComponent>(entity);

            var meta = _metaQuery.GetComponent(entity);
            if (holder.Container != null && holder.Container.Contains(entity))
                _containerSystem.Remove((entity, null, meta), holder.Container, reparent: false, force: true);

            var xform = _xformQuery.GetComponent(entity);
            if (xform.ParentUid != uid)
                continue;

            if (duc != null)
                _containerSystem.Insert((entity, xform, meta), duc.Container);
            else
            {
                _xformSystem.AttachToGridOrMap(entity, xform);
                var direction = holder.CurrentDirection == Direction.Invalid ? holder.PreviousDirection : holder.CurrentDirection;

                if (direction != Direction.Invalid && _xformQuery.TryGetComponent(gridUid, out var gridXform))
                {
                    var directionAngle = direction.ToAngle();
                    directionAngle += _xformSystem.GetWorldRotation(gridXform);
                    _throwing.TryThrow(entity, directionAngle.ToWorldVec() * 3f, 10f);
                }
            }
        }

        if (disposalId != null && duc != null)
        {
            _disposalUnitSystem.TryEjectContents(disposalId.Value, duc);
        }

        MergeAtmos((uid, holder), holder.Air);

        PredictedDel(uid);
    }

    // Note: This function will cause an ExitDisposals on any failure that does not make an ExitDisposals impossible.
    public bool EnterTube(EntityUid holderUid, EntityUid toUid, DisposalHolderComponent? holder = null, TransformComponent? holderTransform = null, DisposalTubeComponent? to = null, TransformComponent? toTransform = null)
    {
        if (!Resolve(holderUid, ref holder, ref holderTransform))
            return false;
        if (holder.IsExitingDisposals)
        {
            //Log.Error("Tried entering tube after exiting disposals. This should never happen.");
            return false;
        }
        if (!Resolve(toUid, ref to, ref toTransform))
        {
            ExitDisposals(holderUid, holder, holderTransform);
            return false;
        }

        if (holder.Container != null)
        {
            foreach (var ent in holder.Container.ContainedEntities)
            {
                var comp = EnsureComp<BeingDisposedComponent>(ent);
                comp.Holder = holderUid;
            }
        }

        // Insert into next tube
        if (to.Contents != null && !_containerSystem.Insert(holderUid, to.Contents))
        {
            ExitDisposals(holderUid, holder, holderTransform);
            return false;
        }

        if (holder.CurrentTube != null)
        {
            holder.PreviousTube = holder.CurrentTube;
            holder.PreviousDirection = holder.CurrentDirection;
        }

        holder.CurrentTube = toUid;

        var ev = new GetDisposalsNextDirectionEvent(holder);
        RaiseLocalEvent(toUid, ref ev);

        holder.CurrentDirection = ev.Next;
        holder.StartingTime = holder.TraversalTime;
        holder.TimeLeft = holder.TraversalTime;
        // Logger.InfoS("c.s.disposal.holder", $"Disposals dir {holder.CurrentDirection}");
        Dirty(holderUid, holder);

        // Invalid direction = exit now!
        if (holder.CurrentDirection == Direction.Invalid)
        {
            ExitDisposals(holderUid, holder, holderTransform);
            return false;
        }

        var xform = Transform(holderUid);
        var parentXform = Transform(xform.ParentUid);
        xform.LocalRotation = holder.CurrentDirection.ToAngle() - parentXform.LocalRotation;

        // damage entities on turns and play sound
        if (holder.Container != null && holder.CurrentDirection != holder.PreviousDirection)
        {
            foreach (var ent in holder.Container.ContainedEntities)
            {
                _damageable.TryChangeDamage(ent, to.DamageOnTurn);
            }
            _audio.PlayPvs(to.ClangSound, toUid);
        }

        return true;
    }

    public override void Update(float frameTime)
    {
        var query = EntityQueryEnumerator<DisposalHolderComponent>();
        while (query.MoveNext(out var uid, out var holder))
        {
            UpdateComp(uid, holder, frameTime);
        }
    }

    private void UpdateComp(EntityUid uid, DisposalHolderComponent holder, float frameTime)
    {
        while (frameTime > 0)
        {
            var time = frameTime;
            if (time > holder.TimeLeft)
            {
                time = holder.TimeLeft;
            }

            holder.TimeLeft -= time;
            frameTime -= time;
            Dirty(uid, holder);

            if (!Exists(holder.CurrentTube))
            {
                ExitDisposals(uid, holder);
                break;
            }

            var currentTube = holder.CurrentTube!.Value;
            if (holder.TimeLeft > 0)
            {
                var progress = 1 - holder.TimeLeft / holder.StartingTime;
                var origin = _xformQuery.GetComponent(currentTube).Coordinates;
                var destination = holder.CurrentDirection.ToVec();
                var newPosition = destination * progress;

                // This is some supreme shit code.
                _xformSystem.SetCoordinates(uid, _xformSystem.WithEntityId(origin.Offset(newPosition), currentTube));
                continue;
            }

            // Past this point, we are performing inter-tube transfer!
            // Remove current tube content
            var tubeQuery = _disposalTubeQuery.GetComponent(currentTube);

            if (tubeQuery.Contents != null)
                _containerSystem.Remove(uid, tubeQuery.Contents, reparent: false);

            // Find next tube
            var nextTube = _disposalTubeSystem.NextTubeFor(currentTube, holder.CurrentDirection);
            if (!Exists(nextTube))
            {
                ExitDisposals(uid, holder);
                break;
            }

            // Perform remainder of entry process
            if (!EnterTube(uid, nextTube!.Value, holder))
            {
                break;
            }
        }
    }
}
