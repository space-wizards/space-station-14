using System.Text.RegularExpressions;
using Robust.Shared.Serialization;

namespace Content.Shared.Disposal;

[Serializable, NetSerializable]
public sealed class DisposalRouterUserInterfaceState : BoundUserInterfaceState
{
    public readonly string Tags;

    public DisposalRouterUserInterfaceState(string tags)
    {
        Tags = tags;
    }
}

[Serializable, NetSerializable]
public sealed class RouterSetTagsMessage : BoundUserInterfaceMessage
{
    public static readonly Regex TagsRegex = new("^[a-zA-Z0-9, ]*$", RegexOptions.Compiled);

    public readonly string Tags;

    public RouterSetTagsMessage(string tags)
    {
        Tags = tags.Substring(0, Math.Min(tags.Length, 150));
    }
}

[Serializable, NetSerializable]
public enum DisposalRouterUiKey
{
    Key
}
