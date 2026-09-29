using Content.Server.Explosion.EntitySystems;
using Content.Server.Xenoarchaeology.Artifact.XAE.Components;
using Content.Shared.Explosion.Components;
using Content.Shared.Xenoarchaeology.Artifact;
using Content.Shared.Xenoarchaeology.Artifact.XAE;

namespace Content.Server.Xenoarchaeology.Artifact.XAE;

/// <summary>
/// System for xeno artifact effect of triggering explosion.
/// </summary>
public sealed partial class XAETriggerExplosivesSystem : BaseXAESystem<XAETriggerExplosivesComponent>
{
    [Dependency] private ExplosionSystem _explosion = default!;

    /// <inheritdoc />
    protected override void OnActivated(Entity<XAETriggerExplosivesComponent> ent, ref XenoArtifactNodeActivatedEvent args)
    {
        if (!TryComp<ExplosiveComponent>(ent, out var explosiveComp))
            return;

        var totalIntensity = explosiveComp.TotalIntensity;
        if (args.Modifications.TryGetValue(XenoArtifactEffectModifier.Power,
                out var totalIntensityModifier))
        {
            totalIntensity += Math.Max(totalIntensity / 4, totalIntensityModifier.Modify(totalIntensity));
        }

        var maxIntensity = explosiveComp.MaxIntensity;
        if (args.Modifications.TryGetValue(XenoArtifactEffectModifier.Power, out var maxIntensityModifier))
        {
            maxIntensity = Math.Max(maxIntensity / 4, maxIntensityModifier.Modify(maxIntensity));
        }

        _explosion.TriggerExplosive(ent, explosiveComp, totalIntensity: totalIntensity, maxIntensity: maxIntensity);
    }
}
