using Content.Shared.DeviceNetwork.Components;

namespace Content.Shared.DeviceNetwork;

/// <summary>
/// A helper class for localization of frequencies and device network IDs.
/// </summary>
public static class DeviceLocalizationHelpers
{
    /// <summary>
    /// A helper method to get the frequency string representation.
    /// </summary>
    /// <remarks>
    /// Decimal point separates the last digit, and a zero gets added at the end if the frequency is 2 digits or fewer.
    /// </remarks>
    public static string FrequencyToString(DeviceFrequency? frequency)
    {
        return frequency == null ? string.Empty : frequency.Value.ToString();
    }

    /// <summary>
    /// Gets the readable device address from a <see cref="DeviceAddress"/> and an optional localized prefix.
    /// </summary>
    /// <remarks>
    /// The address gets converted into its HEX representation,
    /// and a prefix is added in front if a prefix is specified.
    /// </remarks>
    public static string GetAddressFromId(DeviceAddress addressId, LocId? prefix)
    {
        return new LocDeviceAddress(addressId, prefix).ToString();
    }

    /// <summary>
    /// Gets the readable device address from a <see cref="DeviceNetworkComponent"/>.
    /// </summary>
    /// <remarks>
    /// The address gets converted into its HEX representation,
    /// and a prefix is added in front if a prefix is specified.
    /// </remarks>
    public static string GetAddressFromId(DeviceNetworkComponent comp)
    {
        return GetAddressFromId(comp.Data.AddressId, comp.Prefix);
    }
}
