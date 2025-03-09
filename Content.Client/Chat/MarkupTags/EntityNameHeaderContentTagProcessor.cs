using Content.Shared.Chat.ContentMarkupTags;
using Robust.Shared.Utility;

namespace Content.Client.Chat.MarkupTags;

public sealed class EntityNameHeaderContentTagProcessor : ContentMarkupTagProcessorBase
{
    public const string SupportedNodeName = "EntityNameHeader";

    private readonly string? _name;

    /// <inheritdoc />
    public EntityNameHeaderContentTagProcessor(MarkupNode node)
    {
        _name = node.Value.StringValue;
    }

    public override string Name => SupportedNodeName;

    public override IReadOnlyList<MarkupNode> ProcessOpeningTag(MarkupNode node, int randomSeed)
    {
        
        if (_name == null)
            return [];

        return new [] {new MarkupNode(_name) };
    }
}
