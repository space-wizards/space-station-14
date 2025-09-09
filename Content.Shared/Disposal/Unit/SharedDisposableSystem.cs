using Content.Shared.Atmos;
using Content.Shared.Atmos.EntitySystems;
using Content.Shared.Body.Components;
using Content.Shared.Damage;
using Content.Shared.Disposal.Components;
using Content.Shared.Disposal.Tube;
using Content.Shared.Eye;
using Content.Shared.Follower;
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

    public void ExitDisposals(Entity<DisposalHolderComponent> ent)
    {
        if (Terminating(ent))
            return;

        if (ent.Comp.IsExitingDisposals)
            return;

        ent.Comp.IsExitingDisposals = true;
        Dirty(ent);

        // Check for a disposal unit to throw them into and then eject them from it.
        // *This ejection also makes the target not collide with the unit.*
        // *This is on purpose.*

        EntityUid? disposalId = null;
        DisposalUnitComponent? duc = null;
        var xform = Transform(ent);
        var gridUid = xform.GridUid;

        if (TryComp<MapGridComponent>(gridUid, out var grid))
        {
            foreach (var contentUid in _maps.GetLocal(gridUid.Value, grid, xform.Coordinates))
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
        var children = xform.ChildEnumerator;
        while (children.MoveNext(out var held))
        {
            DetachEntityFromDisposalHolder(held);

            var meta = _metaQuery.GetComponent(held);
            if (ent.Comp.Container != null && ent.Comp.Container.Contains(held))
                _containerSystem.Remove((held, null, meta), ent.Comp.Container, reparent: false, force: true);

            var heldXform = _xformQuery.GetComponent(held);
            if (heldXform.ParentUid != ent.Owner)
                continue;

            if (duc != null)
                _containerSystem.Insert((held, heldXform, meta), duc.Container);
            else
            {
                _xformSystem.AttachToGridOrMap(held, heldXform);
                var direction = ent.Comp.CurrentDirection == Direction.Invalid ? ent.Comp.PreviousDirection : ent.Comp.CurrentDirection;

                if (direction != Direction.Invalid && _xformQuery.TryGetComponent(gridUid, out var gridXform))
                {
                    var directionAngle = direction.ToAngle();
                    directionAngle += _xformSystem.GetWorldRotation(gridXform);
                    _throwing.TryThrow(held, directionAngle.ToWorldVec() * 3f, 10f);
                }
            }
        }

        if (disposalId != null && duc != null)
        {
            _disposalUnitSystem.TryEjectContents(disposalId.Value, duc);
        }

        MergeAtmos(ent, ent.Comp.Air);

        PredictedDel(ent.Owner);
    }

    // Note: This function will cause an ExitDisposals on any failure that does not make an ExitDisposals impossible.
    public bool EnterTube(Entity<DisposalHolderComponent> ent, Entity<DisposalTubeComponent> tube)
    {
        if (ent.Comp.IsExitingDisposals)
            return false;

        if (ent.Comp.Container != null)
        {
            foreach (var held in ent.Comp.Container.ContainedEntities)
            {
                AttachEntityToDisposalHolder(ent, held);
            }
        }

        if (ent.Comp.CurrentTube != null)
        {
            ent.Comp.PreviousTube = ent.Comp.CurrentTube;
            ent.Comp.PreviousDirection = ent.Comp.CurrentDirection;
        }

        ent.Comp.CurrentTube = tube;

        var ev = new GetDisposalsNextDirectionEvent(ent.Comp);
        RaiseLocalEvent(tube, ref ev);

        ent.Comp.CurrentDirection = ev.Next;

        // Invalid direction = exit now!
        if (ent.Comp.CurrentDirection == Direction.Invalid)
        {
            ExitDisposals(ent);
            return false;
        }

        ent.Comp.NextTube = _disposalTubeSystem.NextTubeFor(ent.Comp.CurrentTube.Value, ent.Comp.CurrentDirection);

        var xform = Transform(ent);
        var xform2 = Transform(tube);

        if (xform2.GridUid != null)
        {
            var rotation = _xformSystem.GetWorldRotation(xform2.GridUid.Value);
            xform.LocalRotation = ent.Comp.CurrentDirection.ToAngle() - rotation;
        }

        // damage entities on turns and play sound
        if (ent.Comp.Container != null && ent.Comp.CurrentDirection != ent.Comp.PreviousDirection)
        {
            foreach (var held in ent.Comp.Container.ContainedEntities)
            {
                _damageable.TryChangeDamage(held, tube.Comp.DamageOnTurn);
            }

            if (_net.IsServer)
            {
                _audio.PlayPvs(tube.Comp.ClangSound, tube);
            }
        }

        Dirty(ent);

        return true;
    }

    public void AttachEntityToDisposalHolder(Entity<DisposalHolderComponent> ent, EntityUid attachee)
    {
        var comp = EnsureComp<BeingDisposedComponent>(attachee);

        if (comp.Holder == ent.Owner)
            return;

        comp.Holder = ent;
        Dirty(attachee, comp);

        var ev = new DisposalSystemTransitionEvent();
        RaiseLocalEvent(attachee, ref ev);
    }

    public void DetachEntityFromDisposalHolder(EntityUid detachee)
    {
        RemComp<BeingDisposedComponent>(detachee);

        var ev = new DisposalSystemTransitionEvent();
        RaiseLocalEvent(detachee, ref ev);
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
            ExitDisposals(ent);
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

        if (TryComp<DisposalTubeComponent>(nextTube, out var tube) &&
            EnterTube(ent, (nextTube.Value, tube)))
        {
            UpdateComp(ent);
        }
    }
}
