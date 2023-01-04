using Pidgin;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Utility;
using System.Linq;
using static Pidgin.Parser;
using static Pidgin.Parser<char>;
using static Robust.Client.UserInterface.Control;
using static Robust.Client.UserInterface.Controls.BoxContainer;

namespace Content.Client.Guidebook;

public sealed partial class GuidebookParsingManager
{
    private const string ListBullet = "  › ";

    #region Text Parsing
    #region Basic Text Parsing
    // Try look for an escaped character. If found, skip the escaping slash and return the character.
    private static readonly Parser<char, char> TryEscapedChar = Try(Char('\\').Then(OneOf(
     Try(Char('<')),
     Try(Char('>')),
     Try(Char('\\')),
     Try(Char('-')),
     Try(Char('\n')),
     Try(Char('=')),
     Try(Char('"'))
     )));

    private static readonly Parser<char, Unit> SkipNewline = Whitespace.SkipUntil(Char('\n'));

    private static readonly Parser<char, char> TrySingleNewlineToSpace = Try(SkipNewline).Then(SkipWhitespaces).ThenReturn(' ');

    private static readonly Parser<char, char> TextChar = OneOf(
        TryEscapedChar, // consume any backslashed being used to escape text
        TrySingleNewlineToSpace, // turn single newlines into spaces
        Any // just return the character.
        );
    #endregion

    #region Quoted Text
    // try return text wrapped in quotation marks.
    private static readonly Parser<char, string> TryQuotedText = Try(Char('"')).Then(TextChar.Until(Char('"')).Select(string.Concat).Labelled("closing quote"));

    // consumes either a block of quoted text or a single char.
    private static readonly Parser<char, string> TryQuoteOrChar = OneOf(TryQuotedText, TextChar.Select(x => x.ToString()));
    #endregion

    #region rich text-end markers
    private static readonly Parser<char, Unit> TryStartList = Try(SkipNewline.Then(SkipWhitespaces).Then(Char('-'))).Then(SkipWhitespaces);
    private static readonly Parser<char, Unit> TryStartTag = Try(Char('<')).Then(SkipWhitespaces);
    private static readonly Parser<char, Unit> TryStartParagraph = Try(SkipNewline.Then(SkipNewline)).Then(SkipWhitespaces);
    private static readonly Parser<char, Unit> TryLookTextEnd = Lookahead(OneOf(TryStartTag, TryStartList, TryStartParagraph, Try(Whitespace.SkipUntil(End))));
    #endregion

    // parses text characters until it hits a text-end
    private static readonly Parser<char, string> TextParser = TextChar.AtLeastOnceUntil(TryLookTextEnd).Select(string.Concat);

    private static readonly Parser<char, Control> TextControlParser = Try(Map(text =>
    {
        var rt = new RichTextLabel()
        {
            HorizontalExpand = true,
            Margin = new Thickness(0, 0, 0, 15.0f),
        };

        var msg = new FormattedMessage();
        // THANK YOU RICHTEXT VERY COOL
        // (text doesn't default to white).
        msg.PushColor(Color.White);
        msg.AddMarkup(text);
        msg.Pop();
        rt.SetMessage(msg);
        return rt;
    }, TextParser).Cast<Control>()).Labelled("richtext");
    #endregion

    // simple parser for a header. simply consumes anything between a # and a newline.
    private static readonly Parser<char, Control> HeaderControlParser = Try(Char('#')).Then(SkipWhitespaces.Then(Map(text => new Label()
    {
        Text = text,
        StyleClasses = { "LabelHeading" }
    }, AnyCharExcept('\n').AtLeastOnceString()).Cast<Control>())).Labelled("header");

    // Parser that consumes a - and then just parses normal rich text with some prefix text (a bullet point).
    private static readonly Parser<char, Control> ListControlParser = Try(Char('-')).Then(SkipWhitespaces).Then(Map(
        control => new BoxContainer()
        {
            Children = { new Label() { Text = ListBullet, VerticalAlignment = VAlignment.Top, }, control },
            Orientation = LayoutOrientation.Horizontal,
        }, TextControlParser).Cast<Control>()).Labelled("list");

    #region Tag Parsing
    // closing brackets for tags
    private static readonly Parser<char, Unit> TagEnd = Char('>').Then(SkipWhitespaces);
    private static readonly Parser<char, Unit> ImmediateTagEnd = String("/>").Then(SkipWhitespaces);

    private static readonly Parser<char, Unit> TryLookTagEnd = Lookahead(OneOf(Try(TagEnd), Try(ImmediateTagEnd)));

    // delimiter for tag arguments (whitespace or a tag end)
    private static readonly Parser<char, Unit> TryTagArgEnd = OneOf(Try(Whitespace.SkipAtLeastOnce()), TryLookTagEnd);
    private static readonly Parser<char, Unit> TryTagArgDelim = OneOf(Lookahead(Try(Char('='))).ThenReturn(Unit.Value), TryTagArgEnd);

    private static readonly Parser<char, string> TryTagArgKey = TryQuoteOrChar.Until(TryTagArgDelim).Select(string.Concat);
    private static readonly Parser<char, string> TryTagArgValue = Try(Char('=').Then(TryQuoteOrChar.Until(TryTagArgEnd).Select(string.Concat))).Or(Return<string>(default!));

    // parser for a singular tag argument. Note that each TryQuoteOrChar will consume a whole quoted block before the Until() looks for whitespace
    private static readonly Parser<char, (string, string)> TagArgParser = Map((key, value) => (key, value), TryTagArgKey, TryTagArgValue ).Before(SkipWhitespaces);

    // parser for all tag arguments
    private static readonly Parser<char, IEnumerable<(string, string)>> TagArgsParser =
        OneOf(TryLookTagEnd.ThenReturn(Enumerable.Empty<(string, string)>()), TagArgParser.Until(TryLookTagEnd));

    // parser for an opening tag.
    private static Parser<char, Unit> TryOpeningTag(string tag) =>
        Try(Char('<').Then(SkipWhitespaces).Then(String(tag)).Then(OneOf(Whitespace.SkipAtLeastOnce(), TryLookTagEnd))).Labelled($"opening {tag}");

    private static Parser<char, (List<string>, Dictionary<string, string>)> ParseTagArgs(string tag) =>
        SkipWhitespaces.Then(TagArgsParser.Labelled($"{tag} arguments")).Select(ProcessArgs).Before(SkipWhitespaces);

    private static Parser<char, Unit> TryTagTerminator(string tag)
        => Try(String("</")).Then(SkipWhitespaces).Then(String(tag)).Then(SkipWhitespaces).Then(TagEnd).Labelled($"closing {tag}");

    // given a list of strings, split over equal signs and create a dictionary+list.
    private static (List<string>, Dictionary<string, string>) ProcessArgs(IEnumerable<(string, string)> args)
    {
        var list = new List<string>();
        var dict = new Dictionary<string, string>();

        foreach (var (key, value) in args)
        {
            if (string.IsNullOrEmpty(key))
            {
                Logger.Error($"Encountered empty key string while processing args: {string.Join(", ", args)}");
                break;
            }

            if (value == null)
                list.Add(key);
            else
                dict.Add(key, value);
        }

        return (list, dict);
    }
    #endregion
}
