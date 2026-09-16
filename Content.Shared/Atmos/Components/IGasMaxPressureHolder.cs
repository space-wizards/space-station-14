using Robust.Shared.Audio;

namespace Content.Shared.Atmos.Components;

/// <summary>
/// This is an interface meant to designate an atmos device which can hold, a maximum pressurized volume of gas.
/// This device may fail if it exceeds the maximum pressure for too long.
/// </summary>
public interface IGasMaxPressureHolder : IGasMixtureHolder
{
    /// <summary>
    /// Sound made when this device is destroyed from its <see cref="Integrity"/> reaching 0.
    /// </summary>
    SoundSpecifier? RuptureSound { get; set; }

    /// <summary>
    ///     Maximum pressure at which this atmos device will activate any emergency safety features, if it has any.
    /// </summary>
    float SafetyPressure { get; set; }

    /// <summary>
    ///     Maximum pressure this device can handle before it starts losing <see cref="Integrity"/>.
    /// </summary>
    float Overpressure { get; set; }

    /// <summary>
    ///     Popup alert for when this entity's pressure exceeds max pressure.
    /// </summary>
    LocId? SafetyAlert { get; set; }

    /// <summary>
    ///     How many over-pressures until this gas tank detonates.
    ///     An overpressure is defined as pressure exceeding <see cref="Overpressure"/>
    ///     This determines the maximum value
    /// </summary>
    float MaxIntegrity { get; set; }

    /// <summary>
    ///     How many over-pressures until this gas tank detonates.
    ///     An overpressure is defined as pressure exceeding <see cref="Overpressure"/>
    ///     This determines the current value
    /// </summary>
    float Integrity { get; set; }
}
