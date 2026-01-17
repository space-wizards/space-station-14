using Content.Shared.Arcade.Components;
using Content.Shared.Arcade.Events;

namespace Content.Shared.Arcade.Systems;

/// <summary>
///
/// </summary>
public sealed partial class SharedArcadeRewardsSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ArcadeRewardsComponent, ArcadeChangedStateEvent>(OnArcadeChangedState);
    }

    #region Events

    private void OnArcadeChangedState(Entity<ArcadeRewardsComponent> ent, ref ArcadeChangedStateEvent args)
    {
        if (args.NewState != ArcadeGameState.Win)
            return;


    }

    #endregion

    #region API

    #endregion
}
