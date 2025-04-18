using System.Linq;
using Content.Client.Guidebook.Controls;
using Content.Client.Guidebook.Richtext;
using Content.Shared.Guidebook;
using Pidgin;
using Robust.Client.UserInterface;
using Robust.Shared.ContentPack;
using Robust.Shared.Prototypes;
using Robust.Shared.Reflection;
using Robust.Shared.Sandboxing;
using Robust.Shared.Utility;
using static Pidgin.Parser;

namespace Content.Client.Guidebook;

/// <summary>
///     This manager should be used to convert documents (shitty rich-text / pseudo-xaml) into UI Controls
/// </summary>
public sealed partial class DocumentParsingManager
{
    [Dependency] private readonly IPrototypeManager _prototype = default!;
    [Dependency] private readonly IReflectionManager _reflectionManager = default!;
    [Dependency] private readonly IResourceManager _resourceManager = default!;
    [Dependency] private readonly ISandboxHelper _sandboxHelper = default!;

    private readonly Dictionary<string, Parser<char, Control>> _tagControlParsers = new();
    private Parser<char, Control> _controlParser = default!;

    private ISawmill _sawmill = default!;
    private Parser<char, Control> _tagParser = default!;
    public Parser<char, IEnumerable<Control>> ControlParser = default!;

    public void Initialize()
    {
        _tagParser = TryOpeningTag
            .Assert(_tagControlParsers.ContainsKey, tag => $"unknown tag: {tag}")
            .Bind(tag => _tagControlParsers[tag]);

        var whitespaceAndCommentParser = SkipWhitespaces.Then(Try(String("<!--").Then(Parser<char>.Any.SkipUntil(Try(String("-->"))))).SkipMany());

        _controlParser = OneOf(_tagParser, TryHeaderControl, ListControlParser, TextControlParser)
            .Before(whitespaceAndCommentParser);

        foreach (var typ in _reflectionManager.GetAllChildren<IDocumentTag>())
        {
            _tagControlParsers.Add(typ.Name, CreateTagControlParser(typ.Name, typ, _sandboxHelper));
        }

        ControlParser = whitespaceAndCommentParser.Then(_controlParser.Many());

        _sawmill = Logger.GetSawmill("Guidebook");
    }

    public bool TryAddMarkup(Control control, ProtoId<GuideEntryPrototype> entryId, bool log = true)
    {
        if (!_prototype.TryIndex(entryId, out var entry))
            return false;

        using var file = _resourceManager.ContentFileReadText(entry.Text);
        return TryAddMarkup(control, file.ReadToEnd(), log);
    }

    public bool TryAddMarkup(Control control, GuideEntry entry, bool log = true)
    {
        using var file = _resourceManager.ContentFileReadText(entry.Text);
        return TryAddMarkup(control, file.ReadToEnd(), log);
    }

    public bool TryAddMarkup(Control control, string text, bool log = true)
    {
        try
        {
            foreach (var child in ControlParser.ParseOrThrow(text))
            {
                control.AddChild(child);
            }
        }
        catch (Exception e)
        {
            _sawmill.Error($"Encountered error while generating markup controls: {e}");

            control.AddChild(new GuidebookError(text, e.ToStringBetter()));

            return false;
        }

        return true;
    }

    private Parser<char, Control> CreateTagControlParser(string tagId, Type tagType, ISandboxHelper sandbox)
    {
        return Map(
                (args, controls) =>
                {
                    try
                    {
                        var tag = (IDocumentTag) sandbox.CreateInstance(tagType);
                        if (!tag.TryParseTag(args, out var control))
                        {
                            _sawmill.Error($"Failed to parse {tagId} args");
                            return new GuidebookError(args.ToString() ?? tagId, $"Failed to parse {tagId} args");
                        }

                        foreach (var child in controls)
                        {
                            control.AddChild(child);
                        }

                        return control;
                    }
                    catch (Exception e)
                    {
                        var output = args.Aggregate(string.Empty,
                            (current, pair) => current + $"{pair.Key}=\"{pair.Value}\" ");

                        _sawmill.Error($"Tag: {tagId} \n Arguments: {output}/>");
                        return new GuidebookError($"Tag: {tagId}\nArguments: {output}", e.ToString());
                    }
                },
                ParseTagArgs(tagId),
                TagContentParser(tagId))
            .Labelled($"{tagId} control");
    }

    // Parse a bunch of controls until we encounter a matching closing tag.
    private Parser<char, IEnumerable<Control>> TagContentParser(string tag)
    {
        return OneOf(
            Try(ImmediateTagEnd).ThenReturn(Enumerable.Empty<Control>()),
            TagEnd.Then(_controlParser.Until(TryTagTerminator(tag)).Labelled($"{tag} children"))
        );
    }
}
