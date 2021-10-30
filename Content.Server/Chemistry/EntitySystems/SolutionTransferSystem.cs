using Content.Shared.Verbs;
using Content.Server.Chemistry.Components;
using JetBrains.Annotations;
using Robust.Shared.GameObjects;
using Robust.Shared.Localization;
using Robust.Server.GameObjects;
using System.Collections.Generic;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.Popups;

namespace Content.Server.Chemistry.EntitySystems
{
	[UsedImplicitly]
    public class SolutionTransferSystem : EntitySystem
    {
        /// <summary>
        ///     Default transfer amounts for the set-transfer verb.
        /// </summary>
        public static readonly List<int> DefaultTransferAmounts = new() { 1, 5, 10, 25, 50, 100, 250, 500, 1000};

        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<SolutionTransferComponent, GetAlternativeVerbsEvent>(AddSetTransferVerbs);
        }

        private void AddSetTransferVerbs(EntityUid uid, SolutionTransferComponent component, GetAlternativeVerbsEvent args)
        {
            if (!args.CanAccess || !args.CanInteract || !component.CanChangeTransferAmount)
                return;

            if (!args.User.TryGetComponent<ActorComponent>(out var actor))
                return;
            
            // Custom transfer verb
            Verb custom = new();
            custom.Text = Loc.GetString("comp-solution-transfer-verb-custom-amount");
            custom.Category = VerbCategory.SetTransferAmount;
            custom.Act = () => component.UserInterface?.Open(actor.PlayerSession);
            custom.Priority = 1;
            args.Verbs.Add(custom);

            // Add specific transfer verbs according to the container's size
            var priority = 0;
            foreach (var amount in DefaultTransferAmounts)
            {
                if ( amount < component.MinimumTransferAmount.Int() || amount > component.MaximumTransferAmount.Int())
                    continue;

                Verb verb = new();
                verb.Text = Loc.GetString("comp-solution-transfer-verb-amount", ("amount", amount));
                verb.Category = VerbCategory.SetTransferAmount;
                verb.Act = () =>
                {
                    component.TransferAmount = ReagentUnit.New(amount);
                    args.User.PopupMessage(Loc.GetString("comp-solution-transfer-set-amount", ("amount", amount)));
                };

                // we want to sort by size, not alphabetically by the verb text.
                verb.Priority = priority;
                priority--;
                
                args.Verbs.Add(verb);
            }
        }
    }
}
