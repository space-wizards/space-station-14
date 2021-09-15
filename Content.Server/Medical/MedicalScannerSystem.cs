using Content.Server.Medical.Components;
using Content.Shared.ActionBlocker;
using Content.Shared.Verbs;
using JetBrains.Annotations;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Localization;

namespace Content.Server.Medical
{
    [UsedImplicitly]
    internal sealed class MedicalScannerSystem : EntitySystem
    {
        [Dependency] private readonly ActionBlockerSystem _actionBlockerSystem = default!;

        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<MedicalScannerComponent, GetInteractionVerbsEvent>(AddInsertOtherVerb);
            SubscribeLocalEvent<MedicalScannerComponent, GetAlternativeVerbsEvent>(AddAlternativeVerbs);
        }

        private void AddInsertOtherVerb(EntityUid uid, MedicalScannerComponent component, GetInteractionVerbsEvent args)
        {
            if (args.Using == null ||
                !args.CanAccess ||
                !args.CanInteract ||
                component.IsOccupied ||
                !component.CanInsert(args.Using))
                return;

            Verb verb = new();
            verb.Act = () => component.InsertBody(args.Using);
            verb.Category = VerbCategory.Insert;
            verb.Text = args.Using.Name;
            args.Verbs.Add(verb);
        }

        private void AddAlternativeVerbs(EntityUid uid, MedicalScannerComponent component, GetAlternativeVerbsEvent args)
        {
            if (!args.CanAccess || !args.CanInteract)
                return;

            // Eject verb
            if (component.IsOccupied)
            {
                Verb verb = new();
                verb.Act = () => component.EjectBody();
                verb.Category = VerbCategory.Eject;
                verb.Text = Loc.GetString("medical-scanner-verb-noun-occupant");
                args.Verbs.Add(verb);
            }

            // Self-insert verb
            if (!component.IsOccupied &&
                component.CanInsert(args.User) &&
                _actionBlockerSystem.CanMove(args.User))
            {
                Verb verb = new();
                verb.Act = () => component.InsertBody(args.User);
                verb.Text = Loc.GetString("medical-scanner-verb-enter");
                // TODO VERN ICON
                // TODO VERB CATEGORY
                // create a verb category for "enter"?
                // See also, disposal unit.  Also maybe add verbs for entering lockers/body bags?
                args.Verbs.Add(verb);
            }
        }

        public override void Update(float frameTime)
        {
            foreach (var comp in ComponentManager.EntityQuery<MedicalScannerComponent>(true))
            {
                comp.Update(frameTime);
            }
        }
    }
}
