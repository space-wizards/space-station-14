using System.Linq;
using Content.Shared.DeviceNetwork.Components;
using Robust.Shared.Prototypes;

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
        return GetAddressFromId(comp.Address, comp.Prefix);
    }

    /// <summary>
    /// Gets the readable string from an array of device network prototype IDs,
    /// represented by their names separated with commas.
    /// </summary>
    public static string GetDeviceNetIdString(ProtoId<DeviceNetworkPrototype>[] deviceNets, IPrototypeManager manager)
    {
        return string.Join(", ", deviceNets.Select(id => Loc.GetString(manager.Index(id).Name)));
    }
}
