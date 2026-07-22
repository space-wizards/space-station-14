using Content.Shared.Damage.Systems;
using Content.Shared.Disposal.Components;
using Content.Shared.Disposal.Tube;
using Content.Shared.Disposal.Unit;
using Content.Shared.Explosion;
using Content.Shared.Eye;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Containers;
using Robust.Shared.Network;
using Robust.Shared.Physics.Systems;
using Robust.Shared.Player;
using System.Text.RegularExpressions;

namespace Content.Shared.Disposal.Holder;

/// <summary>
/// This sytem handles the insertion, movement, and exiting of entities
/// through the disposals system.
/// </summary>
public abstract partial class SharedDisposalHolderSystem : EntitySystem
{
    [Dependency] private DamageableSystem _damageable = default!;
    [Dependency] private DisposalTubeSystem _disposalTube = default!;
    [Dependency] private SharedAudioSystem _audio = default!;
    [Dependency] private SharedContainerSystem _container = default!;
    [Dependency] private SharedPhysicsSystem _physics = default!;
    [Dependency] private SharedTransformSystem _xform = default!;
    [Dependency] private INetManager _net = default!;
    [Dependency] private SharedEyeSystem _eye = default!;

    private EntityQuery<TransformComponent> _xformQuery;

    /// <summary>
    /// Allowed characters for tagging disposed entities.
    /// </summary>
    public static readonly Regex TagRegex = new("^[a-zA-Z0-9, ]*$", RegexOptions.Compiled);

    public override void Initialize()
    {
        base.Initialize();

        _xformQuery = GetEntityQuery<TransformComponent>();

        SubscribeLocalEvent<DisposalHolderComponent, ComponentStartup>(OnComponentStartup);
        SubscribeLocalEvent<DisposalHolderComponent, BeforeExplodeEvent>(OnExploded);

        SubscribeLocalEvent<ActorComponent, DisposalSystemTransitionEvent>(OnActorTransition);
        SubscribeLocalEvent<BeingDisposedComponent, GetVisMaskEvent>(OnGetVisibility);
    }

    private void OnComponentStartup(Entity<DisposalHolderComponent> ent, ref ComponentStartup args)
    {
        // Ensure the holder will have its container
        ent.Comp.Container = _container.EnsureContainer<Container>(ent, nameof(DisposalHolderComponent));
    }

    private void OnExploded(Entity<DisposalHolderComponent> ent, ref BeforeExplodeEvent args)
    {
        if (ent.Comp.Container == null)
            return;

        args.Contents.AddRange(ent.Comp.Container.ContainedEntities);
    }

    private void OnActorTransition(Entity<ActorComponent> ent, ref DisposalSystemTransitionEvent args)
    {
        // Refreshes visibility mask of a player, leading to OnGetVisibility being called
        _eye.RefreshVisibilityMask(ent.Owner);
    }

    private void OnGetVisibility(Entity<BeingDisposedComponent> entity, ref GetVisMaskEvent ev)
    {
        // Prevents mispredictions by allowing players in the disposal system
        // to be sent any entities that are hidden under subfloors
        if (HasComp<BeingDisposedComponent>(ev.Entity))
            ev.VisibilityMask |= (int)VisibilityFlags.Subfloor;
    }

    /// <summary>
    /// Ejects all entities inside a disposal holder from the disposals system.
    /// </summary>
    /// <param name="ent">The disposal holder.</param>
    public virtual void Exit(Entity<DisposalHolderComponent> ent)
    {
        // Handled by the server
    }

