using Content.Shared.Verbs;
using Robust.Server.GameObjects;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Localization;

namespace Content.Server.Strip
{
    public sealed class StrippableSystem : EntitySystem
    {
        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<StrippableComponent, GetOtherVerbsEvent>(AddStripVerb);
        }

        private void AddStripVerb(EntityUid uid, StrippableComponent component, GetOtherVerbsEvent args)
        {
            if (args.Hands == null || !args.CanAccess || !args.CanInteract || args.Target == args.User)
                return;

            if (!EntityManager.TryGetComponent(args.User, out ActorComponent? actor))
                return;

            Verb verb = new();
            verb.Text = Loc.GetString("strip-verb-get-data-text");
            verb.IconTexture = "/Textures/Interface/VerbIcons/outfit.svg.192dpi.png";
            verb.Act = () => component.OpenUserInterface(actor.PlayerSession);
            args.Verbs.Add(verb);
        }
    }
}
