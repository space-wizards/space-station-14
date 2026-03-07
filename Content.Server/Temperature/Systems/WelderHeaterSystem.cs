using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.FixedPoint;
using Content.Shared.Item.ItemToggle;
using Content.Shared.Popups;
using Content.Shared.Temperature;
using Content.Shared.Tools.Components;

namespace Content.Server.Temperature.Systems;

/// <summary>
///     Handles welder-specific logic for <see cref="HeaterToolComponent"/>.
/// </summary>
public sealed class WelderHeaterSystem : EntitySystem
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

    private void OnHeaterAttempt(Entity<WelderComponent> ent, ref HeaterAttemptEvent args)
    {
        if (args.Cancelled)
            return;

        if (!_itemToggle.IsActivated((ent.Owner, null)))
        {
            args.Cancelled = true;
            return;
        }

        if (!_solutionContainer.TryGetSolution(ent.Owner, ent.Comp.FuelSolutionName, out _, out var fuelSolution))
        {
            args.Cancelled = true;
            return;
        }

        if (fuelSolution.GetTotalPrototypeQuantity(ent.Comp.FuelReagent) <= FixedPoint2.Zero)
        {
            args.Cancelled = true;
            _popup.PopupEntity(Loc.GetString("welder-component-no-fuel-message"), ent.Owner, args.User);
        }
    }

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

/// <summary>
///    Optional component to specify welder-specific heating values.
/// </summary>
[RegisterComponent]
public sealed partial class WelderHeaterComponent : Component
{
    [DataField]
    public float FuelConsumptionPerHeat = 1.0f;
}
