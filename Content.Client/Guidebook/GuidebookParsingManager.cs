using Content.Client.Guidebook.Richtext;
using Pidgin;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Reflection;
using Robust.Shared.Sandboxing;
using System.Linq;
using static Pidgin.Parser;

namespace Content.Client.Guidebook;

public sealed partial class GuidebookParsingManager
{
    [Dependency] private readonly IReflectionManager _reflectionManager = default!;
    [Dependency] private readonly ISandboxHelper _sandboxHelper = default!;

    private readonly List<Parser<char, Control>> _tagControlParsers = new();
    public Parser<char, Control> _controlParser = default!;
    public Parser<char, IEnumerable<Control>> ControlParser = default!;
    public Parser<char, Document> DocumentParser = default!;

    public void Initialize()
    {
        _controlParser = OneOf(Rec(() => OneOf(_tagControlParsers)), HeaderControlParser, ListControlParser, TextControlParser).Before(SkipWhitespaces);

        foreach (var typ in _reflectionManager.GetAllChildren<ITag>())
        {
            _tagControlParsers.Add(CreateTagControlParser(typ.Name, typ, _sandboxHelper));
        }

        ControlParser = SkipWhitespaces.Then(_controlParser.Many());
        DocumentParser = ControlParser.Select(x => new Document(x));
    }

    public bool TryAddMarkup(Control control, string text)
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
            Logger.Error($"Caught error while generating markup controls. Error: {e}");
            control.AddChild(new Label() { Text = "Error" });
            return false;
        }

        return true;
    }

    private Parser<char, Control> CreateTagControlParser(string tagId, Type tagType, ISandboxHelper sandbox) => Map(
        (args, controls) =>
        {
            var tag = (ITag) sandbox.CreateInstance(tagType);
            if (!tag.TryParseTag(args.Item1, args.Item2, out var control))
            {
                Logger.Error($"Failed to parse {tagId} args");
                return new Control();
            }

            foreach (var child in controls)
            {
                control.AddChild(child);
            }
            return control;
        },
        TryOpeningTag(tagId).Then(ParseTagArgs(tagId)),
        TagContentParser(tagId));

    // Parse a bunch of controls until we encounter a matching closing tag.
    private Parser<char, IEnumerable<Control>> TagContentParser(string tag) =>
    OneOf(
        Try(ImmediateTagEnd).ThenReturn(Enumerable.Empty<Control>()),
        TagEnd.Then(_controlParser.Until(TryTagTerminator(tag)).Labelled($"{tag} children"))
    );
}
