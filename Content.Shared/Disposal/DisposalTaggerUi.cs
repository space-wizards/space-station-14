using System.Text.RegularExpressions;
using Robust.Shared.Serialization;

namespace Content.Shared.Disposal;

[Serializable, NetSerializable]
public sealed class DisposalTaggerUserInterfaceState : BoundUserInterfaceState
{
    public readonly string Tag;

    public DisposalTaggerUserInterfaceState(string tag)
    {
        Tag = tag;
    }
}

[Serializable, NetSerializable]
public sealed class TaggerSetTagMessage : BoundUserInterfaceMessage
{
    public static readonly Regex TagRegex = new("^[a-zA-Z0-9 ]*$", RegexOptions.Compiled);

    public readonly string Tag;

    public TaggerSetTagMessage(string tag)
    {
        Tag = tag.Substring(0, Math.Min(tag.Length, 30));
    }
}

[Serializable, NetSerializable]
public enum DisposalTaggerUiKey
{
    Key
}
