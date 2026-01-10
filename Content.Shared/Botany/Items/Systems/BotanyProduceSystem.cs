using Content.Shared.Botany.Components;
using Content.Shared.Botany.Events;
using Content.Shared.Botany.Items.Components;
using Content.Shared.Botany.Systems;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.FixedPoint;
using Content.Shared.IdentityManagement;
using Content.Shared.Interaction;
using Content.Shared.Popups;
using Robust.Shared.Player;

namespace Content.Shared.Botany.Items.Systems;

/// <summary>
/// System for taking a sample of a plant.
/// </summary>
public sealed class BotanyProduceSystem : EntitySystem
{
    [Dependency] private readonly BotanySystem _botany = default!;
    [Dependency] private readonly PlantSystem _plant = default!;
    [Dependency] private readonly PlantTraySystem _plantTray = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly SharedSolutionContainerSystem _solutionContainer = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ProduceComponent, AfterInteractEvent>(OnAfterInteract);
        SubscribeLocalEvent<PlantTrayComponent, CompostingProduceAttemptEvent>(OnCompostingProduceAttempt);
    }

    private void OnAfterInteract(Entity<ProduceComponent> ent, ref AfterInteractEvent args)
    {
        if (args.Target == null || args.Handled || !args.CanReach || !HasComp<PlantTrayComponent>(args.Target))
            return;

        var ev = new CompostingProduceAttemptEvent(ent, args.User);
        RaiseLocalEvent(args.Target.Value, ref ev);

        args.Handled = true;
    }

    private void OnCompostingProduceAttempt(Entity<PlantTrayComponent> ent, ref CompostingProduceAttemptEvent args)
    {
        if (args.Cancelled)
            return;

        _popup.PopupPredictedCursor(Loc.GetString("plant-holder-component-compost-message",
                ("owner", ent.Owner),
                ("usingItem", args.Produce.Owner)),
            args.User,
            PopupType.Medium);
        _popup.PopupEntity(Loc.GetString("plant-holder-component-compost-others-message",
                ("user", Identity.Entity(args.User, EntityManager)),
                ("usingItem", args.Produce.Owner),
                ("owner", ent.Owner)),
            ent.Owner,
            Filter.PvsExcept(args.User),
            true);

        if (_solutionContainer.TryGetSolution(args.Produce.Owner, args.Produce.Comp.SolutionName, out var soln2, out var solution2))
        {
            if (_solutionContainer.ResolveSolution(ent.Owner, ent.Comp.SoilSolutionName, ref ent.Comp.SoilSolution, out var solution1))
            {
                // We try to fit as much of the composted plant's contained solution into the hydroponics tray as we can,
                // since the plant will be consumed anyway.
                var fillAmount = FixedPoint2.Min(solution2.Volume, solution1.AvailableVolume);
                _solutionContainer.TryAddSolution(ent.Comp.SoilSolution.Value, _solutionContainer.SplitSolution(soln2.Value, fillAmount));

                if (_plantTray.TryGetPlant(ent.AsNullable(), out var plantUid))
                    _plant.ForceUpdateByExternalCause(plantUid.Value);
            }
        }

        if (_botany.TryGetPlantComponent<PlantComponent>(args.Produce.Comp.PlantData, args.Produce.Comp.PlantProtoId, out var compostPlant))
        {
            var nutrientBonus = compostPlant.Potency / args.Produce.Comp.NutrientDivider;
            _plantTray.AdjustNutrient(ent.AsNullable(), nutrientBonus);
        }

        PredictedQueueDel(args.Produce.Owner);
    }
}
