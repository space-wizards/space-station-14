using Content.Shared.CosmicCult.Abilities.Colossus;
using Content.Shared.CosmicCult.Components;
using Content.Shared.Damage.Systems;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Systems;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;

namespace Content.Server.CosmicCult.Abilities.Colossus;

public sealed partial class ServerCosmicColossusAbilitySystem : CosmicColossusAbilitySystem
{
    [Dependency] private DamageableSystem _damage = default!;
    [Dependency] private MobStateSystem _state = default!;
    [Dependency] private MobThresholdSystem _threshold = default!;
    [Dependency] private SharedAudioSystem _audio = default!;
    [Dependency] private SharedMapSystem _map = default!;

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<CosmicTileDetonatorComponent>();
        while (query.MoveNext(out var ent, out var comp))
        {
            if (comp.Size > comp.MaxSize || Timing.CurTime < comp.DetonationTimer)
                continue;

            TransformSys.TryGetGridTilePosition(ent, out var entPos);

            var xform = Transform(ent);
            var tile = new Vector2i(entPos.X,entPos.Y);

            if (xform.GridUid is not { } gridUid)
                continue;

            if (!TryComp<MapGridComponent>(gridUid, out var grid))
                continue;

            for (var x = - comp.Size; x <= comp.Size; x++)
            {
                if (Math.Abs(x) == comp.Size)
                {
                    for (var y = - comp.Size; y <= comp.Size; y++)
                    {
                        Spawn(comp.TileDetonation, _map.GridTileToWorld(gridUid, grid, tile + new Vector2i(x, y)));
                    }
                }
                else
                {
                    Spawn(comp.TileDetonation, new EntityCoordinates(gridUid, tile + new Vector2i(x,-comp.Size)));
                    Spawn(comp.TileDetonation, new EntityCoordinates(gridUid, tile + new Vector2i(x,comp.Size)));
                }
            }

            comp.DetonationTimer = comp.DetonateWait + Timing.CurTime;
            comp.Size++;
        }

        var queryColossus = EntityQueryEnumerator<CosmicColossusComponent>();
        while (queryColossus.MoveNext(out var ent, out var comp))
        {
            if (comp.SunderResetTimer is { } sunderTimer && Timing.CurTime >= sunderTimer)
            {
                comp.SunderResetTimer = null;
                if (_state.IsDead(ent))
                    return;

                Appearance.SetData(ent, ColossusVisuals.Visuals, ColossusStatus.Alive);
            }

            if (comp.HibernationTimer is {} hibernationTimer && Timing.CurTime >= hibernationTimer)
            {
                comp.HibernationTimer = null;
                if (!_threshold.TryGetThresholdForState(ent, MobState.Dead, out var health) || _state.IsDead(ent))
                    return;

                Appearance.SetData(ent, ColossusVisuals.Visuals, ColossusStatus.Alive);
                _damage.HealEvenly(ent, health.Value / 2f * -1);
                _audio.PlayPvs(comp.ReawakenSfx, ent);

                Spawn(comp.CultBigVfx, Transform(ent).Coordinates);
            }
        }
    }
}
