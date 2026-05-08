using Content.Server.Arcade.Components;

namespace Content.Server.Arcade.Systems;

public sealed partial class ArcadeSystem
{
    private void OnArcadeRewardComponentInit(Entity<ArcadeRewardComponent> machine, ref ComponentInit args)
    {
        var random = new Random();
        var comp = machine.Comp;

        comp.RewardAmount = random.Next(comp.RewardMinAmount, comp.RewardMaxAmount + 1);
    }

    private void OnArcadeRewardGameEnded(Entity<ArcadeRewardComponent> machine, ref ArcadeGameEndedEvent args)
    {
        if (args.Result == ArcadeGameResult.Win)
            SpawnReward(machine);
    }

    /// <summary>
    ///     Produce a reward from an arcade machine, if the machine is capable of doing so.
    /// </summary>
    /// <param name="machine">The machine to spawn rewards from.</param>
    public void SpawnReward(Entity<ArcadeRewardComponent> machine)
    {
        if (machine.Comp.RewardAmount <= 0)
            return;

        var coords = Transform(machine).Coordinates;
        var spawns = _entityTable.GetSpawns(machine.Comp.PossibleRewards);

        foreach (var proto in spawns)
            Spawn(proto, coords);

        machine.Comp.RewardAmount--;
    }
}
