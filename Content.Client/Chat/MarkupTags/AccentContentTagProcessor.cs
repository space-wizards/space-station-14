using Content.Shared.Chat.ContentMarkupTags;
using Content.Shared.Speech.EntitySystems;
using Robust.Shared.Utility;

namespace Content.Client.Chat.MarkupTags;

public sealed class AccentContentTagProcessor : ContentMarkupTagProcessorBase
{
    public const string SupportedNodeName = "Accent";


    [Dependency] private readonly IEntityManager _entManager = default!;
    private readonly string? _accent;

    /// <inheritdoc />
    public AccentContentTagProcessor(MarkupNode node)
    {
        if (node.Value.TryGetString(out var accentName))
        {
            _accent = accentName;
        }
    }

    public override string Name => SupportedNodeName;

    public override IReadOnlyList<MarkupNode> ProcessTextNode(MarkupNode node, int randomSeed)
    {
        IoCManager.InjectDependencies(this);

        if (_entManager.System<SharedAccentSystem>().TryGetAccent(_accent, out var accent))
            return new [] { new MarkupNode(accent.Accentuate(node.Value.StringValue!, randomSeed)) };

        return new[] { node };
    }
}
