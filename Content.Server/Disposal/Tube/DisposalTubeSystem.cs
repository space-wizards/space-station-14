using Content.Server.Disposal.Tube.Components;
using Content.Shared.Verbs;
using Robust.Server.GameObjects;
using Robust.Shared.GameObjects;
using Robust.Shared.Localization;

namespace Content.Server.Disposal.Tube
{
    public sealed class DisposalTubeSystem : EntitySystem
    {
        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<DisposalTubeComponent, PhysicsBodyTypeChangedEvent>(BodyTypeChanged);
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

            Verb verb = new Verb("disposal:taggerconfig");
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

            Verb verb = new Verb("disposal:routerconfig");
            verb.Text = Loc.GetString("configure-verb-get-data-text");
            verb.IconTexture = "/Textures/Interface/VerbIcons/settings.svg.192dpi.png";
            verb.Act = () => component.OpenUserInterface(actor);
            args.Verbs.Add(verb);
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
