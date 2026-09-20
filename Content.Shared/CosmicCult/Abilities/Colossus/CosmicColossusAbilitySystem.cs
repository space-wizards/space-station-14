using Content.Shared.Coordinates.Helpers;
using Content.Shared.CosmicCult.Components;
using Content.Shared.CosmicCult.Components.Actions;
using Content.Shared.Damage.Systems;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Systems;
using Content.Shared.Stunnable;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Network;
using Robust.Shared.Timing;

namespace Content.Shared.CosmicCult.Abilities.Colossus;

public abstract partial class CosmicColossusAbilitySystem : EntitySystem
{
    [Dependency] protected SharedAppearanceSystem Appearance = default!;

    [Dependency] private CosmicCultSystem _cult = default!;
    [Dependency] private DamageableSystem _damage = default!;
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private INetManager _net = default!;
    [Dependency] private MobThresholdSystem _threshold = default!;
    [Dependency] private SharedAudioSystem _audio = default!;
    [Dependency] private SharedMapSystem _map = default!;
    [Dependency] private SharedStunSystem _stun = default!;
    [Dependency] private SharedTransformSystem _transform = default!;

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<CosmicTileDetonatorComponent>();
        while (query.MoveNext(out var ent, out var comp))
        {
            if (comp.Size > comp.MaxSize || _timing.CurTime < comp.DetonationTimer)
                continue;

            _transform.TryGetGridTilePosition(ent, out var entPos);

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

            comp.DetonationTimer = comp.DetonateWait + _timing.CurTime;
            comp.Size++;
        }

        var queryColossus = EntityQueryEnumerator<CosmicColossusComponent>();
        while (queryColossus.MoveNext(out var ent, out var comp))
        {
            if (_net.IsServer && comp.SunderResetTimer is { } sunderTimer && _timing.CurTime >= sunderTimer)
            {
                comp.SunderResetTimer = null;
                Appearance.SetData(ent, ColossusVisuals.Visuals, ColossusStatus.Alive);
            }

            if (comp.HibernationTimer is {} timer && _timing.CurTime >= timer)
            {
                comp.HibernationTimer = null;

                if (!_threshold.TryGetThresholdForState(ent, MobState.Dead, out var health))
                    return;

                _damage.HealEvenly(ent, health.Value / 2f * -1);
                _audio.PlayPredicted(comp.ReawakenSfx, ent, ent);
                Appearance.SetData(ent, ColossusVisuals.Visuals, ColossusStatus.Alive);

                if (_net.IsServer)
                    Spawn(comp.CultBigVfx, Transform(ent).Coordinates);
            }
        }
    }

    [SubscribeLocalEvent]
    private void OnColossusHibernate(Entity<CosmicActionHibernateComponent> ent, ref EventCosmicColossusHibernate args)
    {
        if (!_cult.CultActionQuery.HasComp(ent) || _timing.ApplyingState)
            return;

        if (_transform.GetGrid(args.Performer) is null)
            return;

        args.Handled = true;

        if (!TryComp<CosmicColossusComponent>(args.Performer, out var colossusComp))
            return;

        var appearance = Comp<AppearanceComponent>(args.Performer);
        Appearance.SetData(args.Performer, ColossusVisuals.Visuals, ColossusStatus.Hibernate, appearance);
        colossusComp.AnimReady = true;

        colossusComp.HibernationTimer = _timing.CurTime + ent.Comp.SlumberTime;
        _stun.TryUpdateStunDuration(args.Performer, ent.Comp.SlumberTime + TimeSpan.FromSeconds(0.75));
    }

    [SubscribeLocalEvent]
    private void OnColossusSunder(Entity<CosmicActionSunderComponent> ent, ref EventCosmicColossusSunder args)
    {
        if (!_cult.CultActionQuery.HasComp(ent) || _timing.ApplyingState)
            return;

        if (_transform.GetGrid(args.Target) is null)
            return;

        args.Handled = true;
        var pos = args.Target.SnapToGrid();
        var areaEffect = PredictedSpawnAtPosition(ent.Comp.AreaEffect, pos);

        EnsureComp<CosmicTileDetonatorComponent>(areaEffect, out var areaComp);
        areaComp.DetonationTimer = _timing.CurTime;

        if (TryComp<CosmicColossusComponent>(args.Performer, out var colossus))
        {
            var resetDelay = TimeSpan.FromSeconds(Math.Max(ent.Comp.SunderPauseTime.TotalSeconds, 1.4));
            colossus.SunderResetTimer = _timing.CurTime + resetDelay;
        }


        Appearance.SetData(args.Performer, ColossusVisuals.Visuals, ColossusStatus.Sunder);
        _stun.TryUpdateStunDuration(args.Performer, ent.Comp.SunderPauseTime);
        _transform.SetCoordinates(args.Performer, pos);
    }
}
