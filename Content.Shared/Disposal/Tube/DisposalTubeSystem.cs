using Content.Shared.Atmos;
using Content.Shared.Disposal.Unit;
using Content.Shared.Popups;
using Robust.Shared.Map.Components;
using Robust.Shared.Random;
using System.Linq;

namespace Content.Shared.Disposal.Tube;

/// <summary>
/// Handles the basic logic for determining which disposal tubes are connected
/// and which direction entities inside the tubes should move next.
/// </summary>
public sealed partial class DisposalTubeSystem : EntitySystem
{
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly SharedPopupSystem _popups = default!;
    [Dependency] private readonly SharedMapSystem _map = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<DisposalTubeComponent, GetDisposalsNextDirectionEvent>(OnGetTubeNextDirection);
    }

    private void OnGetTubeNextDirection(Entity<DisposalTubeComponent> ent, ref GetDisposalsNextDirectionEvent args)
    {
        var exits = GetTubeConnectableDirections(ent);
        SelectNextDirection(ent, exits, ref args);
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
    public void SelectNextDirection(Entity<DisposalTubeComponent> ent, Direction[] exits, ref GetDisposalsNextDirectionEvent args)
    {
        if (args.Handled)
            return;

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
                if (args.Next.GetOpposite() == currentDirection)
                {
                    // If it isn't, pick one of the remaining exits at random
                    var directions = exits.Skip(1).Where(direction => direction != currentDirection).ToArray();

                    if (directions.Length == 0)
                    {
                        args.Next = Direction.Invalid;
                        return;
                    }

                    args.Next = _random.Pick(directions);
                }

                return;
        }
    }

    /// <summary>
    /// Tries to find an adjacent disposal tube in a specified direction.
    /// </summary>
    /// <param name="ent">The original disposal tube.</param>
    /// <param name="direction">The specified direction.</param>
    /// <returns>The adjacent disposal tube.</returns>
    public EntityUid? GetTubeInDirection(Entity<DisposalTubeComponent> ent, Direction direction)
    {
        var oppositeDirection = direction.GetOpposite();

        var xform = Transform(ent);
        if (!TryComp<MapGridComponent>(xform.GridUid, out var grid))
            return null;

        var position = xform.Coordinates;
        foreach (var entity in _map.GetInDir(xform.GridUid.Value, grid, position, direction))
        {
            if (!TryComp(entity, out DisposalTubeComponent? tube))
                continue;

            if (!CanConnect((entity, tube), oppositeDirection))
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
}
