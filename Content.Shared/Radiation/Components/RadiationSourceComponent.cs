using Robust.Shared.Physics;

namespace Content.Shared.Radiation.Components;

/// <summary>
///     Irradiate all objects in range.
/// </summary>
[RegisterComponent]
public sealed partial class RadiationSourceComponent : Component
{
    /// <summary>
    /// A delegate that is called when properties change.
    /// RadiationSystem subscribes to it when initialized.
    /// </summary>
    public Action? OnModified;

    /// <summary>
    ///     Radiation intensity in center of the source in rads per second.
    ///     From there radiation rays will travel over distance and loose intensity
    ///     when hit radiation blocker.
    /// </summary>
    [DataField("intensity")]
    [ViewVariables]
    private float _intensity = 1f;

    [ViewVariables(VVAccess.ReadWrite)]
    public float Intensity
    {
        get => _intensity;
        set
        {
            if (MathHelper.CloseTo(_intensity, value)) return;
            _intensity = value;
            OnModified?.Invoke();
        }
    }

    /// <summary>
    ///     Defines how fast radiation rays will loose intensity
    ///     over distance. The bigger the value, the shorter range
    ///     of radiation source will be.
    /// </summary>
    [DataField("slope")]
    [ViewVariables]
    private float _slope = 0.5f;

    [ViewVariables(VVAccess.ReadWrite)]
    public float Slope
    {
        get => _slope;
        set
        {
            if (MathHelper.CloseTo(_slope, value)) return;
            _slope = value;
            OnModified?.Invoke();
        }
    }

    [DataField("enabled")]
    [ViewVariables]
    private bool _enabled = true;

    [ViewVariables(VVAccess.ReadWrite)]
    public bool Enabled
    {
        get => _enabled;
        set
        {
            if (_enabled == value) return;
            _enabled = value;
            OnModified?.Invoke();
        }
    }

    [ViewVariables]
    public DynamicTree.Proxy Proxy = DynamicTree.Proxy.Free;
}
