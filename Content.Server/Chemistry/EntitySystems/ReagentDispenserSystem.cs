using Content.Shared.Verbs;
using Robust.Shared.GameObjects;
using Content.Server.Chemistry.Components;
using Content.Shared.Chemistry.Solution;

namespace Content.Server.Chemistry.EntitySystems
{
    // TODO ECS ChemMasterComponent
    /// <summary>
    ///     Barebones system for ChemMasterComponent.
    /// </summary>
    public class ReagentDispenserSystem : EntitySystem
    {
        public override void Initialize()
        {
            SubscribeLocalEvent<ReagentDispenserComponent, GetAlternativeVerbsEvent>(AddEjectVerb);
            SubscribeLocalEvent<ReagentDispenserComponent, GetInteractionVerbsEvent>(AddInsertVerb);
        }

        // TODO VERBS EJECTABLES Standardize eject/insert verbs into a single system? Maybe using something like the
        // system mentioned in #4538? The code here is basically identical to the stuff in ChemDispenserSystem
        private void AddEjectVerb(EntityUid uid, ReagentDispenserComponent component, GetAlternativeVerbsEvent args)
        {
            // only physical interactions
            if (!args.DefaultInRangeUnobstructed || args.Hands == null || !component.HasBeaker)
                return;


            Verb verb = new("ReagentDispenser:Eject");
            verb.Act = () =>
            {
                component.TryEject(args.User);
                component.UpdateUserInterface();
            };

            if (args.PrepareGUI)
            {
                verb.Category = VerbCategories.Eject;
            }

            args.Verbs.Add(verb);
        }

        private void AddInsertVerb(EntityUid uid, ReagentDispenserComponent component, GetInteractionVerbsEvent args)
        {
            // Check if we are holding an applicable solution container and can insert it.
            if (component.HasBeaker ||
                args.Using == null ||
                !args.Using.TryGetComponent<SolutionContainerComponent>(out var solution) ||
                !solution.Capabilities.HasFlag(SolutionContainerCaps.FitsInDispenser))
            {
                return;
            }
            Verb verb = new("ReagentDispenser:Insert");
            verb.Act = () =>
            {
                component.BeakerContainer.Insert(args.Using);
                component.UpdateUserInterface();
            };

            if (args.PrepareGUI)
            {
                verb.Category = VerbCategories.Insert;
            }

            args.Verbs.Add(verb);
        }
    }
}
