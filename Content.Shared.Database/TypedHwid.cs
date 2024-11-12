using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;

namespace Content.Shared.Database;

/// <summary>
/// Represents a raw HWID value together with its type.
/// </summary>
[Serializable]
public sealed class ImmutableTypedHwid(ImmutableArray<byte> hwid, HwidType type)
{
    public readonly ImmutableArray<byte> Hwid = hwid;
    public readonly HwidType Type = type;

    public override string ToString()
    {
        var b64 = Convert.ToBase64String(Hwid.AsSpan());
        return Type == HwidType.Modern ? $"V2-{b64}" : b64;
    }

    public static bool TryParse(string value, [NotNullWhen(true)] out ImmutableTypedHwid? hwid)
    {
        var type = HwidType.Legacy;
        if (value.StartsWith("V2-", StringComparison.Ordinal))
        {
            value = value["V2-".Length..];
            type = HwidType.Modern;
        }

        var array = new byte[GetBase64ByteLength(value)];
        if (!Convert.TryFromBase64String(value, array, out _))
        {
            hwid = null;
            return false;
        }

        hwid = new ImmutableTypedHwid([..array], type);
        return true;
    }

    private static int GetBase64ByteLength(string value)
    {
        // Why is .NET like this man wtf.
        return 3 * (value.Length / 4) - value.TakeLast(2).Count(c => c == '=');
    }
}

/// <summary>
/// Represents different types of HWIDs as exposed by the engine.
/// </summary>
public enum HwidType
{
    /// <summary>
    /// The legacy HWID system. Should only be used for checking old existing database bans.
    /// </summary>
    Legacy = 0,

    /// <summary>
    /// The modern HWID system.
    /// </summary>
    Modern = 1,
}
