using Content.Shared.Atmos;
using Content.Shared.Damage;
using Content.Shared.Disposal.Components;
using Content.Shared.Disposal.Unit;
using Content.Shared.Eye;
using Content.Shared.Maps;
using Content.Shared.Stunnable;
using Content.Shared.Throwing;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Containers;
using Robust.Shared.Map.Components;
using Robust.Shared.Network;
using Robust.Shared.Physics.Systems;
using Robust.Shared.Player;
using Robust.Shared.Timing;
using System.Text.RegularExpressions;

namespace Content.Shared.Conduit.Holder;

/// <summary>
/// This sytem handles the insertion, movement, and exiting of entities
/// through a conduit-based system.
/// </summary>
public abstract partial class SharedConduitHolderSystem : EntitySystem
{
    [Dependency] private readonly ThrowingSystem _throwing = default!;
    [Dependency] private readonly DamageableSystem _damageable = default!;
    [Dependency] private readonly SharedDisposalUnitSystem _disposalUnitSystem = default!;
    [Dependency] private readonly SharedConduitSystem _disposalTubeSystem = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedContainerSystem _containerSystem = default!;
    [Dependency] private readonly SharedMapSystem _maps = default!;
    [Dependency] private readonly SharedPhysicsSystem _physicsSystem = default!;
    [Dependency] private readonly SharedTransformSystem _xformSystem = default!;
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly SharedEyeSystem _eye = default!;
    [Dependency] private readonly SharedStunSystem _stun = default!;
    [Dependency] private readonly TileSystem _tile = default!;
    [Dependency] private readonly IGameTiming _timing = default!;

    private EntityQuery<DisposalUnitComponent> _disposalUnitQuery;
    private EntityQuery<MetaDataComponent> _metaQuery;
    private EntityQuery<TransformComponent> _xformQuery;

    /// <summary>
    /// Allowed characters for tagging disposed entities.
    /// </summary>
    public static readonly Regex TagRegex = new("^[a-zA-Z0-9, ]*$", RegexOptions.Compiled);

    public override void Initialize()
    {
        base.Initialize();

        _disposalUnitQuery = GetEntityQuery<DisposalUnitComponent>();
        _metaQuery = GetEntityQuery<MetaDataComponent>();
        _xformQuery = GetEntityQuery<TransformComponent>();

        SubscribeLocalEvent<ConduitHolderComponent, ComponentStartup>(OnComponentStartup);

        SubscribeLocalEvent<ActorComponent, DisposalSystemTransitionEvent>(OnActorTransition);
        SubscribeLocalEvent<GetVisMaskEvent>(OnGetVisibility);
    }

