using Content.Server.Access.Components;
using Content.Shared.Verbs;
using Robust.Shared.GameObjects;
using Robust.Shared.Localization;

namespace Content.Server.Access
{
    public class IdCardConsoleSystem : EntitySystem 
    {
        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<IdCardConsoleComponent, GetInteractionVerbsEvent>(AddInsertVerbs);
            SubscribeLocalEvent<IdCardConsoleComponent, GetAlternativeVerbsEvent>(AddEjectVerbs);
        }

        private void AddInsertVerbs(EntityUid uid, IdCardConsoleComponent component, GetInteractionVerbsEvent args)
        {
            if (args.Hands == null || !args.CanAccess)
                return;

            // If we are holding an ID card, add insert verbs
            if (args.Using == null ||
                !args.Using.HasComponent<IdCardComponent>())
                return;

            // Can we insert a privileged ID? 
            if (component.PrivilegedIDEmpty)
            {
                Verb verb = new("IDConsole:InsertPrivilegedID");
                verb.Act = () => component.InsertIdFromHand(args.User, component.PrivilegedIdContainer, args.Hands);
                verb.Category = VerbCategory.Insert;
                verb.Text = Loc.GetString("id-card-console-privileged-id");
                args.Verbs.Add(verb);
            }

            // Can we insert a target ID?
            if (component.TargetIDEmpty)
            {
                Verb verb = new("IDConsole:InsertTargetID");
                verb.Act = () => component.InsertIdFromHand(args.User, component.TargetIdContainer, args.Hands);
                verb.Category = VerbCategory.Insert;
                verb.Text = Loc.GetString("id-card-console-target-id");
                args.Verbs.Add(verb);
            }
        }

        private void AddEjectVerbs(EntityUid uid, IdCardConsoleComponent component, GetAlternativeVerbsEvent args)
        {
            if (args.Hands == null || !args.CanAccess)
                return;

            // Can we eject a privileged ID? 
            if (!component.PrivilegedIDEmpty)
            {
                Verb verb = new("IDConsole:EjectPrivilegedID");
                verb.Act = () => component.PutIdInHand(component.PrivilegedIdContainer, args.Hands);
                verb.Category = VerbCategory.Eject;
                verb.Text = Loc.GetString("id-card-console-privileged-id");
                args.Verbs.Add(verb);
            }

            // Can we eject a target ID?
            if (!component.TargetIDEmpty)
            {
                Verb verb = new("IDConsole:EjectTargetID");
                verb.Act = () => component.PutIdInHand(component.TargetIdContainer, args.Hands);
                verb.Category = VerbCategory.Eject;
                verb.Text = Loc.GetString("id-card-console-target-id");
                args.Verbs.Add(verb);
            }
        }
    }
}
