using Content.Server.Disposal.Tube.Components;
using Content.Shared.Movement;
using Content.Shared.Verbs;
using Robust.Server.GameObjects;
using Robust.Shared.Audio;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Localization;
using Robust.Shared.Player;
using Robust.Shared.Timing;

namespace Content.Server.Disposal.Tube
{
    public sealed class DisposalTubeSystem : EntitySystem
    {
        [Dependency] private readonly IGameTiming _gameTiming = default!;

        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<DisposalTubeComponent, PhysicsBodyTypeChangedEvent>(BodyTypeChanged);

            SubscribeLocalEvent<DisposalTubeComponent, RelayMovementEntityEvent>(OnRelayMovement);
            SubscribeLocalEvent<DisposalTaggerComponent, GetInteractionVerbsEvent>(AddOpenUIVerbs);
            SubscribeLocalEvent<DisposalRouterComponent, GetInteractionVerbsEvent>(AddOpenUIVerbs);
        }
        
        private void AddOpenUIVerbs(EntityUid uid, DisposalTaggerComponent component, GetInteractionVerbsEvent args)
        {
            if (!args.CanAccess || !args.CanInteract)
                return;

            if (!args.User.TryGetComponent<ActorComponent>(out var actor))
                return;
            var player = actor.PlayerSession;

            Verb verb = new();
            verb.Text = Loc.GetString("configure-verb-get-data-text");
            verb.IconTexture = "/Textures/Interface/VerbIcons/settings.svg.192dpi.png";
            verb.Act = () => component.OpenUserInterface(actor);
            args.Verbs.Add(verb);            
        }

        private void AddOpenUIVerbs(EntityUid uid, DisposalRouterComponent component, GetInteractionVerbsEvent args)
        {
            if (!args.CanAccess || !args.CanInteract)
                return;

            if (!args.User.TryGetComponent<ActorComponent>(out var actor))
                return;
            var player = actor.PlayerSession;

            Verb verb = new();
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

            var entity = EntityManager.GetEntity(uid);
            component.LastClang = _gameTiming.CurTime;
            SoundSystem.Play(Filter.Pvs(entity), component.ClangSound.GetSound(), entity);
        }

        private static void BodyTypeChanged(
            EntityUid uid,
            DisposalTubeComponent component,
            PhysicsBodyTypeChangedEvent args)
        {
            component.AnchoredChanged();
        }
    }
}
