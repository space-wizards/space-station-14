using Content.Server._Den.Components;
using Content.Server.Body.Components;
using Content.Server.Body.Systems;
using Content.Shared.Chemistry.Components.SolutionManager;
using Content.Shared.Mobs.Systems;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared._Den.Vampire.Components;

namespace Content.Server._Den.Vampire;

public sealed class VampireBloodEssenceSystem : EntitySystem
{
    [Dependency] private readonly BloodstreamSystem  _bloodstreamSystem = default!;
    [Dependency] private readonly SharedSolutionContainerSystem _solutionContainerSystem = default!;
    [Dependency] private readonly MobStateSystem _mobStateSystem = default!;

    public void DrinkBlood(Entity<VampireDrinkBloodComponent> ent, Entity<BloodstreamComponent> target)
    {
        Logger.Info($"[DrinkBlood] Attempting to drink blood from {ToPrettyString(target)} to {ToPrettyString(ent)}");

        if (!TryComp(target, out SolutionContainerManagerComponent? solutions))
        {
            Logger.Info($"[DrinkBlood] Failed: Target {ToPrettyString(target)} lacks SolutionContainerManagerComponent");
            return;
        }

        foreach (var bloodProtoId in ent.Comp.BloodTarget)
        {
            Logger.Info($"[DrinkBlood] Checking for blood solution '{bloodProtoId}'");

            if (!_solutionContainerSystem.ResolveSolution((target, solutions), target.Comp.BloodSolutionName, ref target.Comp.BloodSolution, out var bloodSolution))
            {
                Logger.Info($"[DrinkBlood] Could not resolve blood solution on {ToPrettyString(target)}");
                return;
            }


            if (bloodSolution.Volume == 0f || _mobStateSystem.IsDead(target)) // убедись что с Volume работает нормально, и поставь лучше <= гдет среднего значения, ну и не забудь все переделать в fixedpoint*ы
            {
                Logger.Info($"[DrinkBlood] Blood solution '{bloodProtoId}' is empty and target is alive. Skipping.");
                continue;
            }

            Logger.Info($"[DrinkBlood] Draining {ent.Comp.BloodDrainAmount}u blood from {ToPrettyString(target)}");

            _bloodstreamSystem.TryModifyBloodLevel(target, -ent.Comp.BloodDrainAmount, target.Comp);

            // добавь логирование текущей крови target*а , а еще я - забыл видимо

            if (TryComp(ent, out VampireThirstBloodComponent? thirstBlood))
            {
                Logger.Info($"[DrinkBlood] Updated thirst blood to {thirstBlood.CurrentThirstBlood} for {ToPrettyString(ent)}");
                thirstBlood.CurrentThirstBlood += ent.Comp.BloodDrainAmount;
            }

            Logger.Info($"[DrinkBlood] Finished processing blood drain for {ToPrettyString(ent)}");
        }
    }
}
