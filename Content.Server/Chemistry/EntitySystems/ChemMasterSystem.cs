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
            SubscribeLocalEvent<ChemMasterComponent, GetInteractionVerbsEvent>(AddInsertVerb);
            SubscribeLocalEvent<ChemMasterComponent, GetAlternativeVerbsEvent>(AddEjectVerb);
        }

        // TODO VERBS EJECTABLES Standardize eject/insert verbs into a single system? Maybe using something like the
        // system mentioned in #4538? The code here is basically identical to the stuff in ChemDispenserSystem
        private void AddEjectVerb(EntityUid uid, ChemMasterComponent component, GetAlternativeVerbsEvent args)
        {
            if (!args.DefaultInRangeUnobstructed || args.Hands == null || !component.HasBeaker)
                return;

            Verb verb = new("ChemMaster:Eject");
            verb.Act = () =>
            {
                component.TryEject(args.User);
                component.UpdateUserInterface();
            };

            if (args.PrepareGUI)
            {
                verb.Category = VerbCategory.Eject;
            }

            args.Verbs.Add(verb);
        }

        private void AddInsertVerb(EntityUid uid, ChemMasterComponent component, GetInteractionVerbsEvent args)
        {
            // check if we are holding an applicable solution container and can insert it.
            if (component.HasBeaker ||
                args.Using == null ||
                !args.Using.TryGetComponent<SolutionContainerComponent>(out var solution) ||
                !solution.CanUseWithChemDispenser)
            {
                return;
            }

            Verb verb = new("ChemMaster:Insert");
            verb.Act = () =>
            {
                component.BeakerContainer.Insert(args.Using);
                component.UpdateUserInterface();
            };

            if (args.PrepareGUI)
            {
                verb.Category = VerbCategory.Insert;
            }

            args.Verbs.Add(verb);
        }
    }
}
