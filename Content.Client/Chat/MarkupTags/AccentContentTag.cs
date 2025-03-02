using Content.Shared.Chat.ContentMarkupTags;
using Content.Shared.Speech.EntitySystems;
using Robust.Shared.Utility;

namespace Content.Client.Chat.MarkupTags;

public sealed class AccentContentTag : IContentMarkupTag
{
    public string Name => "Accent";

    [Dependency] private readonly IEntityManager _entManager = default!;

    private string? _accent;

    public List<MarkupNode>? ProcessOpeningTag(MarkupNode node, int randomSeed)
    {
        if (node.Value.TryGetString(out var accentName))
        {
            _accent = accentName;
            return null;
        }

        _accent = null;
        return null;
    }

    public List<MarkupNode>? ProcessTextNode(MarkupNode node, int randomSeed)
    {
        IoCManager.InjectDependencies(this);

        if (_entManager.System<SharedAccentSystem>().TryGetAccent(_accent, out var accent))
            return new List<MarkupNode>() { new MarkupNode(accent.Accentuate(node.Value.StringValue!, randomSeed)) };

        return new List<MarkupNode>() { node };
    }

}