    /// <summary>
    /// Attempts to assigns a disposal holder to a new disposal tube, updating the trajectory of the holder.
    /// </summary>
    /// <param name="ent">The disposal holder.</param>
    /// <param name="tube">The tube the holder is attempting to enter.</param>
    /// <returns>True if the holder can enter the tube.</returns>
    /// <remarks>
    /// This function will call <see cref="Exit"> on any critical failure.
    /// </remarks>
    public bool TryEnterTube(Entity<DisposalHolderComponent> ent, Entity<DisposalTubeComponent?> tube)
    {
        if (!Resolve(tube, ref tube.Comp, false))
            return false;

        if (ent.Comp.IsExiting)
            return false;

        if (ent.Comp.CurrentTube == tube)
            return false;

        var ev = new GetDisposalsNextDirectionEvent(ent);
        RaiseLocalEvent(tube, ref ev);

        // If the next direction to move is invalid, exit immediately
        if (ev.Next == Direction.Invalid)
        {
            Exit(ent);
            return false;
        }

        var xform = Transform(ent);

        // Attempt to damage entities when changing direction
        if (ent.Comp.CurrentDirection != Direction.Invalid && ent.Comp.CurrentDirection != ev.Next)
        {
            // Apply damage
            if (ent.Comp.Container != null && ent.Comp.AccumulatedDamage < ent.Comp.MaxAllowedDamage)
            {
                foreach (var held in ent.Comp.Container.ContainedEntities)
                {
                    _damageable.ChangeDamage(held, ent.Comp.DamageOnTurn);
                }

                ent.Comp.AccumulatedDamage += ent.Comp.DamageOnTurn.GetTotal();
            }

            // Play clang sound
            if (_net.IsServer)
            {
                _audio.PlayPvs(ent.Comp.ClangSound, xform.Coordinates);
            }

            // If the disposed entity re-entered a suspect pipe, 
            // it is likely caught in a loop and should try to escape
            if (ent.Comp.SuspectPipes.Contains(tube))
            {
                ent.Comp.CanEscape = true;
            }

            // Calculate the change in the direction of travel, scaled so that
            // -1 is a 90 degree left turn and 1 is a 90 degree right turn
            var delta = 2 * Angle.ShortestDistance(ent.Comp.CurrentDirection.ToAngle(), ev.Next.ToAngle()).Theta / Math.PI;
            ent.Comp.DirectionBias += (float)Math.Clamp(delta, -1, 1);

            // If the updated travel direction bias exceeds the allowed threshold, 
            // the pipe is marked as suspect.
            if (Math.Abs(ent.Comp.DirectionBias) >= ent.Comp.DirectionBiasThreshold)
            {
                ent.Comp.SuspectPipes.Add(tube);
                ent.Comp.DirectionBias = 0;
            }

            Dirty(ent);

            // Check if the holder can escape the current pipe
            if (TryEscaping(ent, (tube, tube.Comp)))
                return false;
        }

        // Update trajectory
        ent.Comp.CurrentDirection = ev.Next;
        ent.Comp.CurrentTube = tube;
        ent.Comp.NextTube = _disposalTube.GetTubeInDirection((tube, tube.Comp), ent.Comp.CurrentDirection);

        // Update rotation
        xform.LocalRotation = ent.Comp.CurrentDirection.ToAngle();

        Dirty(ent);
        return true;
    }

    /// <summary>
    /// Links an entity with a disposal holder.
    /// </summary>
    /// <param name="holder">The disposal holder.</param>
    /// <param name="uid">The entity being linked.</param>
    public void AttachEntity(EntityUid holder, EntityUid uid)
    {
        var comp = EnsureComp<BeingDisposedComponent>(uid);

        if (comp.Holder == holder)
            return;

        comp.Holder = holder;
        Dirty(uid, comp);

        var ev = new DisposalSystemTransitionEvent();
        RaiseLocalEvent(uid, ref ev);
    }

    /// <summary>
    /// Unlinks an entity from its disposal holder.
    /// </summary>
    /// <param name="uid">The entity being unlinked.</param>
    public void DetachEntity(EntityUid uid)
    {
        RemComp<BeingDisposedComponent>(uid);

        var ev = new DisposalSystemTransitionEvent();
        RaiseLocalEvent(uid, ref ev);
    }

    /// <summary>
    /// Adds a tag to a disposal holder.
    /// </summary>
    /// <param name="ent">The diposal holder.</param>
    /// <param name="tag">The tag.</param>
    public void AddTag(Entity<DisposalHolderComponent> ent, string tag)
    {
        ent.Comp.Tags.Add(tag);
    }