    private void OnComponentStartup(Entity<ConduitHolderComponent> ent, ref ComponentStartup args)
    {
        ent.Comp.Container = _containerSystem.EnsureContainer<Container>(ent, nameof(ConduitHolderComponent));
        ent.Comp.EndTime = _timing.CurTime + ent.Comp.LifeTime;
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
            HasComp<ConduitHeldComponent>(ev.Entity))
        {
            ev.VisibilityMask |= (int)VisibilityFlags.Subfloor;
        }
    }

    /// <summary>
    /// Ejects all entities inside a conduit holder from the system.
    /// </summary>
    /// <param name="ent">The conduit holder.</param>
    public void Exit(Entity<ConduitHolderComponent> ent)
    {
        if (Terminating(ent))
            return;

        if (ent.Comp.IsExiting)
            return;

        ent.Comp.IsExiting = true;
        Dirty(ent);

        // Get the holder and grid transforms
        var xform = _xformQuery.GetComponent(ent);
        var gridUid = xform.GridUid;
        _xformQuery.TryGetComponent(gridUid, out var gridXform);

        // Determine the exit angle of the ejected entities
        var exitDirection = ent.Comp.CurrentDirection;
        Angle? exitAngle = exitDirection != Direction.Invalid ? exitDirection.ToAngle() : null;

        // Check for a disposal unit to throw them into and then eject them from it.
        // *This ejection also makes the target not collide with the unit.*
        // *This is on purpose.*

        EntityUid? disposalId = null;
        DisposalUnitComponent? disposalUnit = null;

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

            // If no disposal unit was found, this exit will be a little messy
            if (disposalUnit == null && _net.IsServer)
            {
                // Pry up the tile that the pipe was under
                var tileRef = _maps.GetTileRef((gridUid.Value, grid), xform.Coordinates);
                _tile.PryTile(tileRef);

                // Also pry up the tile infront of the pipe
                if (exitAngle != null)
                {
                    tileRef = _maps.GetTileRef((gridUid.Value, grid), xform.Coordinates.Offset(exitAngle.Value.ToWorldVec()));
                    _tile.PryTile(tileRef);
                }
            }
        }

        // Update the exit angle here to account for the grid's rotation
        if (exitAngle != null && gridXform != null)
        {
            exitAngle += _xformSystem.GetWorldRotation(gridXform);
        }

        // We're purposely iterating over all the holder's children
        // because the holder might have something teleported into it,
        // outside the usual container insertion logic.
        var children = xform.ChildEnumerator;
        while (children.MoveNext(out var held))
        {
            DetachEntityFromConduitHolder(held);

            var meta = _metaQuery.GetComponent(held);
            if (ent.Comp.Container != null && ent.Comp.Container.Contains(held))
                _containerSystem.Remove((held, null, meta), ent.Comp.Container, reparent: false, force: true);

            var heldXform = _xformQuery.GetComponent(held);
            if (heldXform.ParentUid != ent.Owner)
                continue;

            if (disposalUnit != null && disposalUnit.Container != null)
            {
                _containerSystem.Insert((held, heldXform, meta), disposalUnit.Container);
            }
            else
            {
                // Knockdown the entity emerging from the pipe
                _stun.TryKnockdown(held, ent.Comp.ExitStunDuration, force: true);

                // Throw the entity out of the pipe
                _xformSystem.AttachToGridOrMap(held, heldXform);

                if (exitAngle != null)
                {
                    _throwing.TryThrow(held, exitAngle.Value.ToWorldVec() * ent.Comp.ExitDistanceMultiplier, ent.Comp.TraversalSpeed * ent.Comp.ExitSpeedMultiplier);
                }
            }
        }

        if (disposalId != null && disposalUnit != null)
        {
            _disposalUnitSystem.EjectContents((disposalId.Value, disposalUnit));
        }

        ExpelAtmos(ent);

        if (ent.Comp.DespawnEffect != null)
        {
            var effect = Spawn(ent.Comp.DespawnEffect, xform.Coordinates);
            Transform(effect).LocalRotation = xform.LocalRotation;
        }

        PredictedDel(ent.Owner);
    }


    /// <summary>
    /// Attempts to assigns a conduit holder to a new conduit, updating the trajectory of the holder.
    /// </summary>
    /// <param name="ent">The conduit holder.</param>
    /// <param name="tube">The tube the holder is attempting to enter.</param>
    /// <returns>True if the holder can enter the tube.</returns>
    /// <remarks>
    /// This function will call ExitDisposals on any failure that does not make an ExitDisposals impossible.
    /// </remarks>
    public bool TryEnterTube(Entity<ConduitHolderComponent> ent, Entity<ConduitComponent> tube)
    {
        if (ent.Comp.IsExiting)
            return false;

        if (ent.Comp.CurrentConduit == tube)
            return false;

        var ev = new GetConduitNextDirectionEvent(ent);
        RaiseLocalEvent(tube, ref ev);

        // If the next direction to move is invalid, exit immediately
        if (ev.Next == Direction.Invalid)
        {
            Exit(ent);
            return false;
        }

        // Ensure all contained entities are attached to the holder
        if (ent.Comp.Container != null)
        {
            foreach (var held in ent.Comp.Container.ContainedEntities)
            {
                AttachEntityToConduitHolder(ent, held);
            }
        }

        var xform = Transform(ent);

        // Attempt to damage entities when changing direction
        if (ent.Comp.Container != null &&
            ent.Comp.CurrentDirection != ev.Next &&
            ent.Comp.AccumulatedDamage < ent.Comp.MaxAllowedDamage)
        {
            ent.Comp.DirectionChangeCount++;

            var damage = tube.Comp.DamageOnTurn;

            foreach (var held in ent.Comp.Container.ContainedEntities)
            {
                _damageable.TryChangeDamage(held, damage);
            }

            if (_net.IsServer)
            {
                _audio.PlayPvs(tube.Comp.ClangSound, xform.Coordinates);
            }

            ent.Comp.AccumulatedDamage += damage.GetTotal();

            // Check if the holder can escape the current pipe
            if (TryEscaping(ent, tube))
                return false;
        }

        // Update trajectory
        ent.Comp.CurrentDirection = ev.Next;
        ent.Comp.CurrentConduit = tube;
        ent.Comp.NextConduit = _disposalTubeSystem.NextConduitInDirection(tube, ent.Comp.CurrentDirection);

        // Update rotation
        xform.LocalRotation = ent.Comp.CurrentDirection.ToAngle();

        Dirty(ent);
        return true;
    }

    /// <summary>
    /// Links an entity with a conduit holder.
    /// </summary>
    /// <param name="ent">The conduit holder.</param>
    /// <param name="uid">The entity being linked.</param>
    public void AttachEntityToConduitHolder(Entity<ConduitHolderComponent> ent, EntityUid uid)
    {
        var comp = EnsureComp<ConduitHeldComponent>(uid);

        if (comp.Holder == ent.Owner)
            return;

        comp.Holder = ent;
        Dirty(uid, comp);

        var ev = new DisposalSystemTransitionEvent();
        RaiseLocalEvent(uid, ref ev);
    }

    /// <summary>
    /// Unlinks an entity from its conduit holder.
    /// </summary>
    /// <param name="uid">The entity being unlinked.</param>
    public void DetachEntityFromConduitHolder(EntityUid uid)
    {
        RemComp<ConduitHeldComponent>(uid);

        var ev = new DisposalSystemTransitionEvent();
        RaiseLocalEvent(uid, ref ev);
    }

    /// <summary>
    /// Adds a tag to a conduit holder.
    /// </summary>
    /// <param name="ent">The diposal holder.</param>
    /// <param name="tag">The tag.</param>
    public void AddTag(Entity<ConduitHolderComponent> ent, string tag)
    {
        ent.Comp.Tags.Add(tag);
    }

    /// <summary>
    /// Removes a tag from a conduit holder.
    /// </summary>
    /// <param name="ent">The diposal holder.</param>
    /// <param name="tag">The tag.</param>
    public void RemoveTag(Entity<ConduitHolderComponent> ent, string tag)
    {
        ent.Comp.Tags.Remove(tag);
    }

    /// <summary>
    /// Checks if the tags on a conduit holder has any overlap with specified list of tags.
    /// </summary>
    /// <param name="ent">The diposal holder.</param>
    /// <param name="tag">The list of tags.</param>
    /// <returns>True if the conduit holder has one of the listed tags.</returns>
    public bool TagsOverlap(Entity<ConduitHolderComponent> ent, HashSet<string> tags)
    {
        return ent.Comp.Tags.Overlaps(tags);
    }

    /// <summary>
    /// Checks if specified tag is valid for conduit holders.
    /// </summary>
    /// <param name="tag">The tag.</param>
    /// <returns>True if the tag is valid.</returns>
    public bool TagIsValid(string tag)
    {
        return TagRegex.IsMatch(tag);
    }

    public override void Update(float frameTime)
    {
        var query = EntityQueryEnumerator<ConduitHolderComponent, TransformComponent>();
        while (query.MoveNext(out var uid, out var holder, out var xform))
        {
            // Remove any conduit holders that were somehow emptied
            if (holder.Container?.Count == 0)
            {
                if (holder.DespawnEffect != null)
                {
                    var effect = Spawn(holder.DespawnEffect, xform.Coordinates);
                    Transform(effect).LocalRotation = xform.LocalRotation;
                }

                PredictedQueueDel(uid);
                continue;
            }

            UpdateConduitHolder((uid, holder));
        }
    }

    /// <summary>
    /// Runs an update on the trajectory of a conduit holder.
    /// </summary>
    /// <param name="ent">The conduit holder.</param>
    private void UpdateConduitHolder(Entity<ConduitHolderComponent> ent)
    {
        var current = ent.Comp.CurrentConduit;
        var next = ent.Comp.NextConduit;

        if (!Exists(current) || !Exists(next))
        {
            Exit(ent);
            return;
        }

        var gridUid = _xformQuery.GetComponent(current.Value).GridUid;

        if (gridUid == null)
            return;

        // Apply a linear velocity to a conduit holder which
        // will direct it toward the next tube on its route
        var gridRotation = _xformSystem.GetWorldRotation(gridUid.Value);
        var origin = _xformQuery.GetComponent(current.Value).Coordinates;
        var destination = _xformQuery.GetComponent(next.Value).Coordinates;
        var entCoords = _xformQuery.GetComponent(ent).Coordinates;

        // How far off are we from our destination?
        var entDestDiff = destination.Position - entCoords.Position;

        // If we're really close, don't bother updating our velocity
        if (entDestDiff.Length() > 1e-6)
        {
            // Set velocity
            var velocity = gridRotation.RotateVec(entDestDiff.Normalized() * ent.Comp.TraversalSpeed);
            _physicsSystem.SetLinearVelocity(ent, velocity);

            // Determine whether the conduit holder should update its route,
            // based on its current position with respect to its target and origin
            var originDestDiff = destination.Position - origin.Position;
            var originEntDiff = entCoords.Position - origin.Position;

            if (originEntDiff.Length() < originDestDiff.Length())
                return;
        }

        // Attempt to enter the next tube
        if (TryComp<ConduitComponent>(next, out var conduit) &&
            TryEnterTube(ent, (next.Value, conduit)))
        {
            UpdateConduitHolder(ent);
        }
    }

    /// <summary>
    /// Expels the atmos of a conduit holder back into its surrounding environment.
    /// </summary>
    /// <param name="ent">The conduit holder.</param>
    protected virtual void ExpelAtmos(Entity<ConduitHolderComponent> ent)
    {
        // Handled by the server
    }

    /// <summary>
    /// Transfer the atmos of a disposal unit into the conduit holder it is launching.
    /// </summary>
    /// <param name="ent">The conduit holder.</param>
    /// <param name="unit">The disposal unit.</param>
    public virtual void TransferAtmos(Entity<ConduitHolderComponent> ent, Entity<DisposalUnitComponent> unit)
    {
        // Handled by the server
    }

    /// <summary>
    /// The conduit holder attempts to escape the conduit system.
    /// </summary>
    /// <param name="ent">The conduit holder.</param>
    /// <param name="conduit">The conduit the holder is attempting to escape.</param>
    /// <returns> True if the conduit holder escaped.</returns>
    protected virtual bool TryEscaping(Entity<ConduitHolderComponent> ent, Entity<ConduitComponent> conduit)
    {
        // Handled by the server

        return false;
    }
}
