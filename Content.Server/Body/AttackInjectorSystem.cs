using System.Linq;
using Content.Server.Anomaly.Components;
using Content.Server.Body.Systems;
using Content.Server.Chemistry.Components.SolutionManager;
using Content.Server.Chemistry.EntitySystems;
using Content.Shared.Anomaly.Components;
using Content.Shared.Weapons.Melee.Events;

namespace Content.Server.Body;
/// <summary>
/// This component allows the creature to inject reagent from the specified SolutionStogate
/// into the target during an attack
///
/// or vice versa, to pump out the solution from the bloodstream target system to yourself SolutionStogate
/// </summary>
public sealed class AttackInjectorSystem : EntitySystem
{

    [Dependency] private readonly SolutionContainerSystem _solutionContainer = default!;
    [Dependency] private readonly BloodstreamSystem _bloodstream = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<AttackInjectorComponent, MeleeHitEvent>(OnMeleeHit);

    }

    private void OnMeleeHit(EntityUid uid, AttackInjectorComponent component, MeleeHitEvent args)
    {
        Log.Debug("Атака");
        if (component.ReceiveInjectionValue > 0)
        {
            foreach (var ent in args.HitEntities)
            {
                if (!_solutionContainer.TryGetSolution(uid, component.StorageSolution, out var sol))
                    return;
                if (!_solutionContainer.TryGetSolution(ent, component.DrainSolution, out var targetSol))
                    return;
                _solutionContainer.TryTransferSolution(uid, sol, targetSol, component.ReceiveInjectionValue);
            }
        }

        if (component.GivingInjectionValue > 0)
        {
            foreach (var ent in args.HitEntities)
            {
                if (!_solutionContainer.TryGetSolution(uid, component.DrainSolution, out var sol))
                    return;
                if (!_solutionContainer.TryGetSolution(ent, component.StorageSolution, out var targetSol))
                    return;
                _solutionContainer.TryTransferSolution(ent, targetSol, sol, component.GivingInjectionValue);
            }
        }
    }
}
