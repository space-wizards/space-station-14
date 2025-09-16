using Content.Shared.Atmos;
using Content.Shared.Disposal.Components;
using Content.Shared.Disposal.Unit;
using Content.Shared.Popups;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Containers;
using Robust.Shared.Map.Components;
using Robust.Shared.Random;
using System.Linq;

namespace Content.Shared.Disposal.Tube;

public abstract partial class SharedDisposalTubeSystem : EntitySystem
{
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly SharedPopupSystem _popups = default!;
    [Dependency] private readonly SharedAudioSystem _audioSystem = default!;
    [Dependency] private readonly SharedDisposableSystem _disposableSystem = default!;
    [Dependency] private readonly SharedContainerSystem _containerSystem = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly SharedMapSystem _map = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<DisposalEntryComponent, GetDisposalsNextDirectionEvent>(OnGetEntryNextDirection);
        SubscribeLocalEvent<DisposalTubeComponent, GetDisposalsNextDirectionEvent>(OnGetTubeNextDirection);
        SubscribeLocalEvent<DisposalRouterComponent, GetDisposalsNextDirectionEvent>(OnGetRouterNextDirection);
        SubscribeLocalEvent<DisposalTaggerComponent, GetDisposalsNextDirectionEvent>(OnGetTaggerNextDirection);

        Subs.BuiEvents<DisposalRouterComponent>(DisposalRouterUiKey.Key, subs =>
        {
            subs.Event<DisposalRouterUiActionMessage>(OnUiAction);
        });

        Subs.BuiEvents<DisposalTaggerComponent>(DisposalTaggerUiKey.Key, subs =>
        {
            subs.Event<DisposalTaggerUiActionMessage>(OnUiAction);
        });
    }

    /// <summary>
    /// Handles UI messages from the client. For things such as button presses
    /// which interact with the world and require server action.
    /// </summary>
    /// <param name="msg">A user interface message from the client.</param>
    private void OnUiAction(Entity<DisposalTaggerComponent> ent, ref DisposalTaggerUiActionMessage msg)
    {
        if (!Exists(msg.Actor))
            return;

        // Check for correct message and ignore maleformed strings
        if (msg.Action == DisposalTaggerUiAction.Ok && DisposalTaggerComponent.TagRegex.IsMatch(msg.Tag))
        {
            ent.Comp.Tag = msg.Tag.Trim();
            Dirty(ent);

            _audioSystem.PlayPredicted(ent.Comp.ClickSound, ent, msg.Actor, AudioParams.Default.WithVolume(-2f));
        }
    }

    /// <summary>
    /// Handles UI messages from the client. For things such as button presses
    /// which interact with the world and require server action.
    /// </summary>
    /// <param name="msg">A user interface message from the client.</param>
    private void OnUiAction(Entity<DisposalRouterComponent> ent, ref DisposalRouterUiActionMessage msg)
    {
        if (!Exists(msg.Actor))
            return;

        // Check for correct message and ignore maleformed strings
        if (msg.Action == DisposalRouterUiAction.Ok && DisposalRouterComponent.TagRegex.IsMatch(msg.Tags))
        {
            ent.Comp.Tags.Clear();

            foreach (var tag in msg.Tags.Split(',', StringSplitOptions.RemoveEmptyEntries))
            {
                var trimmed = tag.Trim();

                if (string.IsNullOrEmpty(trimmed))
                    continue;

                ent.Comp.Tags.Add(trimmed);
            }

            Dirty(ent);

            _audioSystem.PlayPredicted(ent.Comp.ClickSound, ent, msg.Actor, AudioParams.Default.WithVolume(-2f));
        }
    }

    private void OnGetEntryNextDirection(Entity<DisposalEntryComponent> ent, ref GetDisposalsNextDirectionEvent args)
    {
        // Ejects contents when they pass into the entry from within the disposals system
        if (args.Holder.PreviousDirectionFrom != Direction.Invalid)
        {
            args.Next = Direction.Invalid;
            return;
        }

        args.Next = Transform(ent).LocalRotation.GetDir();
    }

    private void OnGetTubeNextDirection(Entity<DisposalTubeComponent> ent, ref GetDisposalsNextDirectionEvent args)
    {
        var exits = GetTubeConnectableDirections(ent);
        HandleTubeChoice(ent, exits, ref args);
    }

    private void OnGetRouterNextDirection(Entity<DisposalRouterComponent> ent, ref GetDisposalsNextDirectionEvent args)
    {
        var exits = GetTubeConnectableDirections((ent, ent.Comp));

        if (exits.Length < 3 || args.Holder.Tags.Overlaps(ent.Comp.Tags))
        {
            HandleTubeChoice((ent, ent.Comp), exits, ref args);
            return;
        }

        HandleTubeChoice((ent, ent.Comp), exits.Skip(1).ToArray(), ref args);
    }

    private void OnGetTaggerNextDirection(Entity<DisposalTaggerComponent> ent, ref GetDisposalsNextDirectionEvent args)
    {
        args.Holder.Tags.Add(ent.Comp.Tag);
        OnGetTubeNextDirection((ent, ent.Comp), ref args);
    }

    /// <summary>
    /// Returns a list of all potential exits to a disposal tube, accounting for its local rotation.
    /// </summary>
    /// <param name="ent">The disposal tube.</param>
    private Direction[] GetTubeConnectableDirections(Entity<DisposalTubeComponent> ent)
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
    private void HandleTubeChoice(Entity<DisposalTubeComponent> ent, Direction[] exits, ref GetDisposalsNextDirectionEvent args)
    {
        if (exits.Length == 0)
            return;

        // The first exit is the default
        args.Next = exits[0];

        // This may change based on the number of potential exits available
        var previousDirectionFrom = args.Holder.PreviousDirectionFrom;

        switch (exits.Length)
        {
            // There is only one exit
            case 1:
                return;

            // If there are two exits
            case 2:
                if (args.Next == previousDirectionFrom)
                    args.Next = exits[1];

                return;

            // If there are more than two exits
            default:

                // Check that the default exit is valid
                if (previousDirectionFrom == args.Next ||
                    Math.Abs(Angle.ShortestDistance(previousDirectionFrom.ToAngle(), args.Next.ToAngle()).Theta) < ent.Comp.MinDeltaAngle.Theta)
                {
                    // If it isn't, remove it from the list, along with any other invalid exits
                    var directions = exits.Skip(1).
                        Where(direction => direction != previousDirectionFrom &&
                        Math.Abs(Angle.ShortestDistance(previousDirectionFrom.ToAngle(), direction.ToAngle()).Theta) >= ent.Comp.MinDeltaAngle).ToArray();

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
        _popups.PopupPredicted(Loc.GetString("disposal-tube-component-popup-directions-text", ("directions", directions)), ent, recipient);
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

        if (holderComponent.Container != null)
        {
            foreach (var entity in unit.Comp.Container.ContainedEntities.ToArray())
            {
                _containerSystem.Insert(entity, holderComponent.Container);
            }
        }

        IntakeAtmos((holder, holderComponent), unit);

        if (tags != null)
        {
            holderComponent.Tags.UnionWith(tags);
            Dirty(holder, holderComponent);
        }

        return _disposableSystem.TryEnterTube((holder, holderComponent), (ent, ent.Comp2));
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
}
