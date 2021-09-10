using Content.Server.Medical.Components;
using Content.Shared.Verbs;
using JetBrains.Annotations;
using Robust.Shared.GameObjects;
using Robust.Shared.Localization;

namespace Content.Server.Medical
{
    [UsedImplicitly]
    internal sealed class MedicalScannerSystem : EntitySystem
    {
        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<MedicalScannerComponent, GetInteractionVerbsEvent>(AddInsertOtherVerb);
            SubscribeLocalEvent<MedicalScannerComponent, GetAlternativeVerbsEvent>(AddAlternativeVerbs);
        }

        private void AddInsertOtherVerb(EntityUid uid, MedicalScannerComponent component, GetInteractionVerbsEvent args)
        {
            if (!args.CanAccess || args.Hands == null)
                return;

            if (component.IsOccupied || args.Using == null || !component.CanInsert(args.Using))
                return;

            Verb verb = new("medscan:insert");
            verb.Act = () => component.InsertBody(args.Using);
            verb.Category = VerbCategory.Insert;
            args.Verbs.Add(verb);
        }

        private void AddAlternativeVerbs(EntityUid uid, MedicalScannerComponent component, GetAlternativeVerbsEvent args)
        {
            if (!args.CanAccess || args.Hands == null)
                return;

            // Eject verb
            if (component.IsOccupied)
            {
                Verb verb = new("medscan:eject");
                verb.Act = () => component.EjectBody();
                verb.Category = VerbCategory.Eject;
                args.Verbs.Add(verb);
            }

            // Self-insert verb
            if (!component.IsOccupied && component.CanInsert(args.User))
            {
                Verb verb = new("medscan:enter");
                verb.Act = () => component.InsertBody(args.User);
                verb.Text = Loc.GetString("enter-verb-get-data-text");
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
