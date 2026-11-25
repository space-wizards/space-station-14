using Content.Shared.Temperature.Components;

namespace Content.Server.Atmos.Components;

// not if i get there first - Flipp
/// <summary>
/// Represents that entity can be exposed to Atmos
/// </summary>
[RegisterComponent, AutoGenerateComponentPause]
public sealed partial class AtmosExposedComponent : Component
{
    /// <summary>
    /// Last real time we were exposed to the atmosphere!
    /// </summary>
    [DataField, AutoPausedField]
    public TimeSpan LastExposure = TimeSpan.Zero;

    /// <summary>
    /// The amount of surface area this entity has exposed to the atmosphere in m^2.
    /// The human body has about 2m^2 of surface area so we use that as a default.
    /// This is multiplied by the <see cref="TemperatureComponent.ThermalConductivity"/>
    /// Source: https://pmc.ncbi.nlm.nih.gov/articles/PMC8953946/
    /// </summary>
    [DataField]
    public float ExposedArea = 2f;
}
