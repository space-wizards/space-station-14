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
    private void OnUiAction(EntityUid uid, DisposalTaggerComponent tagger, DisposalTaggerUiActionMessage msg)
    {
        if (TryComp<PhysicsComponent>(uid, out var physBody) && physBody.BodyType != BodyType.Static)
            return;

        //Check for correct message and ignore maleformed strings
        if (msg.Action == DisposalTaggerUiAction.Ok && DisposalTaggerComponent.TagRegex.IsMatch(msg.Tag))
        {
            tagger.Tag = msg.Tag.Trim();
            Dirty(uid, tagger);

            _audioSystem.PlayPredicted(tagger.ClickSound, uid, msg.Actor, AudioParams.Default.WithVolume(-2f));
        }
    }


    /// <summary>
    /// Handles ui messages from the client. For things such as button presses
    /// which interact with the world and require server action.
    /// </summary>
    /// <param name="msg">A user interface message from the client.</param>
    private void OnUiAction(EntityUid uid, DisposalRouterComponent router, DisposalRouterUiActionMessage msg)
    {
        if (!Exists(msg.Actor))
            return;

        if (TryComp<PhysicsComponent>(uid, out var physBody) && physBody.BodyType != BodyType.Static)
            return;

        //Check for correct message and ignore maleformed strings
        if (msg.Action == DisposalRouterUiAction.Ok && DisposalRouterComponent.TagRegex.IsMatch(msg.Tags))
        {
            router.Tags.Clear();
            foreach (var tag in msg.Tags.Split(',', StringSplitOptions.RemoveEmptyEntries))
            {
                var trimmed = tag.Trim();
                if (trimmed == "")
                    continue;

                router.Tags.Add(trimmed);
            }

            Dirty(uid, router);

            _audioSystem.PlayPredicted(router.ClickSound, uid, msg.Actor, AudioParams.Default.WithVolume(-2f));
        }
    }

    private void OnGetTransitConnectableDirections(EntityUid uid, DisposalTubeComponent component, ref GetDisposalsConnectableDirectionsEvent args)
    {
        var rotation = Transform(uid).LocalRotation;

        args.Connectable = component.Exits
            .Select(exit => new Angle(exit.ToAngle() + rotation).GetDir())
            .ToArray();
    }

    private void OnGetTransitNextDirection(EntityUid uid, DisposalTubeComponent component, ref GetDisposalsNextDirectionEvent args)
    {
        var ev = new GetDisposalsConnectableDirectionsEvent();
        RaiseLocalEvent(uid, ref ev);

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
                    Math.Abs(Angle.ShortestDistance(previousDF.ToAngle(), args.Next.ToAngle()).Theta) < component.MinDeltaAngle.Theta)
                {
                    var directions = ev.Connectable.Skip(1).
                        Where(direction => direction != previousDF &&
                        Math.Abs(Angle.ShortestDistance(previousDF.ToAngle(), direction.ToAngle()).Theta) >= component.MinDeltaAngle).ToArray();

                    if (directions.Length == 0)
                        return;

                    args.Next = _random.Pick(directions);
                }

                return;
        }
    }

    private void OnGetEntryConnectableDirections(EntityUid uid, DisposalEntryComponent component, ref GetDisposalsConnectableDirectionsEvent args)
    {
        args.Connectable = new[] { Transform(uid).LocalRotation.GetDir() };
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

    private void OnGetRouterConnectableDirections(EntityUid uid, DisposalRouterComponent component, ref GetDisposalsConnectableDirectionsEvent args)
    {
        OnGetTransitConnectableDirections(uid, component, ref args);
    }

    private void OnGetRouterNextDirection(EntityUid uid, DisposalRouterComponent component, ref GetDisposalsNextDirectionEvent args)
    {
        var ev = new GetDisposalsConnectableDirectionsEvent();
        RaiseLocalEvent(uid, ref ev);

        if (args.Holder.Tags.Overlaps(component.Tags))
        {
            args.Next = ev.Connectable[1];
            return;
        }

        args.Next = Transform(uid).LocalRotation.GetDir();
    }

    private void OnGetTaggerConnectableDirections(EntityUid uid, DisposalTaggerComponent component, ref GetDisposalsConnectableDirectionsEvent args)
    {
        OnGetTransitConnectableDirections(uid, component, ref args);
    }

    private void OnGetTaggerNextDirection(EntityUid uid, DisposalTaggerComponent component, ref GetDisposalsNextDirectionEvent args)
    {
        args.Holder.Tags.Add(component.Tag);
        OnGetTransitNextDirection(uid, component, ref args);
    }

    private void OnOpenRouterUI(EntityUid uid, DisposalRouterComponent router, BoundUIOpenedEvent args)
    {
        UpdateRouterUserInterface(uid, router);
    }

    private void OnOpenTaggerUI(EntityUid uid, DisposalTaggerComponent tagger, BoundUIOpenedEvent args)
    {
        if (_uiSystem.HasUi(uid, DisposalTaggerUiKey.Key))
        {
            _uiSystem.SetUiState(uid, DisposalTaggerUiKey.Key,
                new DisposalTaggerUserInterfaceState(tagger.Tag));
        }
    }

    /// <summary>
    /// Gets component data to be used to update the user interface client-side.
    /// </summary>
    /// <returns>Returns a <see cref="DisposalRouterComponent.DisposalRouterUserInterfaceState"/></returns>
    private void UpdateRouterUserInterface(EntityUid uid, DisposalRouterComponent router)
    {
        if (router.Tags.Count <= 0)
        {
            _uiSystem.SetUiState(uid, DisposalRouterUiKey.Key, new DisposalRouterUserInterfaceState(""));
            return;
        }

        var taglist = new StringBuilder();

        foreach (var tag in router.Tags)
        {
            taglist.Append(tag);
            taglist.Append(", ");
        }

        taglist.Remove(taglist.Length - 2, 2);

        _uiSystem.SetUiState(uid, DisposalRouterUiKey.Key, new DisposalRouterUserInterfaceState(taglist.ToString()));
    }

    public EntityUid? NextTubeFor(EntityUid target, Direction nextDirection, DisposalTubeComponent? targetTube = null)
    {
        if (!Resolve(target, ref targetTube))
            return null;
        var oppositeDirection = nextDirection.GetOpposite();

        var xform = Transform(target);
        if (!TryComp<MapGridComponent>(xform.GridUid, out var grid))
            return null;

        var position = xform.Coordinates;
        foreach (var entity in _map.GetInDir(xform.GridUid.Value, grid, position, nextDirection))
        {
            if (!TryComp(entity, out DisposalTubeComponent? tube) || tube.DisposalTubeType != targetTube.DisposalTubeType)
                continue;

            if (!CanConnect(entity, tube, oppositeDirection) && !CanConnect(target, targetTube, nextDirection))
                continue;

            return entity;
        }

        return null;
    }


    public bool CanConnect(EntityUid uid, DisposalTubeComponent tube, Direction direction)
    {
        if (!Transform(uid).Anchored)
        {
            return false;
        }

        var ev = new GetDisposalsConnectableDirectionsEvent();
        RaiseLocalEvent(uid, ref ev);
        return ev.Connectable.Contains(direction);
    }

    public void PopupDirections(EntityUid tubeId, DisposalTubeComponent _, EntityUid recipient)
    {
        var ev = new GetDisposalsConnectableDirectionsEvent();
        RaiseLocalEvent(tubeId, ref ev);
        var directions = string.Join(", ", ev.Connectable);

        _popups.PopupPredicted(Loc.GetString("disposal-tube-component-popup-directions-text", ("directions", directions)), tubeId, recipient);
    }

    public bool TryInsert(EntityUid uid, DisposalUnitComponent from, IEnumerable<string>? tags = default, DisposalEntryComponent? entry = null)
    {
        if (!Resolve(uid, ref entry))
            return false;

        if (!TryComp<DisposalTubeComponent>(uid, out var tube))
            return false;

        var xform = Transform(uid);
        var holder = Spawn(entry.HolderPrototypeId, _transform.GetMapCoordinates(uid, xform: xform));
        var holderComponent = Comp<DisposalHolderComponent>(holder);

        if (holderComponent.Container != null)
        {
            foreach (var entity in from.Container.ContainedEntities.ToArray())
            {
                _containerSystem.Insert(entity, holderComponent.Container);
            }
        }

        MergeAtmos((holder, holderComponent), from.Air);

        if (tags != null)
        {
            holderComponent.Tags.UnionWith(tags);
            Dirty(holder, holderComponent);
        }

        return _disposableSystem.EnterTube((holder, holderComponent), (uid, tube));
    }

    protected virtual void MergeAtmos(Entity<DisposalHolderComponent> ent, GasMixture gasMix)
    {

    }
}