    /// <summary>
    /// Removes a tag from a disposal holder.
    /// </summary>
    /// <param name="ent">The diposal holder.</param>
    /// <param name="tag">The tag.</param>
    public void RemoveTag(Entity<DisposalHolderComponent> ent, string tag)
    {
        ent.Comp.Tags.Remove(tag);
    }

    /// <summary>
    /// Checks if the tags on a disposal holder has any overlap with specified list of tags.
    /// </summary>
    /// <param name="ent">The diposal holder.</param>
    /// <param name="tag">The list of tags.</param>
    /// <returns>True if the disposal holder has one of the listed tags.</returns>
    public bool TagsOverlap(Entity<DisposalHolderComponent> ent, HashSet<string> tags)
    {
        return ent.Comp.Tags.Overlaps(tags);
    }

    /// <summary>
    /// Checks if specified tag is valid for disposal holders.
    /// </summary>
    /// <param name="tag">The tag.</param>
    /// <returns>True if the tag is valid.</returns>
    public bool TagIsValid(string tag)
    {
        return TagRegex.IsMatch(tag);
    }

    public override void Update(float frameTime)
    {
        var query = EntityQueryEnumerator<DisposalHolderComponent, TransformComponent>();
        while (query.MoveNext(out var uid, out var holder, out var xform))
        {
            // Remove any disposal holders that were somehow emptied
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
            Exit(ent);
            return;
        }

        var gridUid = _xformQuery.GetComponent(currentTube.Value).GridUid;

        if (gridUid == null)
            return;

        // Apply a linear velocity to a disposal holder which
        // will direct it toward the next tube on its route
        var origin = _xform.GetWorldPosition(_xformQuery.GetComponent(currentTube.Value));
        var destination = _xform.GetWorldPosition(_xformQuery.GetComponent(nextTube.Value));
        var entCoords = _xform.GetWorldPosition(_xformQuery.GetComponent(ent));

        // How far off are we from our destination?
        var entDestDiff = destination - entCoords;

        // If we're really close, don't bother updating our velocity
        if (entDestDiff.Length() > 1e-3)
        {
            // Set velocity
            var velocity = entDestDiff.Normalized() * ent.Comp.TraversalSpeed;
            _physics.SetLinearVelocity(ent, velocity);

            // Determine whether the disposal holder should update its route,
            // based on its current position with respect to its target and origin
            var originDestDiff = destination - origin;
            var originEntDiff = entCoords - origin;

            if (originEntDiff.Length() < originDestDiff.Length())
                return;
        }

        // Attempt to enter the next tube
        if (TryEnterTube(ent, nextTube.Value))
        {
            UpdateDisposalHolder(ent);
        }
    }

    /// <summary>
    /// Expels the atmos of a disposal holder back into its surrounding environment.
    /// </summary>
    /// <param name="ent">The disposal holder.</param>
    protected virtual void ExpelAtmos(Entity<DisposalHolderComponent> ent)
    {
        // Handled by the server
    }

    /// <summary>
    /// Transfer the atmos of a disposal unit into the disposal holder it is launching.
    /// </summary>
    /// <param name="ent">The disposal holder.</param>
    /// <param name="unit">The disposal unit.</param>
    public virtual void TransferAtmos(Entity<DisposalHolderComponent> ent, Entity<DisposalUnitComponent> unit)
    {
        // Handled by the server
    }

    /// <summary>
    /// The disposal tube holder attempts to escape the disposals system.
    /// </summary>
    /// <param name="ent">The disposal holder.</param>
    /// <param name="tube">The disposal tube the holder is attempting to escape.</param>
    /// <returns> True if the disposal holder escaped the disposal tube.</returns>
    protected virtual bool TryEscaping(Entity<DisposalHolderComponent> ent, Entity<DisposalTubeComponent> tube)
    {
        // Handled by the server

        return false;
    }
}
