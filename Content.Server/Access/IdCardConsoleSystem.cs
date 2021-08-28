using Content.Server.Access.Components;
using Content.Server.Verbs;
using Content.Shared.Verbs;
using Robust.Shared.GameObjects;

namespace Content.Server.Access
{
    public class IdCardConsoleSystem : EntitySystem 
    {
        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<IdCardConsoleComponent, AssembleVerbsEvent>(HandleVerbAssembly);
        }

        private void HandleVerbAssembly(EntityUid uid, IdCardConsoleComponent component, AssembleVerbsEvent args)
        {
            // We Only have physical interactions verbs
            if (args.Hands == null)
                return;

            // Currently, no left click or activate verbs
            if (args.Category == VerbCategory.PrimaryInteraction || args.Category == VerbCategory.Activate)
                return;

            if (!component.PrivilegedIDEmpty)
            {
                var verb = new Verb("EjectPrivilegedID", () => component.PutIdInHand(component.PrivilegedIdContainer, args.Hands));

                if (args.Category == VerbCategory.GUI)
                {
                    verb.LocText = "access-eject-privileged-id-verb-get-data-text";
                    verb.IconTexture = "/Textures/Interface/VerbIcons/eject.svg.192dpi.png";
                }

                args.Verbs.Add(verb);
            }

            if (!component.TargetIDEmpty)
            {
                var verb = new Verb("EjectPrivilegedID", () => component.PutIdInHand(component.TargetIdContainer, args.Hands));

                if (args.Category == VerbCategory.GUI)
                {
                    verb.LocText = "access-eject-target-id-verb-get-data-text";
                    verb.IconTexture = "/Textures/Interface/VerbIcons/eject.svg.192dpi.png";
                }

                args.Verbs.Add(verb);
            }
        }
    }
}
