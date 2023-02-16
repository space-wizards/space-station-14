using Content.Server.Cargo.Components;
using Content.Server.Salvage.Expeditions;
using Content.Server.Salvage.Expeditions.Structure;
using Content.Shared.Procedural.Rewards;
using Content.Shared.Random;
using Content.Shared.Salvage;
using Robust.Shared.Utility;

namespace Content.Server.Salvage;

public sealed partial class SalvageSystem
{
    private void InitializeRunner()
    {
    }

    // Runs the expedition
    private void UpdateRunner()
    {
        // Structure missions
        foreach (var (structure, comp) in EntityQuery<SalvageStructureExpeditionComponent, SalvageExpeditionComponent>())
        {
            if (comp.Completed)
                continue;

            for (var i = 0; i < structure.Structures.Count; i++)
            {
                var objective = structure.Structures[i];

                if (Deleted(objective))
                {
                    structure.Structures.RemoveSwap(i);
                }
            }

            if (structure.Structures.Count == 0)
            {
                var mission = comp.Mission;
                _sawmill.Debug($"Paying out salvage mission completion for {mission.Config} seed {mission.Seed}");
                comp.Completed = true;
                PayoutReward(comp, mission, GetDifficultyModifier(_prototypeManager.Index<SalvageExpeditionPrototype>(mission.Config).DifficultyRating));
            }
        }
    }

    private void PayoutReward(SalvageExpeditionComponent component, SalvageMission mission, float modifier)
    {
        var station = component.Station;

        var reward =
            GetReward(
                _prototypeManager.Index<WeightedRandomPrototype>(_prototypeManager
                    .Index<SalvageExpeditionPrototype>(mission.Config).Reward), mission.Seed, _prototypeManager);

        switch (reward)
        {
            case BankReward bank:
                if (TryComp<StationBankAccountComponent>(station, out var sBank))
                {
                    _cargo.UpdateBankAccount(sBank, (int) (bank.Amount * modifier));
                }
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }
    }
}
