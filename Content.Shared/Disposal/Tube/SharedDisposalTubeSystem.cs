using System.Linq;
using System.Text;
using Content.Shared.Atmos;
using Content.Shared.Disposal.Components;
using Content.Shared.Disposal.Unit;
using Content.Shared.Popups;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Containers;
using Robust.Shared.Map.Components;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Components;
using Robust.Shared.Random;

namespace Content.Shared.Disposal.Tube;

public abstract partial class SharedDisposalTubeSystem : EntitySystem
{
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly SharedPopupSystem _popups = default!;
    [Dependency] private readonly SharedUserInterfaceSystem _uiSystem = default!;
    [Dependency] private readonly SharedAudioSystem _audioSystem = default!;
    [Dependency] private readonly SharedDisposableSystem _disposableSystem = default!;
    [Dependency] private readonly SharedContainerSystem _containerSystem = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly SharedMapSystem _map = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<DisposalTubeComponent, GetDisposalsConnectableDirectionsEvent>(OnGetTransitConnectableDirections);
        SubscribeLocalEvent<DisposalTubeComponent, GetDisposalsNextDirectionEvent>(OnGetTransitNextDirection);

        SubscribeLocalEvent<DisposalEntryComponent, GetDisposalsConnectableDirectionsEvent>(OnGetEntryConnectableDirections);
        SubscribeLocalEvent<DisposalEntryComponent, GetDisposalsNextDirectionEvent>(OnGetEntryNextDirection);

        SubscribeLocalEvent<DisposalRouterComponent, GetDisposalsConnectableDirectionsEvent>(OnGetRouterConnectableDirections);
        SubscribeLocalEvent<DisposalRouterComponent, GetDisposalsNextDirectionEvent>(OnGetRouterNextDirection);

        SubscribeLocalEvent<DisposalTaggerComponent, GetDisposalsConnectableDirectionsEvent>(OnGetTaggerConnectableDirections);
        SubscribeLocalEvent<DisposalTaggerComponent, GetDisposalsNextDirectionEvent>(OnGetTaggerNextDirection);

        Subs.BuiEvents<DisposalRouterComponent>(DisposalRouterUiKey.Key, subs =>
        {
            subs.Event<BoundUIOpenedEvent>(OnOpenRouterUI);
            subs.Event<DisposalRouterUiActionMessage>(OnUiAction);
        });

