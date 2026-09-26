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
    [Dependency] protected IGameTiming Timing = default!;
    [Dependency] protected SharedAppearanceSystem Appearance = default!;
    [Dependency] protected SharedTransformSystem TransformSys = default!;

    [Dependency] private CosmicCultSystem _cult = default!;
    [Dependency] private SharedStunSystem _stun = default!;

    [SubscribeLocalEvent]
    private void OnColossusHibernate(Entity<CosmicActionHibernateComponent> ent, ref EventCosmicColossusHibernate args)
    {
        if (!_cult.CultActionQuery.HasComp(ent))
            return;

        if (TransformSys.GetGrid(args.Performer) is null)
            return;

        args.Handled = true;

        if (!TryComp<CosmicColossusComponent>(args.Performer, out var colossusComp))
            return;

        var appearance = Comp<AppearanceComponent>(args.Performer);
        Appearance.SetData(args.Performer, ColossusVisuals.Visuals, ColossusStatus.Hibernate, appearance);
        colossusComp.AnimReady = true;

        colossusComp.HibernationTimer = Timing.CurTime + ent.Comp.SlumberTime;
        _stun.TryUpdateStunDuration(args.Performer, ent.Comp.SlumberTime + TimeSpan.FromSeconds(0.75));
    }

    [SubscribeLocalEvent]
    private void OnColossusSunder(Entity<CosmicActionSunderComponent> ent, ref EventCosmicColossusSunder args)
    {
        if (!_cult.CultActionQuery.HasComp(ent))
            return;

        if (TransformSys.GetGrid(args.Target) is null)
            return;

        args.Handled = true;
        var pos = args.Target.SnapToGrid();
        var areaEffect = PredictedSpawnAtPosition(ent.Comp.AreaEffect, pos);

        EnsureComp<CosmicTileDetonatorComponent>(areaEffect, out var areaComp);
        areaComp.DetonationTimer = Timing.CurTime;

        if (TryComp<CosmicColossusComponent>(args.Performer, out var colossus))
            colossus.SunderResetTimer = Timing.CurTime + ent.Comp.SunderPauseTime;

        Appearance.SetData(args.Performer, ColossusVisuals.Visuals, ColossusStatus.Sunder);
        _stun.TryUpdateStunDuration(args.Performer, ent.Comp.SunderPauseTime);
        TransformSys.SetCoordinates(args.Performer, pos);
    }
}
