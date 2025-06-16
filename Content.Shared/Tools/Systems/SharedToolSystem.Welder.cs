using Content.Shared.ActionBlocker;
using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.Components.SolutionManager;
using Content.Shared.Database;
using Content.Shared.DoAfter;
using Content.Shared.Examine;
using Content.Shared.FixedPoint;
using Content.Shared.Interaction;
using Content.Shared.Item.ItemToggle.Components;
using Content.Shared.Tools.Components;

namespace Content.Shared.Tools.Systems;

public abstract partial class SharedToolSystem
{
    [Dependency] private readonly ActionBlockerSystem _actionBlocker = default!;

    public void InitializeWelder()
    {
        SubscribeLocalEvent<WelderComponent, ExaminedEvent>(OnWelderExamine);
        SubscribeLocalEvent<WelderComponent, AfterInteractEvent>(OnWelderAfterInteract);

        SubscribeLocalEvent<WelderComponent, ToolUseAttemptEvent>((uid, comp, ev) => {
            CanCancelWelderUse((uid, comp), ev.User, ev.Fuel, ev);
        });
        SubscribeLocalEvent<WelderComponent, DoAfterAttemptEvent<ToolDoAfterEvent>>((uid, comp, ev) => {
            CanCancelWelderUse((uid, comp), ev.Event.User, ev.Event.Fuel, ev);
        });
        SubscribeLocalEvent<WelderComponent, ToolDoAfterEvent>(OnWelderDoAfter);

        SubscribeLocalEvent<WelderComponent, ItemToggledEvent>(OnToggle);
        SubscribeLocalEvent<WelderComponent, ItemToggleActivateAttemptEvent>(OnActivateAttempt);
        SubscribeLocalEvent<WelderComponent, ItemToggleDeactivateAttemptEvent>(OnDeactivateAttempt);
    }

    public void TurnOn(Entity<WelderComponent> entity, EntityUid? user)
    {
        if (!SolutionContainerSystem.TryGetSolution(entity.Owner, entity.Comp.FuelSolutionName, out var solutionComp, out _))
            return;

        SolutionContainerSystem.RemoveReagent(solutionComp.Value, entity.Comp.FuelReagent, entity.Comp.FuelLitCost);
        AdminLogger.Add(LogType.InteractActivate, LogImpact.Low,
            $"{ToPrettyString(user):user} toggled {ToPrettyString(entity.Owner):welder} on");

        entity.Comp.Enabled = true;
        Dirty(entity, entity.Comp);
    }

    public void TurnOff(Entity<WelderComponent> entity, EntityUid? user)
    {
        AdminLogger.Add(LogType.InteractActivate, LogImpact.Low,
            $"{ToPrettyString(user):user} toggled {ToPrettyString(entity.Owner):welder} off");
        entity.Comp.Enabled = false;
        Dirty(entity, entity.Comp);
    }

    public (FixedPoint2 fuel, FixedPoint2 capacity) GetWelderFuelAndCapacity(EntityUid uid, WelderComponent? welder = null, SolutionContainerManagerComponent? solutionContainer = null)
    {
        if (!Resolve(uid, ref welder, ref solutionContainer))
            return default;

        if (!SolutionContainerSystem.TryGetSolution(
                (uid, solutionContainer),
                welder.FuelSolutionName,
                out _,
                out var fuelSolution))
        {
            return default;
        }

        return (fuelSolution.GetTotalPrototypeQuantity(welder.FuelReagent), fuelSolution.MaxVolume);
    }

    private void OnWelderExamine(Entity<WelderComponent> entity, ref ExaminedEvent args)
    {
        using (args.PushGroup(nameof(WelderComponent)))
        {
            if (ItemToggle.IsActivated(entity.Owner))
            {
                args.PushMarkup(Loc.GetString("welder-component-on-examine-welder-lit-message"));
            }
            else
            {
                args.PushMarkup(Loc.GetString("welder-component-on-examine-welder-not-lit-message"));
            }

            if (args.IsInDetailsRange)
            {
                var (fuel, capacity) = GetWelderFuelAndCapacity(entity.Owner, entity.Comp);

                args.PushMarkup(Loc.GetString("welder-component-on-examine-detailed-message",
                    ("colorName", fuel < capacity / FixedPoint2.New(4f) ? "darkorange" : "orange"),
                    ("fuelLeft", fuel),
                    ("fuelCapacity", capacity),
                    ("status", string.Empty))); // Lit status is handled above
            }
        }
    }

