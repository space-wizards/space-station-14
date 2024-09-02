using System.Linq;
using System.Text;
using Content.Server.Atmos.EntitySystems;
using Content.Server.Construction.Completions;
using Content.Server.Disposal.Tube.Components;
using Content.Server.Disposal.Unit.Components;
using Content.Server.Disposal.Unit.EntitySystems;
using Content.Server.Popups;
using Content.Shared.Destructible;
using Content.Shared.Disposal.Components;
using Robust.Server.GameObjects;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Containers;
using Robust.Shared.Map.Components;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Components;
using Robust.Shared.Random;
using static Content.Shared.Disposal.Components.SharedDisposalRouterComponent;
using static Content.Shared.Disposal.Components.SharedDisposalTaggerComponent;

namespace Content.Server.Disposal.Tube
{
    public sealed class DisposalTubeSystem : EntitySystem
    {
        [Dependency] private readonly IRobustRandom _random = default!;
        [Dependency] private readonly SharedAppearanceSystem _appearanceSystem = default!;
        [Dependency] private readonly PopupSystem _popups = default!;
        [Dependency] private readonly UserInterfaceSystem _uiSystem = default!;
        [Dependency] private readonly SharedAudioSystem _audioSystem = default!;
        [Dependency] private readonly DisposableSystem _disposableSystem = default!;
        [Dependency] private readonly SharedContainerSystem _containerSystem = default!;
        [Dependency] private readonly AtmosphereSystem _atmosSystem = default!;
        [Dependency] private readonly TransformSystem _transform = default!;
        [Dependency] private readonly SharedMapSystem _map = default!;

        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<DisposalTubeComponent, ComponentInit>(OnComponentInit);
            SubscribeLocalEvent<DisposalTubeComponent, ComponentRemove>(OnComponentRemove);

            SubscribeLocalEvent<DisposalTubeComponent, AnchorStateChangedEvent>(OnAnchorChange);
            SubscribeLocalEvent<DisposalTubeComponent, BreakageEventArgs>(OnBreak);
            SubscribeLocalEvent<DisposalTubeComponent, ComponentStartup>(OnStartup);
            SubscribeLocalEvent<DisposalTubeComponent, ConstructionBeforeDeleteEvent>(OnDeconstruct);

            SubscribeLocalEvent<DisposalBendComponent, GetDisposalsConnectableDirectionsEvent>(OnGetBendConnectableDirections);
            SubscribeLocalEvent<DisposalBendComponent, GetDisposalsNextDirectionEvent>(OnGetBendNextDirection);

            SubscribeLocalEvent<DisposalEntryComponent, GetDisposalsConnectableDirectionsEvent>(OnGetEntryConnectableDirections);
            SubscribeLocalEvent<DisposalEntryComponent, GetDisposalsNextDirectionEvent>(OnGetEntryNextDirection);

            SubscribeLocalEvent<DisposalJunctionComponent, GetDisposalsConnectableDirectionsEvent>(OnGetJunctionConnectableDirections);
            SubscribeLocalEvent<DisposalJunctionComponent, GetDisposalsNextDirectionEvent>(OnGetJunctionNextDirection);

            SubscribeLocalEvent<DisposalRouterComponent, GetDisposalsConnectableDirectionsEvent>(OnGetRouterConnectableDirections);
            SubscribeLocalEvent<DisposalRouterComponent, GetDisposalsNextDirectionEvent>(OnGetRouterNextDirection);

            SubscribeLocalEvent<DisposalTransitComponent, GetDisposalsConnectableDirectionsEvent>(OnGetTransitConnectableDirections);
            SubscribeLocalEvent<DisposalTransitComponent, GetDisposalsNextDirectionEvent>(OnGetTransitNextDirection);

            SubscribeLocalEvent<DisposalTaggerComponent, GetDisposalsConnectableDirectionsEvent>(OnGetTaggerConnectableDirections);
            SubscribeLocalEvent<DisposalTaggerComponent, GetDisposalsNextDirectionEvent>(OnGetTaggerNextDirection);

            Subs.BuiEvents<DisposalRouterComponent>(DisposalRouterUiKey.Key, subs =>
            {
                subs.Event<BoundUIOpenedEvent>(OnOpenRouterUI);
                subs.Event<SharedDisposalRouterComponent.UiActionMessage>(OnUiAction);
            });

            Subs.BuiEvents<DisposalTaggerComponent>(DisposalTaggerUiKey.Key, subs =>
            {
                subs.Event<BoundUIOpenedEvent>(OnOpenTaggerUI);
                subs.Event<SharedDisposalTaggerComponent.UiActionMessage>(OnUiAction);
            });
        }


