using Content.Server.Temperature.Systems;

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
public sealed partial class InternalTemperatureComponent : Component
{
    /// <summary>
    /// Internal temperature which is modified by surface temperature.
    /// This gets set to <see cref="TemperatureComponent.CurrentTemperature"/> on mapinit.
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public float Temperature;

    /// <summary>
    /// Thermal conductivity of the material in W/m/K.
    /// Higher conductivity means its insides will heat up faster.
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public float Conductivity = 0.5f;

    /// <summary>
    /// Average thickness between the surface and the inside.
    /// For meats and such this is constant.
    /// Thicker materials take longer for heat to dissipate.
    /// </summary>
    [DataField(required: true), ViewVariables(VVAccess.ReadWrite)]
    public float Thickness;

    /// <summary>
    /// Surface area in m^2 for the purpose of conducting surface temperature to the inside.
    /// Larger surface area means it takes longer to heat up/cool down
    /// </summary>
    /// <remarks>
    /// For meats etc this should just be the area of the cooked surface not the whole thing as it's only getting heat from one side usually.
    /// </remarks>
    [DataField(required: true), ViewVariables(VVAccess.ReadWrite)]
    public float Area;
}
