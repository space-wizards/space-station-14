using Content.Shared.DeviceNetwork.Components;
using Robust.Shared.Utility;

namespace Content.Shared.DeviceNetwork;

/// <summary>
/// A helper class for localization of frequencies and device network IDs.
/// </summary>
public static class DeviceLocalizationHelpers
{
    /// <summary>
    /// Converts the unsigned int to string and inserts a number before the last digit
    /// </summary>
    public static string FrequencyToString(uint? frequency)
    {
        if (frequency == null)
            return string.Empty;

        var result = frequency.Value.ToString();
        if (result.Length <= 2)
            return result + ".0";

        return result.Insert(result.Length - 1, ".");
    }

    /// <summary>
    /// Either returns the localized name representation of the corresponding <see cref="DeviceNetIdDefaults"/>
    /// or converts the id to string
    /// </summary>
    public static string DeviceNetIdToLocalizedName(int? id, ILocalizationManager localeMan)
    {
        if (id == null)
            return string.Empty;

        if (!Enum.IsDefined(typeof(DeviceNetIdDefaults), id))
            return id.Value.ToString();

        var result = ((DeviceNetIdDefaults) id).ToString();
        var resultKebab = "device-net-id-" + CaseConversion.PascalToKebab(result);

        return !localeMan.TryGetString(resultKebab, out var name) ? result : name;
    }

    /// <summary>
    /// Gets the readable device address from a <see cref="DeviceAddress"/> and an optional localized prefix.
    /// </summary>
    public static string GetAddressFromId(DeviceAddress addressId, LocId? prefixLoc)
    {
        var prefix = string.IsNullOrWhiteSpace(prefixLoc) ? null : Loc.GetString(prefixLoc);
        return $"{prefix}{addressId >> 16:X4}-{addressId & 0xFFFF:X4}";
    }

    public static string GetAddressFromId(DeviceNetworkComponent comp)
    {
        return GetAddressFromId(comp.Data.AddressId, comp.Prefix);
    }
}