        /// <summary>
        /// Handles ui messages from the client. For things such as button presses
        /// which interact with the world and require server action.
        /// </summary>
        /// <param name="msg">A user interface message from the client.</param>
        private void OnUiAction(EntityUid uid, DisposalTaggerComponent tagger, SharedDisposalTaggerComponent.UiActionMessage msg)
        {
            if (TryComp<PhysicsComponent>(uid, out var physBody) && physBody.BodyType != BodyType.Static)
                return;

            //Check for correct message and ignore maleformed strings
            if (msg.Action == SharedDisposalTaggerComponent.UiAction.Ok && SharedDisposalTaggerComponent.TagRegex.IsMatch(msg.Tag))
            {
                tagger.Tag = msg.Tag.Trim();
                _audioSystem.PlayPvs(tagger.ClickSound, uid, AudioParams.Default.WithVolume(-2f));
            }
        }


        /// <summary>
        /// Handles ui messages from the client. For things such as button presses
        /// which interact with the world and require server action.
        /// </summary>
        /// <param name="msg">A user interface message from the client.</param>
        private void OnUiAction(EntityUid uid, DisposalRouterComponent router, SharedDisposalRouterComponent.UiActionMessage msg)
        {
            if (!EntityManager.EntityExists(msg.Actor))
                return;

            if (TryComp<PhysicsComponent>(uid, out var physBody) && physBody.BodyType != BodyType.Static)
                return;

            //Check for correct message and ignore maleformed strings
            if (msg.Action == SharedDisposalRouterComponent.UiAction.Ok && SharedDisposalRouterComponent.TagRegex.IsMatch(msg.Tags))
            {
                router.Tags.Clear();
                foreach (var tag in msg.Tags.Split(',', StringSplitOptions.RemoveEmptyEntries))
                {
                    var trimmed = tag.Trim();
                    if (trimmed == "")
                        continue;

                    router.Tags.Add(trimmed);
                }

                _audioSystem.PlayPvs(router.ClickSound, uid, AudioParams.Default.WithVolume(-2f));
            }
        }

        private void OnComponentInit(EntityUid uid, DisposalTubeComponent tube, ComponentInit args)
        {
            tube.Contents = _containerSystem.EnsureContainer<Container>(uid, tube.ContainerId);
        }

        private void OnComponentRemove(EntityUid uid, DisposalTubeComponent tube, ComponentRemove args)
        {
            DisconnectTube(uid, tube);
        }

        private void OnGetBendConnectableDirections(EntityUid uid, DisposalBendComponent component, ref GetDisposalsConnectableDirectionsEvent args)
        {
            var direction = Transform(uid).LocalRotation;
            var side = new Angle(MathHelper.DegreesToRadians(direction.Degrees - 90));

            args.Connectable = new[] { direction.GetDir(), side.GetDir() };
        }

