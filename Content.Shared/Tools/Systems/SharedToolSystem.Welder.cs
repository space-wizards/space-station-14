using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.Components.SolutionManager;
using Content.Shared.Chemistry.Components.Solutions;
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
    }

    public virtual void TurnOn(Entity<WelderComponent> entity, EntityUid? user)
    {
        if (!SolutionSystem.TryGetSolution(entity.Owner, entity.Comp.FuelSolutionName, out var solution))
            return;

        SolutionSystem.RemoveReagent(solution, (entity.Comp.FuelReagent.Id, entity.Comp.FuelLitCost), out _);
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

    public (FixedPoint2 fuel, FixedPoint2 capacity) GetWelderFuelAndCapacity(EntityUid uid, WelderComponent? welder = null,
        SolutionHolderComponent? solutionContainer = null)
    {
        if (!Resolve(uid, ref welder, ref solutionContainer))
            return default;

        if (!SolutionSystem.TryGetSolution(
                (uid, solutionContainer),
                welder.FuelSolutionName,
                out var fuelSolution))
        {
            return default;
        }

        return (SolutionSystem.GetTotalQuantity(fuelSolution, welder.FuelReagent), fuelSolution.Comp.MaxVolume);
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
            && TryComp(target, out SolutionHolderComponent? targetSolutionHolder)
            && SolutionSystem.TryGetFirstSolutionWithComp<DrainableSolutionComponent>((target, targetSolutionHolder),
                out var targetSolution)
            && SolutionSystem.TryGetSolution(entity.Owner, entity.Comp.FuelSolutionName, out var welderSolution))
        {
            var trans = FixedPoint2.Min(welderSolution.Comp.AvailableVolume, targetSolution.Comp1.Volume);
            if (trans > 0)
            {
                SolutionSystem.SplitSolution(targetSolution, welderSolution, out var overflow);
                _audioSystem.PlayPredicted(entity.Comp.WelderRefill, entity, user: args.User);
                _popup.PopupClient(Loc.GetString("welder-component-after-interact-refueled-message"), entity, args.User);
            }
            else if (welderSolution.Comp.AvailableVolume <= 0)
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
        if (!SolutionSystem.TryGetSolution(ent.Owner, ent.Comp.FuelSolutionName, out var solution))
            return;
        SolutionSystem.RemoveReagent(solution, (ent.Comp.FuelReagent.Id, FixedPoint2.New(args.Fuel)), out _);
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
        if (!SolutionSystem.TryGetSolution(entity.Owner, entity.Comp.FuelSolutionName, out var solution))
        {
            args.Cancelled = true;
            args.Popup = Loc.GetString("welder-component-no-fuel-message");
            return;
        }

        var fuel = SolutionSystem.GetTotalQuantity(solution,entity.Comp.FuelReagent);
        if (fuel == FixedPoint2.Zero || fuel < entity.Comp.FuelLitCost)
        {
            args.Popup = Loc.GetString("welder-component-no-fuel-message");
            args.Cancelled = true;
        }
    }
}
