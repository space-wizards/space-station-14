using Content.Shared.Chemistry.EntitySystems;
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
        SubscribeLocalEvent<WelderHeaterComponent, HeaterAttemptEvent>(OnHeaterAttempt);
        SubscribeLocalEvent<WelderHeaterComponent, HeaterConsumedEvent>(OnHeaterConsumed);
    }

    /// <summary>
    ///     Checks if the welder is lit and has sufficient fuel before heating can occur.
    /// </summary>
    private void OnHeaterAttempt(Entity<WelderHeaterComponent> ent, ref HeaterAttemptEvent args)
    {
        if (args.Cancelled)
            return;

        if (!TryComp<WelderComponent>(ent, out var welder) || !_itemToggle.IsActivated(ent.Owner))
        {
            args.Cancelled = true;
            return;
        }

        if (!_solutionContainer.TryGetSolution(ent.Owner, welder.FuelSolutionName, out _, out var fuelSolution))
        {
            args.Cancelled = true;
            return;
        }

        if (fuelSolution.GetTotalPrototypeQuantity(welder.FuelReagent) < ent.Comp.FuelConsumptionPerHeat)
        {
            args.Cancelled = true;
            _popup.PopupEntity(Loc.GetString("welder-component-no-fuel-message"), ent, args.User);
        }
    }

    /// <summary>
    ///     Consumes fuel from the welder after a heating cycle completes.
    /// </summary>
    private void OnHeaterConsumed(Entity<WelderHeaterComponent> ent, ref HeaterConsumedEvent args)
    {
        if (!TryComp<WelderComponent>(ent, out var welder))
            return;

        if (!_solutionContainer.TryGetSolution(ent.Owner, welder.FuelSolutionName, out var fuelSolnComp, out var fuelSolution))
            return;

        var fuelNeeded = ent.Comp.FuelConsumptionPerHeat;
        if (fuelSolution.GetTotalPrototypeQuantity(welder.FuelReagent) < fuelNeeded)
        {
            _popup.PopupEntity(Loc.GetString("welder-component-no-fuel-message"), ent, args.User);
            return;
        }

        _solutionContainer.RemoveReagent(fuelSolnComp.Value, welder.FuelReagent, fuelNeeded);
    }
}