        private void OnGetBendNextDirection(EntityUid uid, DisposalBendComponent component, ref GetDisposalsNextDirectionEvent args)
        {
            var ev = new GetDisposalsConnectableDirectionsEvent();
            RaiseLocalEvent(uid, ref ev);

            var previousDF = args.Holder.PreviousDirectionFrom;

            if (previousDF == Direction.Invalid)
            {
                args.Next = ev.Connectable[0];
                return;
            }

            args.Next = previousDF == ev.Connectable[0] ? ev.Connectable[1] : ev.Connectable[0];
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

        private void OnGetJunctionConnectableDirections(EntityUid uid, DisposalJunctionComponent component, ref GetDisposalsConnectableDirectionsEvent args)
        {
            var direction = Transform(uid).LocalRotation;

            args.Connectable = component.Degrees
                .Select(degree => new Angle(degree.Theta + direction.Theta).GetDir())
                .ToArray();
        }

        private void OnGetJunctionNextDirection(EntityUid uid, DisposalJunctionComponent component, ref GetDisposalsNextDirectionEvent args)
        {
            var next = Transform(uid).LocalRotation.GetDir();
            var ev = new GetDisposalsConnectableDirectionsEvent();
            RaiseLocalEvent(uid, ref ev);
            var directions = ev.Connectable.Skip(1).ToArray();

            if (args.Holder.PreviousDirectionFrom == Direction.Invalid ||
                args.Holder.PreviousDirectionFrom == next)
            {
                args.Next = _random.Pick(directions);
                return;
            }

            args.Next = next;
        }

        private void OnGetRouterConnectableDirections(EntityUid uid, DisposalRouterComponent component, ref GetDisposalsConnectableDirectionsEvent args)
        {
            OnGetJunctionConnectableDirections(uid, component, ref args);
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

        private void OnGetTransitConnectableDirections(EntityUid uid, DisposalTransitComponent component, ref GetDisposalsConnectableDirectionsEvent args)
        {
            var rotation = Transform(uid).LocalRotation;
            var opposite = new Angle(rotation.Theta + Math.PI);

            args.Connectable = new[] { rotation.GetDir(), opposite.GetDir() };
        }

        private void OnGetTransitNextDirection(EntityUid uid, DisposalTransitComponent component, ref GetDisposalsNextDirectionEvent args)
        {
            var ev = new GetDisposalsConnectableDirectionsEvent();
            RaiseLocalEvent(uid, ref ev);
            var previousDF = args.Holder.PreviousDirectionFrom;
            var forward = ev.Connectable[0];

            if (previousDF == Direction.Invalid)
            {
                args.Next = forward;
                return;
            }

            var backward = ev.Connectable[1];
            args.Next = previousDF == forward ? backward : forward;
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

        private void OnDeconstruct(EntityUid uid, DisposalTubeComponent component, ConstructionBeforeDeleteEvent args)
        {
            DisconnectTube(uid, component);
        }

        private void OnStartup(EntityUid uid, DisposalTubeComponent component, ComponentStartup args)
        {
            UpdateAnchored(uid, component, Transform(uid).Anchored);
        }

        private void OnBreak(EntityUid uid, DisposalTubeComponent component, BreakageEventArgs args)
        {
            DisconnectTube(uid, component);
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
        /// <returns>Returns a <see cref="SharedDisposalRouterComponent.DisposalRouterUserInterfaceState"/></returns>
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

        private void OnAnchorChange(EntityUid uid, DisposalTubeComponent component, ref AnchorStateChangedEvent args)
        {
            UpdateAnchored(uid, component, args.Anchored);
        }

        private void UpdateAnchored(EntityUid uid, DisposalTubeComponent component, bool anchored)
        {
            if (anchored)
            {
                ConnectTube(uid, component);

                // TODO this visual data should just generalized into some anchored-visuals system/comp, this has nothing to do with disposal tubes.
                _appearanceSystem.SetData(uid, DisposalTubeVisuals.VisualState, DisposalTubeVisualState.Anchored);
            }
            else
            {
                DisconnectTube(uid, component);
                _appearanceSystem.SetData(uid, DisposalTubeVisuals.VisualState, DisposalTubeVisualState.Free);
            }
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
                if (!TryComp(entity, out DisposalTubeComponent? tube))
                {
                    continue;
                }

                if (!CanConnect(entity, tube, oppositeDirection))
                {
                    continue;
                }

                if (!CanConnect(target, targetTube, nextDirection))
                {
                    continue;
                }

                return entity;
            }

            return null;
        }

        public static void ConnectTube(EntityUid _, DisposalTubeComponent tube)
        {
            if (tube.Connected)
            {
                return;
            }

            tube.Connected = true;
        }


        public void DisconnectTube(EntityUid _, DisposalTubeComponent tube)
        {
            if (!tube.Connected)
            {
                return;
            }

            tube.Connected = false;

            var query = GetEntityQuery<DisposalHolderComponent>();
            foreach (var entity in tube.Contents.ContainedEntities.ToArray())
            {
                if (query.TryGetComponent(entity, out var holder))
                    _disposableSystem.ExitDisposals(entity, holder);
            }
        }

        public bool CanConnect(EntityUid tubeId, DisposalTubeComponent tube, Direction direction)
        {
            if (!tube.Connected)
            {
                return false;
            }

            var ev = new GetDisposalsConnectableDirectionsEvent();
            RaiseLocalEvent(tubeId, ref ev);
            return ev.Connectable.Contains(direction);
        }

        public void PopupDirections(EntityUid tubeId, DisposalTubeComponent _, EntityUid recipient)
        {
            var ev = new GetDisposalsConnectableDirectionsEvent();
            RaiseLocalEvent(tubeId, ref ev);
            var directions = string.Join(", ", ev.Connectable);

            _popups.PopupEntity(Loc.GetString("disposal-tube-component-popup-directions-text", ("directions", directions)), tubeId, recipient);
        }

        public bool TryInsert(EntityUid uid, DisposalUnitComponent from, IEnumerable<string>? tags = default, DisposalEntryComponent? entry = null)
        {
            if (!Resolve(uid, ref entry))
                return false;

            var xform = Transform(uid);
            var holder = Spawn(DisposalEntryComponent.HolderPrototypeId, _transform.GetMapCoordinates(uid, xform: xform));
            var holderComponent = Comp<DisposalHolderComponent>(holder);

            foreach (var entity in from.Container.ContainedEntities.ToArray())
            {
                _disposableSystem.TryInsert(holder, entity, holderComponent);
            }

            _atmosSystem.Merge(holderComponent.Air, from.Air);
            from.Air.Clear();

            if (tags != default)
                holderComponent.Tags.UnionWith(tags);

            return _disposableSystem.EnterTube(holder, uid, holderComponent);
        }
    }
}
