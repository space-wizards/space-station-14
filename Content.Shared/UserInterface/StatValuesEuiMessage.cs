using Content.Shared.Eui;
using Robust.Shared.Serialization;

namespace Content.Shared.UserInterface;

/// <summary>
/// It's a message not a state because it's for debugging and it makes it easier to bootstrap more data dumping.
/// </summary>
[Serializable, NetSerializable]
public sealed class StatValuesEuiMessage : EuiMessageBase
{
    /// <summary>
    /// Titles for the window.
    /// </summary>
    public string Title = string.Empty;
    public List<string> Headers = new();
    public List<string[]> Values = new();
}
