using Content.Shared.CosmicCult;
using Content.Shared.CosmicCult.Components;
using Robust.Shared.Timing;

namespace Content.Server.CosmicCult.Abilities.Colossus;

public sealed partial class CosmicSunderSystem : EntitySystem
{
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private SharedAppearanceSystem _appearance = default!;
    [Dependency] private SharedTransformSystem _transform = default!;

    [SubscribeLocalEvent]
    private void OnColossusSunder(Entity<CosmicColossusComponent> ent, ref EventCosmicColossusSunder args)
    {
        args.Handled = true;

        var comp = ent.Comp;
        _appearance.SetData(ent, ColossusVisuals.Status, ColossusStatus.Action);
        _transform.SetCoordinates(ent, args.Target);
        _transform.AnchorEntity(ent);

        comp.Attacking = true;
        comp.AttackHoldTimer = comp.AttackWait + _timing.CurTime;
        Spawn(comp.Attack1Vfx, args.Target);

        var detonator = Spawn(comp.TileDetonations, args.Target);
        EnsureComp<CosmicTileDetonatorComponent>(detonator, out var detonateComp);
        detonateComp.DetonationTimer = _timing.CurTime;
    }
}
