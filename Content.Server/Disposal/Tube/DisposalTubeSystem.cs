using Content.Server.Disposal.Tube.Components;
using Content.Shared.Movement;
using Content.Shared.Verbs;
using Robust.Server.GameObjects;
using Robust.Shared.Audio;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Localization;
using Robust.Shared.Map;
using Robust.Shared.Maths;
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
            SubscribeLocalEvent<DisposalTaggerComponent, GetVerbsEvent<InteractionVerb>>(AddOpenUIVerbs);
            SubscribeLocalEvent<DisposalRouterComponent, GetVerbsEvent<InteractionVerb>>(AddOpenUIVerbs);
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
            SoundSystem.Play(Filter.Pvs(uid), component.ClangSound.GetSound(), uid);
        }

        private static void BodyTypeChanged(
            EntityUid uid,
            DisposalTubeComponent component,
            PhysicsBodyTypeChangedEvent args)
        {
            component.AnchoredChanged();
        }

        public IDisposalTubeComponent? NextTubeFor(EntityUid target, Direction nextDirection, IDisposalTubeComponent? targetTube = null)
        {
            if (!Resolve(target, ref targetTube))
                return null;
            var oppositeDirection = nextDirection.GetOpposite();

            var grid = _mapManager.GetGrid(EntityManager.GetComponent<TransformComponent>(targetTube.Owner).GridID);
            var position = EntityManager.GetComponent<TransformComponent>(targetTube.Owner).Coordinates;
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
