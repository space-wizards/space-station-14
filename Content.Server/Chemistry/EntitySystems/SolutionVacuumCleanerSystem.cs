using Content.Server.Chemistry.Components;
using Content.Server.Popups;
using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.DoAfter;
using Content.Shared.FixedPoint;
using Content.Shared.Forensics;
using Content.Shared.Interaction;
using Content.Shared.Weapons.Ranged.Components;
using Content.Shared.Weapons.Ranged.Systems;

namespace Content.Server.Chemistry.EntitySystems;

public sealed class SolutionVacuumCleanerSystem : EntitySystem
{
    [Dependency] private readonly SharedSolutionContainerSystem _solutionContainerSystem = default!;
    [Dependency] private readonly SharedGunSystem _gunSystem = default!;
    [Dependency] private readonly SharedDoAfterSystem _doAfter = default!;
    [Dependency] private readonly PopupSystem _popup = default!;
    
    /// <inheritdoc />
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<SolutionVacuumCleanerComponent, AfterInteractEvent>(OnVacuumCleanerAfterInteract); // replace with DoAfter? look at InjectorSystem
        SubscribeLocalEvent<SolutionVacuumCleanerComponent, VacuumCleanerDoAfterEvent>(OnVacuumCleanerDoAfter);
    }

    private void OnVacuumCleanerDoAfter(Entity<SolutionVacuumCleanerComponent> entity, ref VacuumCleanerDoAfterEvent args)
    {
        if (args.Cancelled || args.Handled || args.Args.Target == null)
            return;

        args.Handled = TrySuckIn(entity, args.Args.Target.Value, args.Args.User);
    }

    private bool TrySuckIn(Entity<SolutionVacuumCleanerComponent> vacuumCleaner, EntityUid target, EntityUid argsUser)
    {
        if (!_solutionContainerSystem.TryGetDrawableSolution(target, out var drawableSolution, out _))
            return false;

        EntityUid solutionContainer;
        if (TryComp<ClothingSlotAmmoProviderComponent>(vacuumCleaner, out var clothSlotAmmoProvider)
            && _gunSystem.TryGetClothingSlotEntity(vacuumCleaner, clothSlotAmmoProvider, out var slotEntity))
        {
            solutionContainer = slotEntity.Value;
        }
        else
        {
            solutionContainer = vacuumCleaner.Owner;
        }


        if (!_solutionContainerSystem.TryGetSolution(solutionContainer, "tank", out var solutionComp, out var solution))
        {
            return false;
        }

        if (solution.AvailableVolume <= 0)
        {
            _popup.PopupClient(Loc.GetString("vacuum-cleaner-tank-full-popup"), argsUser);
            return false;
        }

        var realTransferAmount = FixedPoint2.Min(
            vacuumCleaner.Comp.FixedTransferAmount,
            drawableSolution.Value.Comp.Solution.Volume,
            solution.AvailableVolume
        );

        if (realTransferAmount <= 0)
        {
            return false;
        }

        // Move units from attackSolution to targetSolution
        var removedSolution = _solutionContainerSystem.Draw(solutionContainer, drawableSolution.Value, realTransferAmount);

        if (!_solutionContainerSystem.TryAddSolution(solutionComp.Value, removedSolution))
        {
            return false;
        }

        var ev = new TransferDnaEvent { Donor = target, Recipient = vacuumCleaner };
        RaiseLocalEvent(target, ref ev);

        return true;
    }

    private void OnVacuumCleanerAfterInteract(
        Entity<SolutionVacuumCleanerComponent> vacuumCleaner,
        ref AfterInteractEvent args
    )
    {
        if (args.Handled || !args.CanReach)
            return;

        if (args.Target is not { Valid: true } target)
            return;

        if (!_solutionContainerSystem.TryGetDrawableSolution(target, out _, out var drawableSolution) || drawableSolution.Volume == 0)
            return;

        if (vacuumCleaner.Comp.DoAfterId != null)
        {
            var status = _doAfter.GetStatus(vacuumCleaner.Comp.DoAfterId);
            if (status == DoAfterStatus.Running)
                return;
        }

        var actualDelay = MathHelper.Max(vacuumCleaner.Comp.Delay, TimeSpan.FromMilliseconds(500));

        var doAfterArgs = new DoAfterArgs(EntityManager, args.User, actualDelay, new VacuumCleanerDoAfterEvent(), vacuumCleaner.Owner, target: target, used: vacuumCleaner.Owner)
        {
            BreakOnMove = true,
            BreakOnWeightlessMove = false,
            BreakOnDamage = true,
            NeedHand = true,
            BreakOnHandChange = true,
            MovementThreshold = 0.1f,
            CancelDuplicate = true,
        };
        if (_doAfter.TryStartDoAfter(doAfterArgs, out var id))
        {
            vacuumCleaner.Comp.DoAfterId = id;
        }
    }
}
