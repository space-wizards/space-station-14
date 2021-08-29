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
            // This system only defines physical interaction verbs
            if (args.Hands == null)
                return;

            // If we are looking for interactions verbs, and we are holding an ID card, add an insert ID verb
            if (args.Types.HasFlag(VerbTypes.Interact) &&
                args.Using != null &&
                args.Using.HasComponent<IdCardComponent>())
            {
                // Can we insert a privileged ID? 
                if (component.PrivilegedIDEmpty)
                {
                    Verb verb = new("IDConsole:InsertPrivilegedID");
                    verb.Act = () => component.InsertIdFromHand(args.User, component.PrivilegedIdContainer, args.Hands);
                    if (args.PrepareGUI)
                    {
                        verb.LocText = "access-insert-privileged-id-verb-get-data-text";
                        verb.IconTexture = "/Textures/Interface/VerbIcons/insert.svg.192dpi.png";
                    }
                    args.Verbs.Add(verb);
                }

                // Can we insert a target ID?
                if (component.TargetIDEmpty)
                {
                    Verb verb = new("IDConsole:InsertTargetID");
                    verb.Act = () => component.InsertIdFromHand(args.User, component.TargetIdContainer, args.Hands);
                    if (args.PrepareGUI)
                    {
                        verb.LocText = "access-insert-target-id-verb-get-data-text";
                        verb.IconTexture = "/Textures/Interface/VerbIcons/insert.svg.192dpi.png";
                    }
                    args.Verbs.Add(verb);
                }
            }

            // If we are looking for alternative verbs, maybe we can eject IDs?
            if (args.Types.HasFlag(VerbTypes.Alternative))
            {
                // Can we eject a privileged ID? 
                if (!component.PrivilegedIDEmpty)
                {
                    Verb verb = new("IDConsole:EjectPrivilegedID");
                    verb.Act = () => component.PutIdInHand(component.PrivilegedIdContainer, args.Hands);
                    if (args.PrepareGUI)
                    {
                        verb.LocText = "access-eject-privileged-id-verb-get-data-text";
                        verb.IconTexture = "/Textures/Interface/VerbIcons/eject.svg.192dpi.png";
                    }
                    args.Verbs.Add(verb);
                }

                // Can we eject a target ID?
                if (!component.TargetIDEmpty)
                {
                    Verb verb = new("IDConsole:EjectTargetID");
                    verb.Act = () => component.PutIdInHand(component.TargetIdContainer, args.Hands);
                    if (args.PrepareGUI)
                    {
                        verb.LocText = "access-eject-target-id-verb-get-data-text";
                        verb.IconTexture = "/Textures/Interface/VerbIcons/eject.svg.192dpi.png";
                    }
                    args.Verbs.Add(verb);
                }
            }
        }
    }
}
