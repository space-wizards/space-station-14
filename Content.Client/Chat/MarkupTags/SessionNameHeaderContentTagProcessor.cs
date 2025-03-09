using Content.Shared.Chat.ContentMarkupTags;
using Robust.Shared.Utility;

namespace Content.Client.Chat.MarkupTags;

public sealed class SessionNameHeaderContentTagProcessor : ContentMarkupTagProcessorBase
{
    public const string SupportedNodeName = "SessionNameHeader";

    private readonly string? _name;

    /// <inheritdoc />
    public SessionNameHeaderContentTagProcessor(MarkupNode node)
    {
        _name = node.Value.StringValue;
    }

    public override string Name => SupportedNodeName;

    public override IReadOnlyList<MarkupNode> ProcessOpeningTag(MarkupNode node, int randomSeed)
    {
        if (_name == null)
            return [];

        return new List<MarkupNode> { new MarkupNode(_name) };
    }
}
