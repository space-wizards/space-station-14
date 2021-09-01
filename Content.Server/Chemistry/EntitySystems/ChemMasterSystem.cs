using Content.Shared.Verbs;
using Robust.Shared.GameObjects;
using Content.Server.Chemistry.Components;

namespace Content.Server.Chemistry.EntitySystems
{
    // TODO ECS ChemMasterComponent
    /// <summary>
    ///     Barebones system for ChemMasterComponent.
    /// </summary>
    public class ChemMasterSysten : EntitySystem
    {
        public override void Initialize()
        {
            SubscribeLocalEvent<ChemMasterComponent, AssembleVerbsEvent>(AddBeakerVerbs);
        }

        // TODO VERBS EJECTABLES Standardize eject/insert verbs into a single system? Maybe using something like the
        // system mentioned in #4538? The code here is basically identical to the stuff in ChemDispenserSystem
        private void AddBeakerVerbs(EntityUid uid, ChemMasterComponent component, AssembleVerbsEvent args)
        {
            // only physical interactions
            if (!args.DefaultInRangeUnobstructed || args.Hands == null)
                return;

            // If the dispenser has a beaker, add a verb to eject it.
            if (args.Types.HasFlag(VerbTypes.Alternative) && component.HasBeaker)
            {
                Verb verb = new("ChemDispenser:Eject");
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

            // If holding an applicable solution container, add a verb to insert it.
            if (args.Types.HasFlag(VerbTypes.Interact) &&
                !component.HasBeaker &&
                args.Using != null &&
                args.Using.TryGetComponent<SolutionContainerComponent>(out var solution) &&
                solution.CanUseWithChemDispenser)
            {   
                Verb verb = new("ChemDispenser:Insert");
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
}
