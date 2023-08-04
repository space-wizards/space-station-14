using System.Linq;
using System.Text;
using Content.Server.Construction.Completions;
using Content.Server.Disposal.Tube.Components;
using Content.Server.Popups;
using Content.Server.UserInterface;
using Content.Shared.Destructible;
using Content.Shared.Disposal.Components;
using Content.Shared.Hands.Components;
using Content.Shared.Movement.Events;
using Content.Shared.Popups;
using Robust.Shared.Audio;
using Robust.Shared.Map;
using Robust.Shared.Player;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Server.Disposal.Tube
{
    public sealed class DisposalTubeSystem : EntitySystem
    {
        [Dependency] private readonly IGameTiming _gameTiming = default!;
        [Dependency] private readonly IMapManager _mapManager = default!;
        [Dependency] private readonly IRobustRandom _random = default!;
        [Dependency] private readonly SharedAppearanceSystem _appearanceSystem = default!;
        [Dependency] private readonly PopupSystem _popups = default!;

        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<DisposalTubeComponent, AnchorStateChangedEvent>(OnAnchorChange);
            SubscribeLocalEvent<DisposalTubeComponent, ContainerRelayMovementEntityEvent>(OnRelayMovement);
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

            SubscribeLocalEvent<DisposalRouterComponent, ActivatableUIOpenAttemptEvent>(OnOpenRouterUIAttempt);
            SubscribeLocalEvent<DisposalTaggerComponent, ActivatableUIOpenAttemptEvent>(OnOpenTaggerUIAttempt);
        }

        private void OnGetBendConnectableDirections(EntityUid uid, DisposalBendComponent component, ref GetDisposalsConnectableDirectionsEvent args)
        {
            var direction = Transform(uid).LocalRotation;
            var side = new Angle(MathHelper.DegreesToRadians(direction.Degrees - 90));

            args.Connectable = new[] {direction.GetDir(), side.GetDir()};
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
            args.Connectable = new[] {Transform(uid).LocalRotation.GetDir()};
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

            args.Connectable = new[] {rotation.GetDir(), opposite.GetDir()};
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
            component.Disconnect();
        }

        private void OnStartup(EntityUid uid, DisposalTubeComponent component, ComponentStartup args)
        {
            UpdateAnchored(uid, component, Transform(uid).Anchored);
        }

        private void OnRelayMovement(EntityUid uid, DisposalTubeComponent component, ref ContainerRelayMovementEntityEvent args)
        {
            if (_gameTiming.CurTime < component.LastClang + DisposalTubeComponent.ClangDelay)
            {
                return;
            }

            component.LastClang = _gameTiming.CurTime;
            SoundSystem.Play(component.ClangSound.GetSound(), Filter.Pvs(uid), uid);
        }

        private void OnBreak(EntityUid uid, DisposalTubeComponent component, BreakageEventArgs args)
        {
            component.Disconnect();
        }

        private void OnOpenRouterUIAttempt(EntityUid uid, DisposalRouterComponent router, ActivatableUIOpenAttemptEvent args)
        {
            if (!TryComp<HandsComponent>(args.User, out var hands))
            {
                uid.PopupMessage(args.User, Loc.GetString("disposal-router-window-tag-input-activate-no-hands"));
                return;
            }

            var activeHandEntity = hands.ActiveHandEntity;
            if (activeHandEntity != null)
            {
                args.Cancel();
            }

            UpdateRouterUserInterface(router);
        }

        private void OnOpenTaggerUIAttempt(EntityUid uid, DisposalTaggerComponent tagger, ActivatableUIOpenAttemptEvent args)
        {
            if (!TryComp<HandsComponent>(args.User, out var hands))
            {
                uid.PopupMessage(args.User, Loc.GetString("disposal-tagger-window-activate-no-hands"));
                return;
            }

            var activeHandEntity = hands.ActiveHandEntity;
            if (activeHandEntity != null)
            {
                args.Cancel();
            }

            tagger.UserInterface?.SetState(new SharedDisposalTaggerComponent.DisposalTaggerUserInterfaceState(tagger.Tag));
        }

        /// <summary>
        /// Gets component data to be used to update the user interface client-side.
        /// </summary>
        /// <returns>Returns a <see cref="SharedDisposalRouterComponent.DisposalRouterUserInterfaceState"/></returns>
        private void UpdateRouterUserInterface(DisposalRouterComponent router)
        {
            if (router.Tags.Count <= 0)
            {
                router.UserInterface?.SetState(new SharedDisposalRouterComponent.DisposalRouterUserInterfaceState(""));
                return;
            }

            var taglist = new StringBuilder();

            foreach (var tag in router.Tags)
            {
                taglist.Append(tag);
                taglist.Append(", ");
            }

            taglist.Remove(taglist.Length - 2, 2);

            router.UserInterface?.SetState(new SharedDisposalRouterComponent.DisposalRouterUserInterfaceState(taglist.ToString()));
        }

        private void OnAnchorChange(EntityUid uid, DisposalTubeComponent component, ref AnchorStateChangedEvent args)
        {
            UpdateAnchored(uid, component, args.Anchored);
        }

        private void UpdateAnchored(EntityUid uid, DisposalTubeComponent component, bool anchored)
        {
            if (anchored)
            {
                component.Connect();

                // TODO this visual data should just generalized into some anchored-visuals system/comp, this has nothing to do with disposal tubes.
                _appearanceSystem.SetData(uid, DisposalTubeVisuals.VisualState, DisposalTubeVisualState.Anchored);
            }
            else
            {
                component.Disconnect();
                _appearanceSystem.SetData(uid, DisposalTubeVisuals.VisualState, DisposalTubeVisualState.Free);
            }
        }

        public DisposalTubeComponent? NextTubeFor(EntityUid target, Direction nextDirection, DisposalTubeComponent? targetTube = null)
        {
            if (!Resolve(target, ref targetTube))
                return null;
            var oppositeDirection = nextDirection.GetOpposite();

            var xform = Transform(targetTube.Owner);
            if (!_mapManager.TryGetGrid(xform.GridUid, out var grid))
                return null;

            var position = xform.Coordinates;
            foreach (var entity in grid.GetInDir(position, nextDirection))
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

                return tube;
            }

            return null;
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

        public void PopupDirections(EntityUid tubeId, DisposalTubeComponent tube, EntityUid recipient)
        {
            var ev = new GetDisposalsConnectableDirectionsEvent();
            RaiseLocalEvent(tubeId, ref ev);
            var directions = string.Join(", ", ev.Connectable);

            _popups.PopupEntity(Loc.GetString("disposal-tube-component-popup-directions-text", ("directions", directions)), tubeId, recipient);
        }
    }
}
