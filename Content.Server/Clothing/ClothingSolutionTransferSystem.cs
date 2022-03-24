using Content.Server.UserInterface;
using Content.Shared.FixedPoint;
using Content.Shared.Interaction;
using Content.Shared.Popups;
using Robust.Server.GameObjects;
using Robust.Shared.GameObjects;
using Robust.Shared.Localization;
using Content.Shared.Actions.ActionTypes;
using Content.Shared.Actions;
using Content.Shared.Toggleable;
using Content.Server.Clothing.Components;
using Content.Shared.Clothing;
using Content.Shared.Verbs;
using Content.Server.Nutrition.EntitySystems;
using Content.Server.Nutrition.Components;

namespace Content.Server.Clothing
{
    public sealed class ClothingSolutionTransferSystem : EntitySystem
    {
        [Dependency] private readonly DrinkSystem _drinkSystem = default!;

        /// <summary>
        ///     Default transfer amounts for the set-transfer verb.
        /// </summary>
        public static readonly List<int> DefaultTransferAmounts = new() { 1, 5, 10, 25, 50, 100, 250, 500, 1000 };

        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<ClothingSolutionTransferComponent, GetVerbsEvent<ActivationVerb>>(AddTransferVerb);
            SubscribeLocalEvent<ClothingSolutionTransferComponent, GetVerbsEvent<AlternativeVerb>>(AddSetTransferVerbs);
        }
        //TODO: add sidebar action

        //custom verb to move solution from clothing item to wearer
        private void AddTransferVerb(EntityUid uid, ClothingSolutionTransferComponent component, GetVerbsEvent<ActivationVerb> args)
        {
            if (!args.CanAccess || !args.CanInteract)
                return;

            ActivationVerb verb = new();
            verb.Text = Loc.GetString("transfer-solution-verb-get-data-text");
            verb.IconEntity = uid;
            verb.Act = () => PerformerDrink(uid, component, args);

            args.Verbs.Add(verb);
        }

        private void PerformerDrink(EntityUid uid, ClothingSolutionTransferComponent component, GetVerbsEvent<ActivationVerb> args)
        {
            component.CanChangeTransferAmount = false;
            _drinkSystem.TryDrink(args.User, args.User, EntityManager.GetComponent<DrinkComponent>(uid));
        }

        private void AddSetTransferVerbs(EntityUid uid, ClothingSolutionTransferComponent component, GetVerbsEvent<AlternativeVerb> args)
        {
            if (!args.CanAccess || !args.CanInteract || !component.CanChangeTransferAmount)
                return;

            if (!EntityManager.TryGetComponent<ActorComponent?>(args.User, out var actor))
                return;

            // Custom transfer verb
            AlternativeVerb custom = new();
            custom.Text = Loc.GetString("comp-solution-transfer-verb-custom-amount");
            custom.Category = VerbCategory.SetTransferAmount;
            custom.Priority = 1;
            args.Verbs.Add(custom);

            // Add specific transfer verbs according to the container's size
            var priority = 0;
            foreach (var amount in DefaultTransferAmounts)
            {
                if (amount < component.MinimumTransferAmount.Int() || amount > component.MaximumTransferAmount.Int())
                    continue;

                AlternativeVerb verb = new();
                verb.Text = Loc.GetString("comp-solution-transfer-verb-amount", ("amount", amount));
                verb.Category = VerbCategory.SetTransferAmount;
                verb.Act = () =>
                {
                    EntityManager.GetComponent<DrinkComponent>(uid).TransferAmount = FixedPoint2.New(amount);
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
