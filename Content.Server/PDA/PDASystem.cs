using Content.Server.Access.Components;
using Content.Shared.ActionBlocker;
using Content.Shared.Tag;
using Content.Shared.Verbs;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Localization;

namespace Content.Server.PDA
{
    public class PDASystem : EntitySystem
    {
        [Dependency] private readonly ActionBlockerSystem _actionBlockerSystem = default!;

        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<PDAComponent, GetAlternativeVerbsEvent>(AddEjectVerb);
            SubscribeLocalEvent<PDAComponent, GetInteractionVerbsEvent>(AddInsertVerb);
            SubscribeLocalEvent<PDAComponent, GetOtherVerbsEvent>(AddToggleLightVerb);
        }

        private void AddToggleLightVerb(EntityUid uid, PDAComponent component, GetOtherVerbsEvent args)
        {
            if (!args.CanAccess || !args.CanInteract)
                return;

            Verb verb = new("pda:togglelight");
            verb.Text = Loc.GetString("verb-toggle-light");
            verb.IconTexture = "/Textures/Interface/VerbIcons/light.svg.192dpi.png";
            verb.Act = () => component.ToggleLight();
            args.Verbs.Add(verb);
        }

        // TODO VERBS EJECTABLES Standardize eject/insert verbs into a single system?
        // TODO QUESTION: I thought component inheritance was a no-no? These two chargers inherit from BaseCharger?
        private void AddEjectVerb(EntityUid uid, PDAComponent component, GetAlternativeVerbsEvent args)
        {
            if (args.Hands == null ||
                !args.CanAccess ||
                !args.CanInteract ||
                !_actionBlockerSystem.CanPickup(args.User))
                return;

            // eject ID
            if (!component.IdSlotEmpty)
            {
                Verb verb = new("pda:ejectID");
                verb.Text = component.IdSlot.ContainedEntity!.Name;
                verb.Category = VerbCategory.Eject;
                verb.Act = () => component.HandleIDEjection(args.User);
                args.Verbs.Add(verb);
            }

            // eject pen
            if (!component.PenSlotEmpty)
            {
                Verb verb = new("pda:ejectPen");
                verb.Text = component.PenSlot.ContainedEntity!.Name;
                verb.Category = VerbCategory.Eject;
                verb.Act = () => component.HandlePenEjection(args.User);
                verb.Priority = -1; // ID takes priority.
                args.Verbs.Add(verb);
            }
        }

        private void AddInsertVerb(EntityUid uid, PDAComponent component, GetInteractionVerbsEvent args)
        {
            if (args.Using == null ||
                !args.CanAccess ||
                !args.CanInteract ||
                !_actionBlockerSystem.CanDrop(args.User))
                return;

            // insert ID
            if (component.IdSlotEmpty &&
                args.Using.TryGetComponent(out IdCardComponent? id))
            {
                Verb verb = new("pda:insertID");
                verb.Text = args.Using.Name;
                verb.Category = VerbCategory.Insert;
                verb.Act = () =>
                {
                    component.InsertIdCard(id);
                    component.UpdatePDAUserInterface();
                };
                args.Verbs.Add(verb);
            }

            // insert pen
            if (component.PenSlotEmpty &&
                args.Using.HasTag("Write"))
            {
                Verb verb = new("pda:insertPen");
                verb.Text = args.Using.Name;
                verb.Category = VerbCategory.Insert;
                verb.Act = () =>
                {
                    component.PenSlot.Insert(args.Using);
                    component.UpdatePDAUserInterface();
                };
                args.Verbs.Add(verb);
            }
        }
    }
}
