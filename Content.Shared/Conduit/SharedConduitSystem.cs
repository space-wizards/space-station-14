using Content.Shared.Atmos;
using Content.Shared.Conduit.Holder;
using Content.Shared.Popups;
using Robust.Shared.Containers;
using Robust.Shared.Map.Components;
using Robust.Shared.Network;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Timing;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace Content.Shared.Conduit;

/// <summary>
/// Handles the basic logic for determining which <see cref="ConduitComponent"/>s are connected
/// and which direction the entities inside them should move next.
/// </summary>
public abstract partial class SharedConduitSystem : EntitySystem
{
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly SharedPopupSystem _popups = default!;
    [Dependency] private readonly SharedConduitHolderSystem _disposalHolder = default!;
    [Dependency] private readonly SharedContainerSystem _containerSystem = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly SharedMapSystem _map = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly INetManager _net = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ConduitComponent, GetConduitNextDirectionEvent>(OnGetTubeNextDirection);
    }

    private void OnGetTubeNextDirection(Entity<ConduitComponent> ent, ref GetConduitNextDirectionEvent args)
    {
        var exits = GetConnectableDirections(ent);
        SelectNextExit(ent, exits, ref args);
    }

    /// <summary>
    /// Returns a list of all potential exits to a conduit, accounting for its local rotation.
    /// </summary>
    /// <param name="ent">The conduit.</param>
    public Direction[] GetConnectableDirections(Entity<ConduitComponent> ent)
    {
        var rotation = Transform(ent).LocalRotation;

        return ent.Comp.Exits
            .Select(exit => new Angle(exit.ToAngle() + rotation).GetDir())
            .ToArray();
    }

    /// <summary>
    /// Selects the best exit for a conduit, based on a curated list of choices.
    /// </summary>
    /// <param name="ent">The conduit.</param>
    /// <param name="exits">The currated list of possible exits from the conduit.</param>
    /// <param name="args">The args for the 'get next direction' event.</param>
    public void SelectNextExit(Entity<ConduitComponent> ent, Direction[] exits, ref GetConduitNextDirectionEvent args)
    {
        if (exits.Length == 0)
            return;

        // The first exit is the default
        args.Next = exits[0];

        // This may change based on the potential exits available
        // and our current direction of travel
        var currentDirection = args.Holder.Comp.CurrentDirection;

        switch (exits.Length)
        {
            // There is only one exit
            case 1:
                if (args.Next.GetOpposite() == currentDirection)
                    args.Next = Direction.Invalid;

                return;

            // If there are two exits
            case 2:
                if (args.Next.GetOpposite() == currentDirection)
                    args.Next = exits[1];

                return;

            // If there are more than two exits
            default:

                // Check that the default exit is valid
                if (args.Next.GetOpposite() == currentDirection ||
                    Math.Abs(Angle.ShortestDistance(currentDirection.ToAngle(), args.Next.ToAngle())) > ent.Comp.MaxDeltaAngle)
                {
                    // If it isn't, remove it from the list, along with any other invalid exits
                    var directions = exits.Skip(1).
                        Where(direction => direction != currentDirection &&
                        Math.Abs(Angle.ShortestDistance(currentDirection.ToAngle(), direction.ToAngle())) <= ent.Comp.MaxDeltaAngle).ToArray();

                    // If no exits were valid, just use the default
                    if (directions.Length == 0)
                        return;

                    // Otherwise, pick one of the remaining exits at random
                    args.Next = _random.Pick(directions);
                }

                return;
        }
    }

    /// <summary>
    /// Tries to find an adjacent conduit in a specified direction.
    /// </summary>
    /// <param name="ent">The original conduit.</param>
    /// <param name="nextDirection">The specified direction.</param>
    /// <returns>The adjacent conduit.</returns>
    public EntityUid? NextConduitInDirection(Entity<ConduitComponent> ent, Direction nextDirection)
    {
        var oppositeDirection = nextDirection.GetOpposite();

        var xform = Transform(ent);
        if (!TryComp<MapGridComponent>(xform.GridUid, out var grid))
            return null;

        var position = xform.Coordinates;
        foreach (var entity in _map.GetInDir(xform.GridUid.Value, grid, position, nextDirection))
        {
            if (!TryComp(entity, out ConduitComponent? tube) || tube.ConduitType != ent.Comp.ConduitType)
                continue;

            if (!CanConnect((entity, tube), oppositeDirection))
                continue;

            return entity;
        }

        return null;
    }

    /// <summary>
    /// Checks whether a conduit can make a valid connection in a specified direction.
    /// </summary>
    /// <param name="ent">The conduit.</param>
    /// <param name="direction">The specified direction.</param>
    /// <returns>True if a valid connection can be made.</returns>
    public bool CanConnect(Entity<ConduitComponent> ent, Direction direction)
    {
        if (!Transform(ent).Anchored)
            return false;

        var exits = GetConnectableDirections(ent);

        if (exits.Length == 0)
            return false;

        return exits.Contains(direction);
    }

    /// <summary>
    /// Creates a pop up message over a conduit, listing its potential exits.
    /// </summary>
    /// <param name="ent">The conduit.</param>
    /// <param name="recipient">The recipient of the pop up message.</param>
    public void PopupDirections(Entity<ConduitComponent> ent, EntityUid recipient)
    {
        var exits = GetConnectableDirections(ent);

        if (exits.Length == 0)
            return;

        var directions = string.Join(", ", exits);
        _popups.PopupEntity(Loc.GetString("conduit-component-popup-directions-text", ("directions", directions)), ent, recipient);
    }

    /// <summary>
    /// Tries to insert the contents of a container into the conduit system.
    /// </summary>
    /// <param name="ent">The entry point into disposals.</param>
    /// <param name="container">The container of entities to be inserted.</param>
    /// <param name="holderProtoId">The proto ID of the conduit holder to spawn.</param>
    /// <param name="holderEnt">The spawned conduit holder.</param>
    /// <param name="tags">Tags to add to the conduit holder.</param>
    /// <returns>True if the insertion was successful.</returns>
    public bool TryInsert
        (Entity<ConduitComponent> ent,
        Container container,
        EntProtoId holderProtoId,
        [NotNullWhen(true)] out Entity<ConduitHolderComponent>? holderEnt,
        IEnumerable<string>? tags = null)
    {
        holderEnt = null;

        if (container.Count == 0)
            return false;

        if (_net.IsClient && !_timing.IsFirstTimePredicted)
            return false;

        var xform = Transform(ent);
        var holder = Spawn(holderProtoId, _transform.GetMapCoordinates(ent, xform: xform));
        var holderComponent = Comp<ConduitHolderComponent>(holder);
        holderEnt = new Entity<ConduitHolderComponent>(holder, holderComponent);

        if (holderComponent.Container == null)
        {
            PredictedQueueDel(holderEnt);
            return false;
        }

        // Add the entity to the PVS override so it gets sent to clients
        // while hidden under subfloors.
        AddPVSOverride(holderEnt.Value);

        foreach (var entity in container.ContainedEntities.ToArray())
        {
            _containerSystem.Insert(entity, holderComponent.Container);
        }

        if (tags != null)
        {
            holderComponent.Tags.UnionWith(tags);
            Dirty(holderEnt.Value);
        }

        return _disposalHolder.TryEnterTube(holderEnt.Value, ent);
    }

    /// <summary>
    /// Adds a PVS override to a specified conduit holder.
    /// </summary>
    /// <param name="ent">The disposal holder.</param>
    protected virtual void AddPVSOverride(Entity<ConduitHolderComponent> ent)
    {
        // Handled by the server
    }
}
