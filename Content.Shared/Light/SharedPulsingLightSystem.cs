using Content.Shared.Light.Component;
using JetBrains.Annotations;

namespace Content.Shared.Light;

/// <summary>
/// This handles logic relating to <see cref="PulsingLightComponent"/>
/// </summary>
public abstract class SharedPulsingLightSystem : EntitySystem
{
    [PublicAPI]
    public void SetEnabled(EntityUid uid, bool enabled, PulsingLightComponent? component = null, bool dirty = true)
    {
        if (!Resolve(uid, ref component))
            return;
        component.Enabled = enabled;
        if (dirty)
            Dirty(component);
    }

    [PublicAPI]
    public void SetBrightness(EntityUid uid, float min, float max, PulsingLightComponent? component = null, bool dirty = true)
    {
        if (!Resolve(uid, ref component))
            return;
        component.MinBrightness = min;
        component.MaxBrightness = max;
        if (dirty)
            Dirty(component);
    }

    [PublicAPI]
    public void SetRadius(EntityUid uid, float min, float max, PulsingLightComponent? component = null, bool dirty = true)
    {
        if (!Resolve(uid, ref component))
            return;
        component.MinRadius = min;
        component.MaxRadius = max;
        if (dirty)
            Dirty(component);
    }

    [PublicAPI]
    public void SetPeriod(EntityUid uid, float period, PulsingLightComponent? component = null, bool dirty = true)
    {
        if (!Resolve(uid, ref component))
            return;
        component.Period = period;
        if (dirty)
            Dirty(component);
    }
}
