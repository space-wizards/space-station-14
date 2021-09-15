using Content.Shared.Verbs;
using Content.Server.Chemistry.Components;
using Content.Shared.Chemistry.EntitySystems;
using JetBrains.Annotations;
using Robust.Shared.GameObjects;
using Content.Shared.Chemistry.Components.SolutionManager;
using Robust.Shared.IoC;
using Content.Shared.ActionBlocker;
using Robust.Shared.Localization;
using Content.Shared.Notification.Managers;
using Robust.Server.GameObjects;
using System.Collections.Generic;
using Content.Shared.Chemistry.Reagent;

namespace Content.Server.Chemistry.EntitySystems
{
	[UsedImplicitly]
    public class SolutionTransferSystem : EntitySystem
    {
        [Dependency] private readonly ActionBlockerSystem _actionBlockerSystem = default!;

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
            int priority = 0;
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

                // sort by size, not by alphabetical
                verb.Priority = priority;
                priority--;
                
                args.Verbs.Add(verb);
            }
        }
    }
}