    private void OnWelderAfterInteract(Entity<WelderComponent> entity, ref AfterInteractEvent args)
    {
        if (args.Handled)
            return;

        if (args.Target is not { Valid: true } target || !args.CanReach)
            return;

        if (TryComp(target, out ReagentTankComponent? tank)
            && tank.TankType == ReagentTankType.Fuel
            && SolutionContainerSystem.TryGetDrainableSolution(target, out var targetSoln, out var targetSolution)
            && SolutionContainerSystem.TryGetSolution(entity.Owner, entity.Comp.FuelSolutionName, out var solutionComp, out var welderSolution))
        {
            var trans = FixedPoint2.Min(welderSolution.AvailableVolume, targetSolution.Volume);
            if (trans > 0)
            {
                var drained = SolutionContainerSystem.Drain(target, targetSoln.Value, trans);
                SolutionContainerSystem.TryAddSolution(solutionComp.Value, drained);
                _audioSystem.PlayPredicted(entity.Comp.WelderRefill, entity, user: args.User);
                _popup.PopupClient(Loc.GetString("welder-component-after-interact-refueled-message"), entity, args.User);
            }
            else if (welderSolution.AvailableVolume <= 0)
            {
                _popup.PopupClient(Loc.GetString("welder-component-already-full"), entity, args.User);
            }
            else
            {
                _popup.PopupClient(Loc.GetString("welder-component-no-fuel-in-tank", ("owner", args.Target)), entity, args.User);
            }

            args.Handled = true;
        }
    }

    private void CanCancelWelderUse(Entity<WelderComponent> entity, EntityUid user, float requiredFuel, CancellableEntityEventArgs ev)
    {
        if (!ItemToggle.IsActivated(entity.Owner))
        {
            _popup.PopupClient(Loc.GetString("welder-component-welder-not-lit-message"), entity, user);
            ev.Cancel();
        }

        var (currentFuel, _) = GetWelderFuelAndCapacity(entity);

        if (requiredFuel > currentFuel)
        {
            _popup.PopupClient(Loc.GetString("welder-component-cannot-weld-message"), entity, user);
            ev.Cancel();
        }
    }

    private void OnWelderDoAfter(Entity<WelderComponent> ent, ref ToolDoAfterEvent args)
    {
        if (args.Cancelled)
            return;

        if (!SolutionContainerSystem.TryGetSolution(ent.Owner, ent.Comp.FuelSolutionName, out var solution))
            return;

        SolutionContainerSystem.RemoveReagent(solution.Value, ent.Comp.FuelReagent, FixedPoint2.New(args.Fuel));
    }

    private void OnToggle(Entity<WelderComponent> entity, ref ItemToggledEvent args)
    {
        if (args.Activated)
            TurnOn(entity, args.User);
        else
            TurnOff(entity, args.User);
    }

    private void OnActivateAttempt(Entity<WelderComponent> entity, ref ItemToggleActivateAttemptEvent args)
    {
        if (args.User != null && !_actionBlocker.CanComplexInteract(args.User.Value)) {
            args.Cancelled = true;
            return;
        }

        if (!SolutionContainerSystem.TryGetSolution(entity.Owner, entity.Comp.FuelSolutionName, out _, out var solution))
        {
            args.Cancelled = true;
            args.Popup = Loc.GetString("welder-component-no-fuel-message");
            return;
        }

        var fuel = solution.GetTotalPrototypeQuantity(entity.Comp.FuelReagent);
        if (fuel == FixedPoint2.Zero || fuel < entity.Comp.FuelLitCost)
        {
            args.Popup = Loc.GetString("welder-component-no-fuel-message");
            args.Cancelled = true;
        }
    }

    private void OnDeactivateAttempt(Entity<WelderComponent> entity, ref ItemToggleDeactivateAttemptEvent args)
    {
        if (args.User != null && !_actionBlocker.CanComplexInteract(args.User.Value)) {
            args.Cancelled = true;
            return;
        }
    }
}
