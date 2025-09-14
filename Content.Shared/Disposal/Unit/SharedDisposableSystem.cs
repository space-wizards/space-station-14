using Content.Shared.Atmos;
using Content.Shared.Damage;
using Content.Shared.Disposal.Components;
using Content.Shared.Disposal.Tube;
using Content.Shared.Eye;
using Content.Shared.Throwing;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Containers;
using Robust.Shared.Map.Components;
using Robust.Shared.Network;
using Robust.Shared.Physics.Systems;
using Robust.Shared.Player;

namespace Content.Shared.Disposal.Unit;

/// <summary>
/// This sytem handles the insertion, movement, and exiting of entities
/// through the disposals system
/// </summary>
public abstract partial class SharedDisposableSystem : EntitySystem
{
    [Dependency] private readonly ThrowingSystem _throwing = default!;
    [Dependency] private readonly DamageableSystem _damageable = default!;
    [Dependency] private readonly SharedDisposalUnitSystem _disposalUnitSystem = default!;
    [Dependency] private readonly SharedDisposalTubeSystem _disposalTubeSystem = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedContainerSystem _containerSystem = default!;
    [Dependency] private readonly SharedMapSystem _maps = default!;
    [Dependency] private readonly SharedPhysicsSystem _physicsSystem = default!;
    [Dependency] private readonly SharedTransformSystem _xformSystem = default!;
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly SharedEyeSystem _eye = default!;

    private EntityQuery<DisposalUnitComponent> _disposalUnitQuery;
    private EntityQuery<MetaDataComponent> _metaQuery;
    private EntityQuery<TransformComponent> _xformQuery;

    public override void Initialize()
    {
        base.Initialize();

        _disposalUnitQuery = GetEntityQuery<DisposalUnitComponent>();
        _metaQuery = GetEntityQuery<MetaDataComponent>();
        _xformQuery = GetEntityQuery<TransformComponent>();

        SubscribeLocalEvent<DisposalHolderComponent, ComponentStartup>(OnComponentStartup);

        SubscribeLocalEvent<ActorComponent, DisposalSystemTransitionEvent>(OnActorTransition);
        SubscribeLocalEvent<GetVisMaskEvent>(OnGetVisibility);
    }

    private void OnComponentStartup(EntityUid uid, DisposalHolderComponent holder, ComponentStartup args)
    {
        holder.Container = _containerSystem.EnsureContainer<Container>(uid, nameof(DisposalHolderComponent));
    }

    private void OnActorTransition(Entity<ActorComponent> ent, ref DisposalSystemTransitionEvent args)
    {
        // Refreshes visibility mask of a player,
        // leading to OnGetVisibility being called
        _eye.RefreshVisibilityMask(ent.Owner);
    }

    private void OnGetVisibility(ref GetVisMaskEvent ev)
    {
        // Prevents mispredictions by allowing players in the disposal system
        // to be sent any entities that are hidden under subfloors
        if (HasComp<ActorComponent>(ev.Entity) &&
            HasComp<BeingDisposedComponent>(ev.Entity))
        {
            ev.VisibilityMask |= (int)VisibilityFlags.Subfloor;
        }
    }

    /// <summary>
    /// Ejects all entities inside a disposal holder from
    /// the disposals system.
    /// </summary>
    /// <param name="ent">The disposal holder.</param>
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
        DisposalUnitComponent? disposalUnit = null;
        var xform = Transform(ent);
        var gridUid = xform.GridUid;

