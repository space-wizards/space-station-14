using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.Components.SolutionManager;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.Examine;
using Content.Shared.FixedPoint;
using Content.Shared.Interaction;
using Content.Shared.Item.ItemToggle;
using Content.Shared.Item.ItemToggle.Components;
using Content.Shared.Lighter.Components;
using Content.Shared.Popups;
using Content.Shared.Tools.Components;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Timing;


namespace Content.Shared.Lighter.Systems;

public sealed partial class LighterSystem : EntitySystem
{
    [Dependency] private SharedSolutionContainerSystem _solutionContainer = default!;
    [Dependency] private ItemToggleSystem _itemToggle = default!;
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private SharedPopupSystem _popup = default!;
    [Dependency] private SharedAudioSystem _audio = default!;

    public void TurnOn(Entity<LighterComponent> entity)
    {
        if (!_solutionContainer.TryGetSolution(entity.Owner, entity.Comp.FuelSolutionName, out var solutionComp, out _))
            return;

        _solutionContainer.RemoveReagent(solutionComp.Value, entity.Comp.FuelReagent, entity.Comp.FuelLitCost);

        entity.Comp.Enabled = true;
        Dirty(entity, entity.Comp);
    }

    public void TurnOff(Entity<LighterComponent> entity)
    {
        entity.Comp.Enabled = false;
        Dirty(entity, entity.Comp);
    }

    public (FixedPoint2 fuel, FixedPoint2 capacity) GetFuelAndCapacity(Entity<LighterComponent> entity, SolutionManagerComponent? solutionManager = null)
    {
        var lighter = entity.Comp;

        if (!_solutionContainer.TryGetSolution(
            (entity.Owner, solutionManager),
            lighter.FuelSolutionName,
            out _,
            out var fuelSolution))
        {
            return default;
        }

        return (fuelSolution.GetTotalPrototypeQuantity(lighter.FuelReagent), fuelSolution.MaxVolume);
    }

    [SubscribeLocalEvent]
    private void OnExamine(Entity<LighterComponent> entity, ref ExaminedEvent args)
    {
        using (args.PushGroup(nameof(LighterComponent)))
        {
            if (_itemToggle.IsActivated(entity.Owner))
            {
                args.PushMarkup(Loc.GetString("lighter-component-on-examine-lighter-lit-message"));
            }
            else
            {
                args.PushMarkup(Loc.GetString("lighter-component-on-examine-lighter-not-lit-message"));
            }

            if (args.IsInDetailsRange)
            {
                var (fuel, capacity) = GetFuelAndCapacity(entity);

                args.PushMarkup(Loc.GetString("lighter-component-on-examine-detailed-message",
                    ("colorName", fuel > capacity / FixedPoint2.New(4f) ? "darkorange" : "orange"),
                    ("fuelLeft", fuel),
                    ("fuelCapacity", capacity),
                    ("status", string.Empty)));
            }
        }
    }

    [SubscribeLocalEvent]
    private void OnLighterInit(Entity<LighterComponent> entity, ref MapInitEvent args)
    {
        entity.Comp.NextUpdate = entity.Comp.LighterUpdateTimer;
        Dirty(entity);
    }

    [SubscribeLocalEvent]
    private void OnToggle(Entity<LighterComponent> entity, ref ItemToggledEvent args)
    {
        if (args.Activated)
            TurnOn(entity);
        else
            TurnOff(entity);
    }

    /// <summary>
    /// Checks lighter fuel cost against available fuel before allowing activation.
    /// </summary>
    /// <param name="entity"></param>
    /// <param name="args"></param>
    [SubscribeLocalEvent]
    private void OnActivateAttempt(Entity<LighterComponent> entity, ref ItemToggleActivateAttemptEvent args)
    {
        if (!_solutionContainer.TryGetSolution(entity.Owner, entity.Comp.FuelSolutionName, out _, out var solution))
        {
            args.Cancelled = true;
            args.Popup = Loc.GetString("lighter-component-no-fuel-message", ("owner", entity.Owner));
            return;
        }

        var fuel = solution.GetTotalPrototypeQuantity(entity.Comp.FuelReagent);
        if (fuel < entity.Comp.FuelLitCost)
        {
            args.Popup = Loc.GetString("lighter-component-no-fuel-message", ("owner", entity.Owner));
            args.Cancelled = true;
        }
    }

    private void UpdateLighters()
    {
        var query = EntityQueryEnumerator<LighterComponent>();
        var curTime = _timing.CurTime;

        while (query.MoveNext(out var uid, out var lighter))
        {
            if (curTime < lighter.NextUpdate)
                continue;

            lighter.NextUpdate = curTime + lighter.LighterUpdateTimer;
            Dirty(uid, lighter);

            if (!lighter.Enabled)
                continue;
            if (!_solutionContainer.TryGetSolution(uid, lighter.FuelSolutionName, out var solutionComp, out var solution))
                continue;

            _solutionContainer.RemoveReagent(solutionComp.Value, lighter.FuelReagent, lighter.FuelConsumption * lighter.LighterUpdateTimer.TotalSeconds);

            if (solution.GetTotalPrototypeQuantity(lighter.FuelReagent) <= FixedPoint2.Zero)
                _itemToggle.Toggle(uid);
        }
    }

    /// <summary>
    /// Generic refill logic.
    /// Ensures the target's solution contains a matching ReagentId before allowing the lighter to refuel from it.
    /// Allows for refueling from solutions where only a portion is the appropriate ReagentId. Any other reagents in the solution will take up some of the lighters max volume, permanently. In this case, the lighter's capacity will reflect the offset instead of showing it completely full. 
    /// </summary>
    /// <param name="entity"></param>
    /// <param name="args"></param>
    [SubscribeLocalEvent]
    private void OnLighterAfterInteract(Entity<LighterComponent> entity, ref AfterInteractEvent args)
    {
        if (args.Handled)
            return;

        if (args.Target is not { Valid: true } target || !args.CanReach)
            return;

        if (_solutionContainer.TryGetDrainableSolution(
            target,
            out var targetSoln,
            out var targetSolution)
            &&
            _solutionContainer.TryGetSolution(
            entity.Owner,
            entity.Comp.FuelSolutionName,
            out var solution,
            out var lighterSolution)
            &&
            targetSolution.ContainsPrototype(entity.Comp.FuelReagent)
            )
        {
            var trans = FixedPoint2.Min(lighterSolution.AvailableVolume, targetSolution.Volume);
            if (trans > 0)
            {
                var drained = _solutionContainer.Drain(target, targetSoln.Value, trans);
                _solutionContainer.TryAddSolution(solution.Value, drained);
                _audio.PlayPredicted(entity.Comp.LighterRefill, entity, user: args.User);
                _popup.PopupEntity(Loc.GetString("lighter-component-after-interact-refueled-message"), entity, args.User);
            }
            else if (lighterSolution.AvailableVolume <= 0)
            {
                _popup.PopupEntity(Loc.GetString("lighter-component-already-full", ("owner", entity.Owner)), entity, args.User);
            }
            else
            {
                _popup.PopupEntity(Loc.GetString("lighter-component-no-fuel-in-tank", ("target", args.Target)), entity, args.User);
            }
            args.Handled = true;
        }
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);
        UpdateLighters();
    }
}
