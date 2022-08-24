using System.Text;
using Content.Server.Disposal.Tube.Components;
using Content.Server.UserInterface;
using Content.Server.Hands.Components;
using Content.Shared.Destructible;
using Content.Shared.Disposal.Components;
using Content.Shared.Movement;
using Content.Shared.Movement.Events;
using Content.Shared.Verbs;
using Content.Shared.Popups;
using Robust.Server.GameObjects;
using Robust.Shared.Audio;
using Robust.Shared.Map;
using Robust.Shared.Player;
using Robust.Shared.Timing;

namespace Content.Server.Disposal.Tube
{
    public sealed class DisposalTubeSystem : EntitySystem
    {
        [Dependency] private readonly IGameTiming _gameTiming = default!;
        [Dependency] private readonly IMapManager _mapManager = default!;

        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<DisposalTubeComponent, PhysicsBodyTypeChangedEvent>(BodyTypeChanged);
            SubscribeLocalEvent<DisposalTubeComponent, ContainerRelayMovementEntityEvent>(OnRelayMovement);
            SubscribeLocalEvent<DisposalTubeComponent, BreakageEventArgs>(OnBreak);
            SubscribeLocalEvent<DisposalRouterComponent, ActivatableUIOpenAttemptEvent>(OnOpenRouterUIAttempt);
            SubscribeLocalEvent<DisposalTaggerComponent, ActivatableUIOpenAttemptEvent>(OnOpenTaggerUIAttempt);

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

        private static void BodyTypeChanged(
            EntityUid uid,
            DisposalTubeComponent component,
            ref PhysicsBodyTypeChangedEvent args)
        {
            component.AnchoredChanged();
        }

        public IDisposalTubeComponent? NextTubeFor(EntityUid target, Direction nextDirection, IDisposalTubeComponent? targetTube = null)
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
                if (!EntityManager.TryGetComponent(entity, out IDisposalTubeComponent? tube))
                {
                    continue;
                }

                if (!tube.CanConnect(oppositeDirection, targetTube))
                {
                    continue;
                }

                if (!targetTube.CanConnect(nextDirection, tube))
                {
                    continue;
                }

                return tube;
            }

            return null;
        }
    }
}
