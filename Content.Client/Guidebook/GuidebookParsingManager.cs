using Content.Client.Guidebook.Richtext;
using Pidgin;
using Robust.Client.UserInterface;
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
    private Parser<char, Control> _controlParser = default!;
    public Parser<char, Document> DocumentParser = default!;

    public void Initialize()
    {
        _controlParser = OneOf(Rec(() => OneOf(_tagControlParsers)), HeaderControlParser, ListControlParser, TextControlParser).Before(SkipWhitespaces);

        foreach (var typ in _reflectionManager.GetAllChildren<ITag>())
        {
            _tagControlParsers.Add(CreateTagControlParser(typ.Name, typ, _sandboxHelper));
        }

        DocumentParser = SkipWhitespaces.Then(Map((x) => new Document(x), _controlParser.Many()));
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
}
