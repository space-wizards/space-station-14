using Content.Shared.Arcade.Components;
using Robust.Shared.Timing;

namespace Content.Shared.Arcade.Systems;

/// <summary>
///
/// </summary>
public sealed partial class SharedBlockGameArcadeSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _gameTiming = default!;
    [Dependency] private readonly SharedArcadeSystem _arcade = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<BlockGameArcadeComponent, MapInitEvent>(OnMapInit);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var curTime = _gameTiming.CurTime;
        var query = EntityQueryEnumerator<BlockGameArcadeComponent>();
        while (query.MoveNext(out var uid, out var comp))
        {
            if (_arcade.GetGameState(uid) != ArcadeGameState.Game)
                continue;

            if (comp.NextPhysicsUpdate > curTime)
                continue;

            comp.NextPhysicsUpdate += comp.PhysicsCooldown;
            DirtyField(uid, comp, nameof(BlockGameArcadeComponent.NextPhysicsUpdate));

            var width = comp.Size.X;
            for (var i = comp.Cells.Length - 1; i >= 0; i--)
            {
                var cell = comp.Cells[i];
                if (cell == BlockGameArcadeCell.Empty || cell == BlockGameArcadeCell.Player)
                    continue;

                var x = i / width;
                var newY = i % width - 1;
                if (newY > 0 && x == comp.PlayerPosition.X && newY == comp.PlayerPosition.Y)
                    _arcade.TryChangeGameState(uid, EntityUid.Invalid, ArcadeGameState.Lose);

                comp.Cells[i] = BlockGameArcadeCell.Empty;
            }

            if (comp.NextSpawn <= curTime)
            {
                // TODO: Spawn logic

                comp.NextSpawn += comp.SpawnCooldown;
                DirtyField(uid, comp, nameof(BlockGameArcadeComponent.NextSpawn));
            }

            DirtyField(uid, comp, nameof(BlockGameArcadeComponent.Cells));
        }
    }

    #region Events

    private void OnMapInit(Entity<BlockGameArcadeComponent> ent, ref MapInitEvent args)
    {
        ent.Comp.Cells = new BlockGameArcadeCell[ent.Comp.Size.X * ent.Comp.Size.Y];
        Array.Fill(ent.Comp.Cells, BlockGameArcadeCell.Empty);
    }

    #endregion

    #region BUI

    #endregion

    #region API

    /// <summary>
    ///
    /// </summary>
    public BlockGameArcadeCell GetCell(Entity<BlockGameArcadeComponent?> ent, uint x, uint y)
    {
        if (!Resolve(ent, ref ent.Comp))
            return BlockGameArcadeCell.Invalid;

        if (x < 0 || ent.Comp.Size.X < x)
            return BlockGameArcadeCell.Invalid;

        if (y < 0 || ent.Comp.Size.Y < y)
            return BlockGameArcadeCell.Invalid;

        return ent.Comp.Cells[ent.Comp.Size.X * x + y];
    }

    #endregion
}
