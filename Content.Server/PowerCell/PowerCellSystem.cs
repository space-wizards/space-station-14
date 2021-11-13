using Content.Server.Chemistry.EntitySystems;
using Content.Server.PowerCell.Components;
using Content.Shared.ActionBlocker;
using Content.Shared.Verbs;
using JetBrains.Annotations;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;

namespace Content.Server.PowerCell
{
    [UsedImplicitly]
    public class PowerCellSystem  : EntitySystem
    {
        [Dependency] private readonly SolutionContainerSystem _solutionsSystem = default!;
        [Dependency] private readonly ActionBlockerSystem _actionBlockerSystem = default!;

        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<PowerCellComponent, SolutionChangedEvent>(OnSolutionChange);
            SubscribeLocalEvent<PowerCellSlotComponent, GetAlternativeVerbsEvent>(AddEjectVerb);
            SubscribeLocalEvent<PowerCellSlotComponent, GetInteractionVerbsEvent>(AddInsertVerb);
        }

        // TODO VERBS EJECTABLES Standardize eject/insert verbs into a single system?
        private void AddEjectVerb(EntityUid uid, PowerCellSlotComponent component, GetAlternativeVerbsEvent args)
        {
            if (args.Hands == null ||
                !args.CanAccess ||
                !args.CanInteract ||
                !component.ShowVerb ||
                !component.HasCell ||
                !_actionBlockerSystem.CanPickup(args.User.Uid))
                return;

            Verb verb = new();
            verb.Text = component.Cell!.Name;
            verb.Category = VerbCategory.Eject;
            verb.Act = () => component.EjectCell(args.User);
            args.Verbs.Add(verb);
        }

        private void AddInsertVerb(EntityUid uid, PowerCellSlotComponent component, GetInteractionVerbsEvent args)
        {
            if (args.Using == null ||
                !args.CanAccess ||
                !args.CanInteract ||
                component.HasCell ||
                !args.Using.HasComponent<PowerCellComponent>() ||
                !_actionBlockerSystem.CanDrop(args.User.Uid))
                return;

            Verb verb = new();
            verb.Text = args.Using.Name;
            verb.Category = VerbCategory.Insert;
            verb.Act = () => component.InsertCell(args.Using);
            args.Verbs.Add(verb);
        }

        private void OnSolutionChange(EntityUid uid, PowerCellComponent component, SolutionChangedEvent args)
        {
            component.IsRigged =  _solutionsSystem.TryGetSolution(uid, PowerCellComponent.SolutionName, out var solution)
                                   && solution.ContainsReagent("Plasma", out var plasma)
                                   && plasma >= 5;
        }
    }
}