        Subs.BuiEvents<DisposalTaggerComponent>(DisposalTaggerUiKey.Key, subs =>
        {
            subs.Event<BoundUIOpenedEvent>(OnOpenTaggerUI);
            subs.Event<DisposalTaggerUiActionMessage>(OnUiAction);
        });
    }

    /// <summary>
    /// Handles ui messages from the client. For things such as button presses
    /// which interact with the world and require server action.
    /// </summary>
    /// <param name="msg">A user interface message from the client.</param>
    private void OnUiAction(Entity<DisposalTaggerComponent> ent, ref DisposalTaggerUiActionMessage msg)
    {
        if (TryComp<PhysicsComponent>(ent, out var physBody) && physBody.BodyType != BodyType.Static)
            return;

        //Check for correct message and ignore maleformed strings
        if (msg.Action == DisposalTaggerUiAction.Ok && DisposalTaggerComponent.TagRegex.IsMatch(msg.Tag))
        {
            ent.Comp.Tag = msg.Tag.Trim();
            Dirty(ent);

            _audioSystem.PlayPredicted(ent.Comp.ClickSound, ent, msg.Actor, AudioParams.Default.WithVolume(-2f));
        }
    }

    /// <summary>
    /// Handles ui messages from the client. For things such as button presses
    /// which interact with the world and require server action.
    /// </summary>
    /// <param name="msg">A user interface message from the client.</param>
    private void OnUiAction(Entity<DisposalRouterComponent> ent, ref DisposalRouterUiActionMessage msg)
    {
        if (!Exists(msg.Actor))
            return;

        if (TryComp<PhysicsComponent>(ent, out var physBody) && physBody.BodyType != BodyType.Static)
            return;

        //Check for correct message and ignore maleformed strings
        if (msg.Action == DisposalRouterUiAction.Ok && DisposalRouterComponent.TagRegex.IsMatch(msg.Tags))
        {
            ent.Comp.Tags.Clear();
            foreach (var tag in msg.Tags.Split(',', StringSplitOptions.RemoveEmptyEntries))
            {
                var trimmed = tag.Trim();
                if (trimmed == "")
                    continue;

                ent.Comp.Tags.Add(trimmed);
            }

            Dirty(ent);

            _audioSystem.PlayPredicted(ent.Comp.ClickSound, ent, msg.Actor, AudioParams.Default.WithVolume(-2f));
        }
    }

    private void OnGetTransitConnectableDirections(Entity<DisposalTubeComponent> ent, ref GetDisposalsConnectableDirectionsEvent args)
    {
        var rotation = Transform(ent).LocalRotation;

        args.Connectable = ent.Comp.Exits
            .Select(exit => new Angle(exit.ToAngle() + rotation).GetDir())
            .ToArray();
    }

    private void OnGetTransitNextDirection(Entity<DisposalTubeComponent> ent, ref GetDisposalsNextDirectionEvent args)
    {
        var ev = new GetDisposalsConnectableDirectionsEvent();
        RaiseLocalEvent(ent, ref ev);

        args.Next = ev.Connectable[0];
        var previousDF = args.Holder.PreviousDirectionFrom;

        switch (ev.Connectable.Length)
        {
            case 1:
                return;

            case 2:
                if (args.Next == previousDF)
                    args.Next = ev.Connectable[1];

                return;

            default:
                if (previousDF == args.Next ||
                    Math.Abs(Angle.ShortestDistance(previousDF.ToAngle(), args.Next.ToAngle()).Theta) < ent.Comp.MinDeltaAngle.Theta)
                {
                    var directions = ev.Connectable.Skip(1).
                        Where(direction => direction != previousDF &&
                        Math.Abs(Angle.ShortestDistance(previousDF.ToAngle(), direction.ToAngle()).Theta) >= ent.Comp.MinDeltaAngle).ToArray();

                    if (directions.Length == 0)
                        return;

                    args.Next = _random.Pick(directions);
                }

                return;
        }
    }

    private void OnGetEntryConnectableDirections(Entity<DisposalEntryComponent> ent, ref GetDisposalsConnectableDirectionsEvent args)
    {
        args.Connectable = new[] { Transform(ent).LocalRotation.GetDir() };
    }

    private void OnGetEntryNextDirection(EntityUid uid, DisposalEntryComponent component, ref GetDisposalsNextDirectionEvent args)
    {
        // Ejects contents when they come from the same direction the entry is facing.
        if (args.Holder.PreviousDirectionFrom != Direction.Invalid)
        {
            args.Next = Direction.Invalid;
            return;
        }

        var ev = new GetDisposalsConnectableDirectionsEvent();
        RaiseLocalEvent(uid, ref ev);
        args.Next = ev.Connectable[0];
    }

    private void OnGetRouterConnectableDirections(Entity<DisposalRouterComponent> ent, ref GetDisposalsConnectableDirectionsEvent args)
    {
        OnGetTransitConnectableDirections((ent, ent.Comp), ref args);
    }

    private void OnGetRouterNextDirection(Entity<DisposalRouterComponent> ent, ref GetDisposalsNextDirectionEvent args)
    {
        var ev = new GetDisposalsConnectableDirectionsEvent();
        RaiseLocalEvent(ent, ref ev);

        if (args.Holder.Tags.Overlaps(ent.Comp.Tags))
        {
            args.Next = ev.Connectable[1];
            return;
        }

        args.Next = Transform(ent).LocalRotation.GetDir();
    }

    private void OnGetTaggerConnectableDirections(Entity<DisposalTaggerComponent> ent, ref GetDisposalsConnectableDirectionsEvent args)
    {
        OnGetTransitConnectableDirections((ent, ent.Comp), ref args);
    }

    private void OnGetTaggerNextDirection(Entity<DisposalTaggerComponent> ent, ref GetDisposalsNextDirectionEvent args)
    {
        args.Holder.Tags.Add(ent.Comp.Tag);
        OnGetTransitNextDirection((ent, ent.Comp), ref args);
    }

    private void OnOpenRouterUI(Entity<DisposalRouterComponent> ent, ref BoundUIOpenedEvent args)
    {
        UpdateRouterUserInterface(ent);
    }

    private void OnOpenTaggerUI(Entity<DisposalTaggerComponent> ent, ref BoundUIOpenedEvent args)
    {
        if (_uiSystem.HasUi(ent, DisposalTaggerUiKey.Key))
        {
            _uiSystem.SetUiState(ent.Owner, DisposalTaggerUiKey.Key,
                new DisposalTaggerUserInterfaceState(ent.Comp.Tag));
        }
    }

    /// <summary>
    /// Gets component data to be used to update the user interface client-side.
    /// </summary>
    /// <returns>Returns a <see cref="DisposalRouterComponent.DisposalRouterUserInterfaceState"/></returns>
    private void UpdateRouterUserInterface(Entity<DisposalRouterComponent> ent)
    {
        if (ent.Comp.Tags.Count <= 0)
        {
            _uiSystem.SetUiState(ent.Owner, DisposalRouterUiKey.Key, new DisposalRouterUserInterfaceState(""));
            return;
        }

        var taglist = new StringBuilder();

        foreach (var tag in ent.Comp.Tags)
        {
            taglist.Append(tag);
            taglist.Append(", ");
        }

        taglist.Remove(taglist.Length - 2, 2);

        _uiSystem.SetUiState(ent.Owner, DisposalRouterUiKey.Key, new DisposalRouterUserInterfaceState(taglist.ToString()));
    }

    /// <summary>
    ///
    /// </summary>
    /// <param name="target"></param>
    /// <param name="nextDirection"></param>
    /// <returns></returns>
    public EntityUid? NextTubeFor(Entity<DisposalTubeComponent> target, Direction nextDirection)
    {
        var oppositeDirection = nextDirection.GetOpposite();

        var xform = Transform(target);
        if (!TryComp<MapGridComponent>(xform.GridUid, out var grid))
            return null;

        var position = xform.Coordinates;
        foreach (var entity in _map.GetInDir(xform.GridUid.Value, grid, position, nextDirection))
        {
            if (!TryComp(entity, out DisposalTubeComponent? tube) || tube.DisposalTubeType != target.Comp.DisposalTubeType)
                continue;

            if (!CanConnect((entity, tube), oppositeDirection) && !CanConnect(target, nextDirection))
                continue;

            return entity;
        }

        return null;
    }

    /// <summary>
    ///
    /// </summary>
    /// <param name="ent"></param>
    /// <param name="direction"></param>
    /// <returns></returns>
    public bool CanConnect(Entity<DisposalTubeComponent> ent, Direction direction)
    {
        if (!Transform(ent).Anchored)
        {
            return false;
        }

        var ev = new GetDisposalsConnectableDirectionsEvent();
        RaiseLocalEvent(ent, ref ev);
        return ev.Connectable.Contains(direction);
    }

    /// <summary>
    ///
    /// </summary>
    /// <param name="ent"></param>
    /// <param name="recipient"></param>
    public void PopupDirections(Entity<DisposalTubeComponent> ent, EntityUid recipient)
    {
        var ev = new GetDisposalsConnectableDirectionsEvent();
        RaiseLocalEvent(ent, ref ev);
        var directions = string.Join(", ", ev.Connectable);

        _popups.PopupPredicted(Loc.GetString("disposal-tube-component-popup-directions-text", ("directions", directions)), ent, recipient);
    }

    /// <summary>
    ///
    /// </summary>
    /// <param name="ent"></param>
    /// <param name="unit"></param>
    /// <param name="tags"></param>
    /// <returns></returns>
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

        MergeAtmos((holder, holderComponent), unit.Comp.Air);

        if (tags != null)
        {
            holderComponent.Tags.UnionWith(tags);
            Dirty(holder, holderComponent);
        }

        return _disposableSystem.EnterTube((holder, holderComponent), (ent, ent.Comp2));
    }

    protected virtual void MergeAtmos(Entity<DisposalHolderComponent> ent, GasMixture gasMix)
    {

    }
}
