using Content.Shared.Atmos;
using Content.Shared.Atmos.EntitySystems;
using Content.Shared.Body.Components;
using Content.Shared.Damage;
using Content.Shared.Disposal.Components;
using Content.Shared.Disposal.Tube;
using Content.Shared.Eye;
using Content.Shared.Follower.Components;
using Content.Shared.Item;
using Content.Shared.Throwing;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Containers;
using Robust.Shared.Map.Components;
using Robust.Shared.Network;
using Robust.Shared.Physics.Components;
using Robust.Shared.Physics.Systems;
using Robust.Shared.Player;

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
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly SharedEyeSystem _eye = default!;

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

        SubscribeLocalEvent<ActorComponent, DisposalSystemTransitionEvent>(OnActorTransition);
        SubscribeLocalEvent<FollowerComponent, DisposalSystemTransitionEvent>(OnFollowerTransition);
        SubscribeLocalEvent<GetVisMaskEvent>(OnGetVisibility);
    }

    private void OnActorTransition(Entity<ActorComponent> ent, ref DisposalSystemTransitionEvent args)
    {
        _eye.RefreshVisibilityMask(ent.Owner);
    }

    private void OnFollowerTransition(Entity<FollowerComponent> ent, ref DisposalSystemTransitionEvent args)
    {
        _eye.RefreshVisibilityMask(ent.Owner);
    }

    private void OnGetVisibility(ref GetVisMaskEvent ev)
    {
        // Prevents mispredictions by allowing clients in the disposal system
        // to be sent any entities that are hidden under subfloors
        if (HasComp<ActorComponent>(ev.Entity) &&
            HasComp<BeingDisposedComponent>(ev.Entity))
        {
            ev.VisibilityMask |= (int)VisibilityFlags.Subfloor;
        }
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

            var ev = new DisposalSystemTransitionEvent();
            RaiseLocalEvent(entity, ref ev);

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
    public bool EnterTube(EntityUid holderUid, EntityUid toUid, DisposalHolderComponent? holder = null, TransformComponent? holderTransform = null, DisposalTubeComponent? to = null)
    {
        if (!Resolve(holderUid, ref holder, ref holderTransform))
            return false;

        if (holder.IsExitingDisposals)
            return false;

        if (!Resolve(toUid, ref to))
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
                Dirty(ent, comp);
            }
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

        // Invalid direction = exit now!
        if (holder.CurrentDirection == Direction.Invalid)
        {
            ExitDisposals(holderUid, holder, holderTransform);
            return false;
        }

        holder.NextTube = _disposalTubeSystem.NextTubeFor(holder.CurrentTube.Value, holder.CurrentDirection);

        var xform = Transform(holderUid);
        var xform2 = Transform(toUid);

        if (xform2.GridUid != null)
        {
            var rotation = _xformSystem.GetWorldRotation(xform2.GridUid.Value);
            xform.LocalRotation = holder.CurrentDirection.ToAngle() - rotation;
        }

        if (holder.Container != null)
        {
            foreach (var ent in holder.Container.ContainedEntities)
            {
                var entryEvent = new DisposalSystemTransitionEvent();
                RaiseLocalEvent(ent, ref entryEvent);

                // damage entities on turns and play sound
                if (holder.CurrentDirection != holder.PreviousDirection)
                {
                    _damageable.TryChangeDamage(ent, to.DamageOnTurn);

                    if (_net.IsServer)
                    {
                        _audio.PlayPvs(to.ClangSound, toUid);
                    }
                }
            }
        }

        Dirty(holderUid, holder);

        return true;
    }

    public override void Update(float frameTime)
    {
        var query = EntityQueryEnumerator<DisposalHolderComponent>();
        while (query.MoveNext(out var uid, out var holder))
        {
            if (holder.Container?.Count == 0)
            {
                PredictedQueueDel(uid);
                continue;
            }

            UpdateComp((uid, holder));
        }
    }

    private void UpdateComp(Entity<DisposalHolderComponent> ent)
    {
        var currentTube = ent.Comp.CurrentTube;
        var nextTube = ent.Comp.NextTube;

        if (currentTube == null || !Exists(currentTube) ||
            nextTube == null || !Exists(nextTube))
        {
            ExitDisposals(ent.Owner, ent.Comp);
            return;
        }

        var gridUid = _xformQuery.GetComponent(currentTube.Value).GridUid;

        if (gridUid == null)
            return;

        var gridRotation = _xformSystem.GetWorldRotation(gridUid.Value);
        var origin = _xformQuery.GetComponent(currentTube.Value).Coordinates;
        var destination = _xformQuery.GetComponent(nextTube.Value).Coordinates;
        var entCoords = _xformQuery.GetComponent(ent).Coordinates;

        var originDestDiff = destination.Position - origin.Position;
        var originEntDiff = entCoords.Position - origin.Position;
        var entDestDiff = destination.Position - entCoords.Position;
        var velocity = gridRotation.RotateVec(entDestDiff.Normalized()) * ent.Comp.TraversalSpeed;

        _physicsSystem.SetLinearVelocity(ent, velocity);

        if (originEntDiff.Length() <= originDestDiff.Length())
            return;

        if (EnterTube(ent.Owner, nextTube!.Value, ent.Comp))
        {
            UpdateComp(ent);
        }
    }
}
