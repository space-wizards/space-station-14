using System.Linq;
using Content.Shared.Arcade.Components;
using Content.Shared.Arcade.Events;
using Content.Shared.EntityTable;
using Content.Shared.Random.Helpers;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Shared.Arcade.Systems;

/// <summary>
///
/// </summary>
public sealed partial class SharedArcadeRewardsSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly EntityTableSystem _entityTable = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ArcadeRewardsComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<ArcadeRewardsComponent, ArcadeChangedStateEvent>(OnArcadeChangedState);
    }

    #region Events

    private void OnMapInit(Entity<ArcadeRewardsComponent> ent, ref MapInitEvent args)
    {
        // TODO: Use RandomPredicted https://github.com/space-wizards/RobustToolbox/pull/5849
        var seed = SharedRandomExtensions.HashCodeCombine((int)_timing.CurTick.Value, GetNetEntity(ent).Id);
        var rand = new System.Random(seed);
        ent.Comp.Amount = rand.NextByte(ent.Comp.MinAmount, ent.Comp.MaxAmount);
    }

    private void OnArcadeChangedState(Entity<ArcadeRewardsComponent> ent, ref ArcadeChangedStateEvent args)
    {
        if (args.NewState != ArcadeGameState.Win)
            return;

        TrySpawnReward(ent.AsNullable());
    }

    #endregion

    #region API

    /// <summary>
    ///
    /// </summary>
    public bool TrySpawnReward(Entity<ArcadeRewardsComponent?> ent)
    {
        if (!Resolve(ent, ref ent.Comp))
            return false;

        if (ent.Comp.Amount == 0)
            return false;

        // TODO: Use RandomPredicted https://github.com/space-wizards/RobustToolbox/pull/5849
        var seed = SharedRandomExtensions.HashCodeCombine((int)_timing.CurTick.Value, GetNetEntity(ent).Id);
        var rand = new System.Random(seed);
        PredictedSpawnAtPosition(_entityTable.GetSpawns(ent.Comp.Rewards, rand).First().Id, Transform(ent).Coordinates);

        ent.Comp.Amount--;
        DirtyField(ent, nameof(ArcadeRewardsComponent.Amount));

        return true;
    }

    #endregion
}
