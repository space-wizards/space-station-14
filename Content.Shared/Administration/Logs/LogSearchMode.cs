using Robust.Shared.Serialization;

namespace Content.Shared.Administration.Logs;

/// <summary>
/// Controls how log search text is interpreted by the database query layer.
/// </summary>
[Serializable, NetSerializable]
public enum LogSearchMode : byte
{
    Keyword = 0,
    Regex = 1,
    Wildcard = 2,
    Exact = 3,
}
