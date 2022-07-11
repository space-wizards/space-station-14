using Content.Server.Disposal.Tube.Components;
using Content.Server.UserInterface;
using Content.Server.Hands.Components;
using Content.Shared.Destructible;
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
            SubscribeLocalEvent<DisposalTubeComponent, RelayMovementEntityEvent>(OnRelayMovement);
            SubscribeLocalEvent<DisposalTubeComponent, BreakageEventArgs>(OnBreak);
            SubscribeLocalEvent<DisposalTaggerComponent, GetVerbsEvent<InteractionVerb>>(AddOpenUIVerbs);
            SubscribeLocalEvent<DisposalRouterComponent, GetVerbsEvent<InteractionVerb>>(AddOpenUIVerbs);
            SubscribeLocalEvent<DisposalRouterComponent, ActivatableUIOpenAttemptEvent>(OnOpenRouterUIAttempt);
            SubscribeLocalEvent<DisposalTaggerComponent, ActivatableUIOpenAttemptEvent>(OnOpenTaggerUIAttempt);

        }

        private void AddOpenUIVerbs(EntityUid uid, DisposalTaggerComponent component, GetVerbsEvent<InteractionVerb> args)
        {
            if (!args.CanAccess || !args.CanInteract)
                return;

            if (!EntityManager.TryGetComponent<ActorComponent?>(args.User, out var actor))
                return;
            var player = actor.PlayerSession;

            InteractionVerb verb = new();
            verb.Text = Loc.GetString("configure-verb-get-data-text");
            verb.IconTexture = "/Textures/Interface/VerbIcons/settings.svg.192dpi.png";
            verb.Act = () => component.OpenUserInterface(actor);
            args.Verbs.Add(verb);
        }

        private void AddOpenUIVerbs(EntityUid uid, DisposalRouterComponent component, GetVerbsEvent<InteractionVerb> args)
        {
            if (!args.CanAccess || !args.CanInteract)
                return;

            if (!EntityManager.TryGetComponent<ActorComponent?>(args.User, out var actor))
                return;
            var player = actor.PlayerSession;

            InteractionVerb verb = new();
            verb.Text = Loc.GetString("configure-verb-get-data-text");
            verb.IconTexture = "/Textures/Interface/VerbIcons/settings.svg.192dpi.png";
            verb.Act = () => component.OpenUserInterface(actor);
            args.Verbs.Add(verb);
        }

        private void OnRelayMovement(EntityUid uid, DisposalTubeComponent component, RelayMovementEntityEvent args)
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
        }

        private void OnOpenTaggerUIAttempt(EntityUid uid, DisposalTaggerComponent router, ActivatableUIOpenAttemptEvent args)
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
