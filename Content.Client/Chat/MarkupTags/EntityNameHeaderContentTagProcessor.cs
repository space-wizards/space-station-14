using System.Diagnostics.CodeAnalysis;
using Content.Shared.Chat;
using Content.Shared.Chat.ContentMarkupTags;
using Robust.Shared.Utility;

namespace Content.Client.Chat.MarkupTags;

public sealed class EntityNameHeaderContentTagProcessor : ContentMarkupTagProcessorBase
{
    public const string SupportedNodeName = "EntityNameHeader";

    private readonly string _name;

    /// <inheritdoc />
    public EntityNameHeaderContentTagProcessor(string name)
    {
        _name = name;
    }

    public override string Name => SupportedNodeName;

    public override IReadOnlyList<MarkupNode> ProcessOpeningTag(MarkupNode node)
    {
        return new [] {new MarkupNode(_name) };
    }

    public static bool TryCreate(
        MarkupNode node,
        ChatMessageContext context,
        [NotNullWhen(true)] out ContentMarkupTagProcessorBase? processor
    )
    {
        if (!node.Value.TryGetString(out var val))
        {
            processor = null;
            return false;
        }

        processor = new EntityNameHeaderContentTagProcessor(val);
        return true;
    }
}
