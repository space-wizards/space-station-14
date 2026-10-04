using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.FixedPoint;
using Content.Shared.Item.ItemToggle;
using Content.Shared.Popups;
using Content.Shared.Temperature;
using Content.Shared.Temperature.Components;
using Content.Shared.Tools.Components;

namespace Content.Server.Temperature.Systems;

/// <summary>
///     Handles welder-specific heating logic and fuel consumption for <see cref="HeaterToolComponent"/>.
/// </summary>
public sealed partial class WelderHeaterSystem : EntitySystem
{
    [Dependency] private readonly SharedSolutionContainerSystem _solutionContainer = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly ItemToggleSystem _itemToggle = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<WelderComponent, HeaterAttemptEvent>(OnHeaterAttempt);
        SubscribeLocalEvent<WelderComponent, HeaterConsumedEvent>(OnHeaterConsumed);
    }

    /// <summary>
    ///     Checks if the welder is lit and has sufficient fuel before heating can occur.
    /// </summary>
    private void OnHeaterAttempt(Entity<WelderComponent> ent, ref HeaterAttemptEvent args)
    {
        if (args.Cancelled)
            return;

        if (!_itemToggle.IsActivated(ent.Owner))
        {
            args.Cancelled = true;
            return;
        }

        if (!_solutionContainer.TryGetSolution(ent.Owner, ent.Comp.FuelSolutionName, out _, out var fuelSolution))
        {
            args.Cancelled = true;
            return;
        }

        var fuelNeeded = FixedPoint2.New(1.0f);
        if (TryComp<WelderHeaterComponent>(ent, out var welderHeater))
        {
            fuelNeeded = FixedPoint2.New(welderHeater.FuelConsumptionPerHeat);
        }

        if (fuelSolution.GetTotalPrototypeQuantity(ent.Comp.FuelReagent) < fuelNeeded)
        {
            args.Cancelled = true;
            _popup.PopupEntity(Loc.GetString("welder-component-no-fuel-message"), ent.Owner, args.User);
        }
    }

    /// <summary>
    ///     Consumes fuel from the welder after a heating cycle completes.
    /// </summary>
    private void OnHeaterConsumed(Entity<WelderComponent> ent, ref HeaterConsumedEvent args)
    {
        if (!_solutionContainer.TryGetSolution(ent.Owner, ent.Comp.FuelSolutionName, out var fuelSolnComp, out var fuelSolution))
            return;

        var fuelConsumption = 1.0f;
        if (TryComp<WelderHeaterComponent>(ent, out var welderHeater))
        {
            fuelConsumption = welderHeater.FuelConsumptionPerHeat;
        }

        var fuelNeeded = FixedPoint2.New(fuelConsumption);
        if (fuelSolution.GetTotalPrototypeQuantity(ent.Comp.FuelReagent) < fuelNeeded)
        {
            _popup.PopupEntity(Loc.GetString("welder-component-no-fuel-message"), ent.Owner, args.User);
            return;
        }

        _solutionContainer.RemoveReagent(fuelSolnComp.Value, ent.Comp.FuelReagent, fuelNeeded);
    }
}
