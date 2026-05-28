using Content.Server.Temperature.Systems;
using Content.Shared.Atmos;
using Content.Shared.Temperature.Components;
using Content.Shared.Temperature.HeatContainer;

namespace Content.Server.Temperature.Components;

/// <summary>
/// Entity has an internal temperature which conducts heat from its surface.
/// Requires <see cref="TemperatureComponent"/> to function.
/// </summary>
/// <remarks>
/// Currently this is only used for cooking but animal metabolism could use it too.
/// Too hot? Suffering heatstroke, start sweating to cool off and increase thirst.
/// Too cold? Suffering hypothermia, start shivering to warm up and increase hunger.
/// </remarks>
[RegisterComponent, Access(typeof(TemperatureSystem))]
public sealed partial class InternalTemperatureComponent : Component, IHeatContainer
{
    // TODO: These values probably shouldn't be duplicated from temperature component, but they're only used for the chef atm so low priority.
    /// <summary>
    /// Internal temperature which is modified by surface temperature.
    /// This gets set to <see cref="TemperatureComponent.Temperature"/> on mapinit.
    /// </summary>
    [DataField]
    public float Temperature { get; set; } = Atmospherics.T20C;

    /// <summary>
    /// Heat capacity of our internal temperature.
    /// This gets set to <see cref="TemperatureComponent.HeatCapacity"/> on mapinit.
    /// </summary>
    [DataField]
    public float HeatCapacity { get; set; }

    /// <summary>
    /// Thermal Conductance in W/K to this entity's <see cref="TemperatureComponent"/>.
    /// Roughly estimated by multiplying meat's thermal conductivity of about 0.4 W/(m*K) by the total surface area of the meat,
    /// and dividing by the thickness of the meat.
    /// Then we multiply by four because we should only care about half the thickness typically, and also we're sharing a heat capacity.
    /// Yes this is stupid. I'll care when chef has content or this is used by BodySystem.
    /// No I'm not doing a custom value for each piece of meat.
    /// </summary>
    [DataField]
    public float Conductance = 40f;


}
