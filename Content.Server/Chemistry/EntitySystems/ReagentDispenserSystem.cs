using Content.Shared.Verbs;
using Content.Server.Chemistry.Components;
using Content.Server.Chemistry.Components.SolutionManager;
using Content.Server.Construction.Components;
using JetBrains.Annotations;
using Robust.Shared.Containers;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Content.Shared.ActionBlocker;

namespace Content.Server.Chemistry.EntitySystems
{
    [UsedImplicitly]
    public class ReagentDispenserSystem : EntitySystem
    {
        [Dependency] private readonly SolutionContainerSystem _solutionContainerSystem = default!;
        [Dependency] private readonly ActionBlockerSystem _actionBlockerSystem = default!;

        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<ReagentDispenserComponent, SolutionChangedEvent>(OnSolutionChange);

            SubscribeLocalEvent<ReagentDispenserComponent, GetAlternativeVerbsEvent>(AddEjectVerb);
            SubscribeLocalEvent<ReagentDispenserComponent, GetInteractionVerbsEvent>(AddInsertVerb);
        }

        // TODO VERBS EJECTABLES Standardize eject/insert verbs into a single system? Maybe using something like the
        // system mentioned in #4538? The code here is basically identical to the stuff in ChemDispenserSystem.
        private void AddEjectVerb(EntityUid uid, ReagentDispenserComponent component, GetAlternativeVerbsEvent args)
        {
            if (args.Hands == null ||
                !args.CanAccess ||
                !args.CanInteract ||
                !component.HasBeaker ||
                !_actionBlockerSystem.CanPickup(args.User.Uid))
                return;

            Verb verb = new();
            verb.Act = () =>
            {
                component.TryEject(args.User);
                component.UpdateUserInterface();
            };
            verb.Category = VerbCategory.Eject;
            verb.Text = component.BeakerContainer.ContainedEntity!.Name;

            args.Verbs.Add(verb);
        }

        private void AddInsertVerb(EntityUid uid, ReagentDispenserComponent component, GetInteractionVerbsEvent args)
        {
            if (args.Using == null ||
                !args.CanAccess ||
                !args.CanInteract ||
                component.HasBeaker ||
                !args.Using.HasComponent<FitsInDispenserComponent>() ||
                !_actionBlockerSystem.CanDrop(args.User.Uid))
                return;

            Verb verb = new();
            verb.Act = () =>
            {
                component.BeakerContainer.Insert(args.Using);
                component.UpdateUserInterface();
            };
            verb.Category = VerbCategory.Insert;
            verb.Text = args.Using.Name;
            args.Verbs.Add(verb);
        }


        private void OnSolutionChange(EntityUid uid, ReagentDispenserComponent component, SolutionChangedEvent args)
        {
            component.UpdateUserInterface();
        }
    }
}
