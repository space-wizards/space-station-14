using System.Diagnostics.CodeAnalysis;
using Content.Client.UserInterface.Systems.Chat;
using Content.Shared.Chat;
using Content.Shared.Chat.ContentMarkupTags;
using Content.Shared.Speech.EntitySystems;
using Robust.Shared.Utility;

namespace Content.Client.Chat.MarkupTags;

public sealed class AccentContentTagProcessor : ContentMarkupTagProcessorBase
{
    public const string SupportedNodeName = "Accent";

    [Dependency] private readonly IEntityManager _entManager = default!;

    private readonly string _accent;
    private readonly Dictionary<string,MarkupParameter> _attributes;
    private readonly int _seed;

    private AccentContentTagProcessor(string accentName, Dictionary<string,MarkupParameter> attributes, int seed)
    {
        _accent = accentName;
        _attributes = attributes;
        _seed = seed;
    }

    public override string Name => SupportedNodeName;

    public override IReadOnlyList<MarkupNode> ProcessTextNode(MarkupNode node)
    {
        IoCManager.InjectDependencies(this);

        if (_entManager.System<SharedAccentSystem>().TryGetAccent(_accent, out var accent))
            return new [] { new MarkupNode(accent.Accentuate(node.Value.StringValue!, _attributes, _seed)) };

        return new[] { node };
    }

    public static bool TryCreate(
        MarkupNode node,
        ChatMessageContext context,
        [NotNullWhen(true)] out ContentMarkupTagProcessorBase? processor
    )
    {
        if (!node.Value.TryGetString(out var accentName)
            || !context.TryGet<int>(ChatMessageContextParameters.MessageId, out var seed))
        {
            processor = null;
            return false;
        }

        processor = new AccentContentTagProcessor(accentName, node.Attributes, seed);
        return true;
    }
}