        if (TryComp<MapGridComponent>(gridUid, out var grid))
        {
            foreach (var contentUid in _maps.GetLocal(gridUid.Value, grid, xform.Coordinates))
            {
                if (_disposalUnitQuery.TryGetComponent(contentUid, out disposalUnit))
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

            if (disposalUnit != null)
                _containerSystem.Insert((held, heldXform, meta), disposalUnit.Container);
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

        if (disposalId != null && disposalUnit != null)
        {
            _disposalUnitSystem.TryEjectContents((disposalId.Value, disposalUnit));
        }

        ExpelAtmos(ent);
        PredictedDel(ent.Owner);
    }


    /// <summary>
    /// Attempts to assigns a disposal holder to a new disposal tube, updating the trajectory of the holder.
    /// </summary>
    /// <param name="ent">The disposal holder.</param>
    /// <param name="tube">The tube the holder is attempting to enter.</param>
    /// <returns>True if the holder can enter the tube.</returns>
    /// <remarks>
    /// This function will call ExitDisposals on any failure that does not make an ExitDisposals impossible.
    /// </remarks>
    public bool TryEnterTube(Entity<DisposalHolderComponent> ent, Entity<DisposalTubeComponent> tube)
    {
        if (ent.Comp.IsExitingDisposals)
            return false;

        if (ent.Comp.CurrentTube == tube)
            return false;

        var ev = new GetDisposalsNextDirectionEvent(ent.Comp);
        RaiseLocalEvent(tube, ref ev);

        // If the next direction to move is invalid, exit immediately
        if (ev.Next == Direction.Invalid)
        {
            ExitDisposals(ent);
            return false;
        }

        // Ensure all conatined entities are attached to the holder
        if (ent.Comp.Container != null)
        {
            foreach (var held in ent.Comp.Container.ContainedEntities)
            {
                AttachEntityToDisposalHolder(ent, held);
            }
        }

        // Update trajectory
        ent.Comp.PreviousDirection = ent.Comp.CurrentDirection;
        ent.Comp.CurrentDirection = ev.Next;

        ent.Comp.PreviousTube = ent.Comp.CurrentTube;
        ent.Comp.CurrentTube = tube;
        ent.Comp.NextTube = _disposalTubeSystem.NextTubeFor(tube, ent.Comp.CurrentDirection);

        // Update rotation
        Transform(ent).LocalRotation = ent.Comp.CurrentDirection.ToAngle();

        // Attempt to damage entities when changing direction
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

    /// <summary>
    /// Links an entity with a disposal holder.
    /// </summary>
    /// <param name="ent">The disposal holder.</param>
    /// <param name="uid">The entity being linked.</param>
    public void AttachEntityToDisposalHolder(Entity<DisposalHolderComponent> ent, EntityUid uid)
    {
        var comp = EnsureComp<BeingDisposedComponent>(uid);

        if (comp.Holder == ent.Owner)
            return;

        comp.Holder = ent;
        Dirty(uid, comp);

        var ev = new DisposalSystemTransitionEvent();
        RaiseLocalEvent(uid, ref ev);
    }

    /// <summary>
    /// Unlinks an entity from its disposal holder.
    /// </summary>
    /// <param name="uid">The entity being unlinked.</param>
    public void DetachEntityFromDisposalHolder(EntityUid uid)
    {
        RemComp<BeingDisposedComponent>(uid);

        var ev = new DisposalSystemTransitionEvent();
        RaiseLocalEvent(uid, ref ev);
    }

    public override void Update(float frameTime)
    {
        var query = EntityQueryEnumerator<DisposalHolderComponent>();
        while (query.MoveNext(out var uid, out var holder))
        {
            // Remove any disposal holders that are empty
            if (holder.Container?.Count == 0)
            {
                PredictedQueueDel(uid);
                continue;
            }

            UpdateDisposalHolder((uid, holder));
        }
    }

    /// <summary>
    /// Runs an update on the trajectory of a disposal holder.
    /// </summary>
    /// <param name="ent">The disposal holder.</param>
    private void UpdateDisposalHolder(Entity<DisposalHolderComponent> ent)
    {
        var currentTube = ent.Comp.CurrentTube;
        var nextTube = ent.Comp.NextTube;

        if (!Exists(currentTube) || !Exists(nextTube))
        {
            ExitDisposals(ent);
            return;
        }

        var gridUid = _xformQuery.GetComponent(currentTube.Value).GridUid;

        if (gridUid == null)
            return;

        // Apply a linear velocity to a disposal holder which
        // will direct it toward the next tube on its route
        var gridRotation = _xformSystem.GetWorldRotation(gridUid.Value);
        var origin = _xformQuery.GetComponent(currentTube.Value).Coordinates;
        var destination = _xformQuery.GetComponent(nextTube.Value).Coordinates;
        var entCoords = _xformQuery.GetComponent(ent).Coordinates;

        var entDestDiff = destination.Position - entCoords.Position;
        var velocity = gridRotation.RotateVec(entDestDiff.Normalized()) * ent.Comp.TraversalSpeed;
        _physicsSystem.SetLinearVelocity(ent, velocity);

        // Determine whether the disposal holder should update its route,
        // based on its current position with respect to its target and origin
        var originDestDiff = destination.Position - origin.Position;
        var originEntDiff = entCoords.Position - origin.Position;

        if (originEntDiff.Length() <= originDestDiff.Length())
            return;

        // If it should update its route, attempt to enter the next tube
        if (TryComp<DisposalTubeComponent>(nextTube, out var tube) &&
            TryEnterTube(ent, (nextTube.Value, tube)))
        {
            UpdateDisposalHolder(ent);
        }
    }

    /// <summary>
    /// Expels the atmos of a disposal holder into its surrounding environment.
    /// </summary>
    /// <param name="ent">The disposal holder.</param>
    protected virtual void ExpelAtmos(Entity<DisposalHolderComponent> ent)
    {

    }
}
