using Content.Shared.Light;
using Content.Shared.Light.Component;
using Robust.Client.GameObjects;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Client.Light;

/// <inheritdoc/>
public sealed class PulsingLightSystem : SharedPulsingLightSystem
{
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly PointLightSystem _pointLight = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<PulsingLightComponent, MapInitEvent>(OnMapInit);
    }

    private void OnMapInit(EntityUid uid, PulsingLightComponent component, MapInitEvent args)
    {
        if (component.RandomlyOffset)
            component.RandomOffset = _random.NextFloat(component.Period);
    }

    private void UpdateLight(EntityUid uid, float val, PulsingLightComponent component, PointLightComponent light)
    {
        val -= component.RandomOffset; // horizontal offset
        var b = 2 * MathF.PI / component.Period; // period

        if (component.MaxBrightness != null && component.MinBrightness != null)
        {
            var bA = (component.MaxBrightness - component.MinBrightness).Value / 2; // amplitude of brightness
            var bD = component.MinBrightness.Value + bA; // vertical shift for brightness

            var brightness = bA * MathF.Sin(b * val) + bD;
            light.Energy = brightness;
        }

        if (component.MaxRadius != null && component.MinRadius != null)
        {
            var rA = (component.MaxRadius - component.MinRadius).Value / 2; // amplitude of radius
            var rD = component.MinRadius.Value + rA; // vertical shift for radius

            var radius = rA * MathF.Sin(b * val) + rD;
            _pointLight.SetRadius(uid, radius, light);
        }
    }

    public override void FrameUpdate(float frameTime)
    {
        base.FrameUpdate(frameTime);

        var query = EntityQueryEnumerator<PulsingLightComponent, PointLightComponent>();
        while (query.MoveNext(out var uid, out var pulsing, out var light))
        {
            if (!pulsing.Enabled)
                continue;

            UpdateLight(uid, (float) _timing.CurTime.TotalSeconds, pulsing, light);
        }
    }
}
