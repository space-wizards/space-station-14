using Content.Shared.CosmicCult;
using Content.Shared.CosmicCult.Abilities;
using Content.Shared.CosmicCult.Components;
using Content.Shared.CosmicCult.Components.Actions;
using Robust.Shared.Spawners;

namespace Content.Client.CosmicCult.Abilities;

public sealed partial class ClientCosmicImpositionSystem : CosmicImpositionSystem
{
    protected override void OnCosmicImposition(Entity<CosmicActionImpositionComponent> ent, ref EventCosmicImposition args)
    {
        base.OnCosmicImposition(ent, ref args);

        if (!Cult.CultActionQuery.TryComp(ent, out var action) || !args.Handled || !Timing.IsFirstTimePredicted)
            return;

        var duration = action.Empowered ? ent.Comp.DurationEmpowered : ent.Comp.DurationDefault;
        var overlayEffect = SpawnAttachedTo(ent.Comp.ImpositionOverlay, Transform(ent).Coordinates);

        SpawnAttachedTo(action.Vfx, Transform(ent).Coordinates);
        EnsureComp<CosmicVisualFadeComponent>(overlayEffect, out var fade);
        EnsureComp<TimedDespawnComponent>(overlayEffect, out var despawn);

        despawn.Lifetime = (float) duration.TotalSeconds;
        fade.Duration = (float) duration.TotalSeconds;
    }
}
