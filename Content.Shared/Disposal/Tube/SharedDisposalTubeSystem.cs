using Content.Shared.Atmos;
using Content.Shared.Disposal.Components;
using Content.Shared.Disposal.Holder;
using Content.Shared.Disposal.Unit;
using Content.Shared.Popups;
using Robust.Shared.Containers;
using Robust.Shared.Map.Components;
using Robust.Shared.Random;
using System.Linq;

namespace Content.Shared.Disposal.Tube;

public abstract partial class SharedDisposalTubeSystem : EntitySystem
{
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly SharedPopupSystem _popups = default!;
    [Dependency] private readonly SharedDisposalHolderSystem _disposableSystem = default!;
    [Dependency] private readonly SharedContainerSystem _containerSystem = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly SharedMapSystem _map = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<DisposalTubeComponent, GetDisposalsNextDirectionEvent>(OnGetTubeNextDirection);
    }

    private void OnGetTubeNextDirection(Entity<DisposalTubeComponent> ent, ref GetDisposalsNextDirectionEvent args)
    {
        var exits = GetTubeConnectableDirections(ent);
        SelectNextTube(ent, exits, ref args);
    }

    /// <summary>
    /// Returns a list of all potential exits to a disposal tube, accounting for its local rotation.
    /// </summary>
    /// <param name="ent">The disposal tube.</param>
    public Direction[] GetTubeConnectableDirections(Entity<DisposalTubeComponent> ent)
    {
        var rotation = Transform(ent).LocalRotation;

        return ent.Comp.Exits
            .Select(exit => new Angle(exit.ToAngle() + rotation).GetDir())
            .ToArray();
    }

    /// <summary>
    /// Selects the best exit for a disposal tube, based on a curated list of choices.
    /// </summary>
    /// <param name="ent">The disposal tube.</param>
    /// <param name="exits">The currated list of possible exits from the disposal tube.</param>
    /// <param name="args">The args for the 'get next direction' event.</param>
    public void SelectNextTube(Entity<DisposalTubeComponent> ent, Direction[] exits, ref GetDisposalsNextDirectionEvent args)
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
    /// Tries to find an adjacent disposal tube in a specified direction.
    /// </summary>
    /// <param name="ent">The original disposal tube.</param>
    /// <param name="nextDirection">The specified direction.</param>
    /// <returns>The adjacent disposal tube.</returns>
    public EntityUid? NextTubeFor(Entity<DisposalTubeComponent> ent, Direction nextDirection)
    {
        var oppositeDirection = nextDirection.GetOpposite();

        var xform = Transform(ent);
        if (!TryComp<MapGridComponent>(xform.GridUid, out var grid))
            return null;

        var position = xform.Coordinates;
        foreach (var entity in _map.GetInDir(xform.GridUid.Value, grid, position, nextDirection))
        {
            if (!TryComp(entity, out DisposalTubeComponent? tube) || tube.DisposalTubeType != ent.Comp.DisposalTubeType)
                continue;

            if (!CanConnect((entity, tube), oppositeDirection) && !CanConnect(ent, nextDirection))
                continue;

            return entity;
        }

        return null;
    }

    /// <summary>
    /// Checks whether a disposal tube can make a valid connection in a specified direction.
    /// </summary>
    /// <param name="ent">The disposal tube.</param>
    /// <param name="direction">The specified direction.</param>
    /// <returns>True if a valid conenction can be made.</returns>
    public bool CanConnect(Entity<DisposalTubeComponent> ent, Direction direction)
    {
        if (!Transform(ent).Anchored)
            return false;

        var exits = GetTubeConnectableDirections(ent);

        if (exits.Length == 0)
            return false;

        return exits.Contains(direction);
    }

    /// <summary>
    /// Creates a pop up message over a disposal tube, listing its potential exits.
    /// </summary>
    /// <param name="ent">The disposal tube.</param>
    /// <param name="recipient">The recipient of the pop up message.</param>
    public void PopupDirections(Entity<DisposalTubeComponent> ent, EntityUid recipient)
    {
        var exits = GetTubeConnectableDirections(ent);

        if (exits.Length == 0)
            return;

        var directions = string.Join(", ", exits);
        _popups.PopupEntity(Loc.GetString("disposal-tube-component-popup-directions-text", ("directions", directions)), ent, recipient);
    }

    /// <summary>
    /// Tries to insert the contents of a disposal unit into the disposals system.
    /// </summary>
    /// <param name="ent">The entry point into disposals.</param>
    /// <param name="unit">The disposals unit.</param>
    /// <param name="tags">Tags to add to the disposed contents.</param>
    /// <returns>True if the insertion was successful.</returns>
    public bool TryInsert(Entity<DisposalEntryComponent, DisposalTubeComponent> ent, Entity<DisposalUnitComponent> unit, IEnumerable<string>? tags = default)
    {
        if (unit.Comp.Container.Count == 0)
            return false;

        var xform = Transform(ent);
        var holder = Spawn(ent.Comp1.HolderPrototypeId, _transform.GetMapCoordinates(ent, xform: xform));
        var holderComponent = Comp<DisposalHolderComponent>(holder);
        var holderEnt = new Entity<DisposalHolderComponent>(holder, holderComponent);

        AddPVSOverride(holderEnt);

        if (holderComponent.Container != null)
        {
            foreach (var entity in unit.Comp.Container.ContainedEntities.ToArray())
            {
                _containerSystem.Insert(entity, holderComponent.Container);
            }
        }

        IntakeAtmos(holderEnt, unit);

        if (tags != null)
        {
            holderComponent.Tags.UnionWith(tags);
            Dirty(holderEnt);
        }

        return _disposableSystem.TryEnterTube(holderEnt, (ent, ent.Comp2));
    }

    /// <summary>
    /// Intakes the atmos of disposal unit into a disposal holder.
    /// </summary>
    /// <param name="ent">The disposal holder.</param>
    /// /// <param name="ent">The disposal unit.</param>
    protected virtual void IntakeAtmos(Entity<DisposalHolderComponent> ent, Entity<DisposalUnitComponent> unit)
    {
        // Handled by the server
    }

    /// <summary>
    /// Adds a PVS override to a specified disposal holder.
    /// </summary>
    /// <param name="ent">The disposal holder.</param>
    protected virtual void AddPVSOverride(Entity<DisposalHolderComponent> ent)
    {
        // Handled by the server
    }
}
