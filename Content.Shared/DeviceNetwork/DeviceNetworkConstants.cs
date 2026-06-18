using Robust.Shared.Utility;

namespace Content.Shared.DeviceNetwork;

/// <summary>
/// A collection of constants to help with using device networks
/// </summary>
public static class DeviceNetworkConstants
{
    /// <summary>
    /// The key for command names
    /// E.g. [DeviceNetworkConstants.Command] = "ping"
    /// </summary>
    public const string Command = "command";

    #region DisplayHelpers

    /// <summary>
    /// Converts the unsigned int to string and inserts a number before the last digit
    /// </summary>
    public static string FrequencyToString(this uint frequency)
    {
        var result = frequency.ToString();
        if (result.Length <= 2)
            return result + ".0";

        return result.Insert(result.Length - 1, ".");
    }

    /// <summary>
    /// Either returns the localized name representation of the corresponding <see cref="DeviceNetIdDefaults"/>
    /// or converts the id to string
    /// </summary>
    public static string DeviceNetIdToLocalizedName(this int id)
    {
        if (!Enum.IsDefined(typeof(DeviceNetIdDefaults), id))
            return id.ToString();

        var result = ((DeviceNetIdDefaults) id).ToString();
        var resultKebab = "device-net-id-" + CaseConversion.PascalToKebab(result);

        return !Loc.TryGetString(resultKebab, out var name) ? result : name;
    }

    #endregion
}
