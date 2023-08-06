using Content.Shared.Radiation.Components;

namespace Content.Shared.Radiation.Systems;

/// <summary>
/// This system handles radiation decay.
/// Provides intensity and slope setting API for other systems.
/// </summary>
public sealed class SharedRadiationSystem : EntitySystem
{
    public override void Update(float frameTime)
    {
        var query = EntityQueryEnumerator<RadiationDecayComponent, RadiationSourceComponent>();
        while (query.MoveNext(out var _, out var decay, out var source))
        {
            source.Intensity *= (1f - decay.Rate * frameTime);
        }
    }

    /// <summary>
    /// Set the Intensity field of a radiation source
    /// </summary>
    public void SetIntensity(EntityUid uid, float intensity, RadiationSourceComponent? comp = null)
    {
        if (!Resolve(uid, ref comp))
            return;

        comp.Intensity = intensity;
    }

    /// <summary>
    /// Set the Slope field of a radiation source
    /// </summary>
    public void SetSlope(EntityUid uid, float slope, RadiationSourceComponent? comp = null)
    {
        if (!Resolve(uid, ref comp))
            return;

        comp.Slope = slope;
    }
}
