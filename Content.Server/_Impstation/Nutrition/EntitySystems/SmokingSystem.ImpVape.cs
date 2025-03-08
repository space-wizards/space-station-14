using Content.Server._Impstation.Nutrition.Components;
using Content.Server.Body.Components;
using Content.Server.Power.Components;
using Content.Server.PowerCell;
using Content.Shared.Atmos;
using Content.Shared.Chemistry;
using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.DoAfter;
using Content.Shared.Emag.Systems;
using Content.Shared.Examine;
using Content.Shared.FixedPoint;
using Content.Shared.IdentityManagement;
using Content.Shared.Interaction;
using Content.Shared.Interaction.Events;
using Content.Shared.Nutrition;
using Content.Shared.Smoking;
using Robust.Shared.Containers;
using Robust.Shared.Prototypes;
using System.Diagnostics.CodeAnalysis;


namespace Content.Server.Nutrition.EntitySystems
{
    public sealed partial class SmokingSystem
    {
        [Dependency] private readonly FlavorProfileSystem _flavorProfile = default!;
        [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
        [Dependency] private readonly PowerCellSystem _powerCell = default!;

        private void InitializeImpVapes()
        {
            SubscribeLocalEvent<VapePenComponent, ExaminedEvent>(OnExamine);
            SubscribeLocalEvent<VapePenComponent, EntInsertedIntoContainerMessage>(OnInsertInSlot);
            SubscribeLocalEvent<VapePenComponent, EntRemovedFromContainerMessage>(OnRemoveFromSlot);
            SubscribeLocalEvent<VapePenComponent, UseInHandEvent>(OnVapeUseInHand);
            SubscribeLocalEvent<VapePenComponent, AfterInteractEvent>(OnVapeInteraction);
            SubscribeLocalEvent<VapePenComponent, VapeDoAfterEvent>(OnVapeDoAfter);
            SubscribeLocalEvent<VapePenComponent, GotEmaggedEvent>(OnEmagged);
        }

        private void OnRemoveFromSlot(Entity<VapePenComponent> ent, ref EntRemovedFromContainerMessage args)
        {
            UpdateAppearance(ent);
        }

        private void OnInsertInSlot(Entity<VapePenComponent> ent, ref EntInsertedIntoContainerMessage args)
        {
            UpdateAppearance(ent);
        }

        private void UpdateAppearance(Entity<VapePenComponent> ent)
        {
            var hasCart = TryGetCartFromSlot(ent.Owner, ent.Comp, out var cartEnt);

            ent.Comp.FillLevel = 0;

            if (hasCart && cartEnt != null && _solutionContainerSystem.TryGetSolution(cartEnt.Value, "smokable", out _, out var solution))
            {
                ent.Comp.FillLevel = (float)(solution.Volume / solution.MaxVolume);

                _appearance.SetData(ent, SolutionContainerVisuals.Color, solution.GetColor(_prototypeManager));
            }

            _appearance.SetData(ent, SmokingVisuals.CartInserted, hasCart);
            _appearance.SetData(ent, SolutionContainerVisuals.FillFraction, ent.Comp.FillLevel);
        }

        public bool TryGetCartFromSlot(EntityUid uid, VapePenComponent vape,
            [NotNullWhen(true)] out EntityUid? cartEnt)
        {
            if (_itemSlotsSystem.TryGetSlot(uid, vape.CartSlotId, out var slot))
            {
                cartEnt = slot.Item;
                return slot.HasItem;
            }

            cartEnt = null;
            return false;
        }

        private void OnExamine(EntityUid uid, VapePenComponent component, ExaminedEvent args)
        {
            _powerCell.TryGetBatteryFromSlot(uid, out var battery);
            var charges = UsesRemaining(component, battery);
            var maxCharges = MaxUses(component, battery);

            using (args.PushGroup(nameof(VapePenComponent)))
            {
                args.PushMarkup(Loc.GetString("limited-charges-charges-remaining", ("charges", charges)));

                if (charges > 0 && charges == maxCharges)
                {
                    args.PushMarkup(Loc.GetString("limited-charges-max-charges"));
                }
            }
        }

        private void OnVapeUseInHand(Entity<VapePenComponent> entity, ref UseInHandEvent args)
        {
            if (args.Handled)
            {
                return;
            }

            if (VapePuff(entity, args.User, args.User, true))
            {
                args.Handled = true;
            }
        }

        private void OnVapeInteraction(Entity<VapePenComponent> entity, ref AfterInteractEvent args)
        {
            if (args.Handled || !args.Target.HasValue)
            {
                return;
            }

            if (VapePuff(entity, args.User, args.Target.Value, args.CanReach))
            {
                args.Handled = true;
            }
        }

        private bool VapePuff(Entity<VapePenComponent> entity, EntityUid user, EntityUid target, bool canReach)
        {
            var delay = entity.Comp.Delay;
            var forced = true;
            var exploded = false;

            if (!TryGetCartFromSlot(entity.Owner, entity.Comp, out var cart))
            {
                _popupSystem.PopupEntity(
                    Loc.GetString("vapepen-component-vape-cart-slot-empty"), target,
                    user);
                return false;
            }

            if (!canReach
                || !_solutionContainerSystem.TryGetSolution(cart.Value, "smokable", out _, out var solution)
                || !HasComp<BloodstreamComponent>(target)
                || _foodSystem.IsMouthBlocked(target, user))
            {
                return false;
            }

            if (solution.Contents.Count == 0)
            {
                _popupSystem.PopupEntity(
                    Loc.GetString("vapecart-component-vape-cart-solution-empty"), target,
                    user);
                return false;
            }

            var sufficientSolvent = false;
            foreach (var solvent in entity.Comp.AcceptableSolvents)
            {
                var reagPercentage = FixedPoint2.New(100) * solution.GetReagentQuantity(solvent.Reagent) / solution.Volume;
                if (reagPercentage >= solvent.Quantity)
                {
                    sufficientSolvent = true;
                }
            }
            if (!sufficientSolvent)
            {
                _popupSystem.PopupEntity(Loc.GetString("vapepen-component-vape-incompatible"), target, user);
                return false;
            }

            // if no battery or no charge, doesn't work
            if (!_powerCell.TryUseCharge(entity.Owner, entity.Comp.ChargeUse, user: user))
                return false;

            if (target == user)
            {
                delay = entity.Comp.UserDelay;
                forced = false;
            }

            var invalidQuantity = false;
            foreach (var solute in entity.Comp.UnstableReagent)
            {
                var soluteQuantity = solution.GetReagentQuantity(solute.Reagent);
                if (soluteQuantity >= solute.Quantity)
                {
                    invalidQuantity = true;
                }
            }
            if (entity.Comp.ExplodeOnUse
                || _emag.CheckFlag(entity, EmagType.Interaction)
                || invalidQuantity)
            {
                exploded = true;
                _explosionSystem.QueueExplosion(entity.Owner, "Default", entity.Comp.ExplosionIntensity, 0.5f, 3, canCreateVacuum: false);
                EntityManager.DeleteEntity(entity);
            }

            if (forced)
            {
                var targetName = Identity.Entity(target, EntityManager);
                var userName = Identity.Entity(user, EntityManager);

                _popupSystem.PopupEntity(
                    Loc.GetString("vape-component-try-use-vape-forced", ("user", userName)), target,
                    target);

                _popupSystem.PopupEntity(
                    Loc.GetString("vape-component-try-use-vape-forced-user", ("target", targetName)), user,
                    user);
            }

            if (!exploded)
            {
                var vapeDoAfterEvent = new VapeDoAfterEvent(solution, forced);
                _doAfterSystem.TryStartDoAfter(new DoAfterArgs(EntityManager, user, delay, vapeDoAfterEvent, entity.Owner, target: target, used: entity.Owner)
                {
                    BreakOnMove = false,
                    BreakOnDamage = true
                });
            }
            return true;
        }

        private void OnVapeDoAfter(Entity<VapePenComponent> entity, ref VapeDoAfterEvent args)
        {

            if (args.Cancelled || args.Handled || !args.Args.Target.HasValue || !TryComp<BloodstreamComponent>(args.Args.Target.Value, out var bloodstream))
                return;

            if (!TryGetCartFromSlot(entity.Owner, entity.Comp, out var cart))
            {
                _popupSystem.PopupEntity(
                    Loc.GetString("vapepen-component-vape-cart-slot-empty"), args.Args.Target.Value,
                    args.Args.User);
                return;
            }

            if (!TryComp(cart.Value, out VapeCartComponent? cartComp))
            {
                return;
            }

            var environment = _atmos.GetContainingMixture(args.Args.Target.Value, true, true);
            if (environment == null)
            {
                return;
            }

            //Smoking kills(your lungs, but there is no organ damage yet)
            if (!cartComp.IgnoreDamage)
            {
                _damageableSystem.TryChangeDamage(args.Args.Target.Value, entity.Comp.Damage, true);
            }

            var drawQuantity = Math.Min(args.Solution.Volume.Value, 3.0f);

            var merger = new GasMixture(1) { Temperature = args.Solution.Temperature };

            var flavors = "";

            if (_solutionContainerSystem.TryGetSolution(cart.Value, "smokable", out var soln))
            {
                var intake = _solutionContainerSystem.SplitSolution(soln.Value, FixedPoint2.New(drawQuantity));
                foreach (var reagent in intake.Contents)
                {
                    var vapors = VaporizeReagent(reagent);
                    foreach (var vapor in vapors)
                    {
                        if (vapor.Solution != null)
                        {
                            vapor.Solution.ScaleSolution(vapor.Proportion * cartComp.FlavorMultiplicationFactor);
                            _bloodstreamSystem.TryAddToChemicals(args.Args.Target.Value, vapor.Solution, bloodstream);
                        }
                        if (vapor.Gas.HasValue)
                        {
                            merger.AdjustMoles(vapor.Gas.Value, vapor.Proportion * reagent.Quantity.Float() / cartComp.GasReductionFactor);
                        }
                    }
                }
                flavors = _flavorProfile.GetLocalizedFlavorsMessage(args.Args.User, intake);
            }

            _atmos.Merge(environment, merger);

            if (args.Forced)
            {
                var targetName = Identity.Entity(args.Args.Target.Value, EntityManager);
                var userName = Identity.Entity(args.Args.User, EntityManager);

                _popupSystem.PopupEntity(
                    Loc.GetString("vapepen-component-vape-success-forced", ("user", userName), ("flavors", flavors)), args.Args.Target.Value,
                    args.Args.Target.Value);

                _popupSystem.PopupEntity(
                    Loc.GetString("vape-component-vape-success-user-forced", ("target", targetName)), args.Args.User,
                    args.Args.Target.Value);
            }
            else
            {
                _popupSystem.PopupEntity(
                    Loc.GetString("vapepen-component-vape-success", ("flavors", flavors)), args.Args.Target.Value,
                    args.Args.Target.Value);
            }

            UpdateAppearance(entity);
        }

        private void OnEmagged(Entity<VapePenComponent> entity, ref GotEmaggedEvent args)
        {
            if (!_emag.CompareFlag(args.Type, EmagType.Interaction))
                return;

            if (_emag.CheckFlag(entity, EmagType.Interaction))
                return;

            args.Handled = true;
        }

        private int UsesRemaining(VapePenComponent component, BatteryComponent? battery = null)
        {
            if (battery == null ||
                component.ChargeUse == 0f) return 0;

            return (int)(battery.CurrentCharge / component.ChargeUse);
        }

        private int MaxUses(VapePenComponent component, BatteryComponent? battery = null)
        {
            if (battery == null ||
                component.ChargeUse == 0f) return 0;

            return (int)(battery.MaxCharge / component.ChargeUse);
        }

        // TODO: Make this a config file!! We need more phase transitions!!!
        private static List<VapeResult> VaporizeReagent(ReagentQuantity reagent)
        {
            List<VapeResult> vapors = [];
            switch (reagent.Reagent.Prototype)
            {
                case "Sugar":
                    vapors.Add(new VapeResult(Gas.WaterVapor, 2 / 3.0f));
                    vapors.Add(new VapeResult(Gas.Ammonia, 1 / 3.0f));
                    break;
                case "Water":
                    vapors.Add(new VapeResult(Gas.WaterVapor));
                    break;
                case "Ammonia":
                    vapors.Add(new VapeResult(Gas.Ammonia));
                    break;
                case "Plasma":
                    vapors.Add(new VapeResult(Gas.Plasma));
                    break;
                case "Trit":
                    vapors.Add(new VapeResult(Gas.Tritium));
                    break;
                default:
                    vapors.Add(new VapeResult(new Solution([reagent])));
                    break;
            }
            return vapors;
        }
    }

    public struct VapeResult
    {
        public VapeResult(Solution solution, float proportion = 1.0f)
        {
            Solution = solution;
            Gas = null;
            Proportion = proportion;
        }
        public VapeResult(Gas gas, float proportion = 1.0f)
        {
            Solution = null;
            Gas = gas;
            Proportion = proportion;
        }
        public Solution? Solution { get; set; }
        public Gas? Gas { get; set; }
        public float Proportion { get; set; }
    }
}
