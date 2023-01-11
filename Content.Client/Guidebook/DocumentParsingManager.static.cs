using System.Linq;
using Pidgin;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Utility;
using static Pidgin.Parser;
using static Pidgin.Parser<char>;
using static Robust.Client.UserInterface.Control;
using static Robust.Client.UserInterface.Controls.BoxContainer;

namespace Content.Client.Guidebook;

public sealed partial class DocumentParsingManager
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
     Try(Char('=')),
     Try(Char('"')),
     Try(Char(' ')),
     Try(Char('n')).ThenReturn('\n'),
     Try(Char('t')).ThenReturn('\t')
     )));

    private static readonly Parser<char, Unit> SkipNewline = Whitespace.SkipUntil(Char('\n'));

    private static readonly Parser<char, char> TrySingleNewlineToSpace = Try(SkipNewline).Then(SkipWhitespaces).ThenReturn(' ');

    private static readonly Parser<char, char> TextChar = OneOf(
        TryEscapedChar, // consume any backslashed being used to escape text
        TrySingleNewlineToSpace, // turn single newlines into spaces
        Any // just return the character.
        );

    // like TextChar, but not skipping whitespace around newlines
    private static readonly Parser<char, char> QuotedTextChar = OneOf(TryEscapedChar, Any);

    // Quoted text
    private static readonly Parser<char, string> QuotedText = Char('"').Then(QuotedTextChar.Until(Try(Char('"'))).Select(string.Concat)).Labelled("quoted text");
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

    #region Headers
    private static readonly Parser<char, Control> HeaderControlParser = Try(Char('#')).Then(SkipWhitespaces.Then(Map(text => new Label()
    {
        Text = text,
        StyleClasses = { "LabelHeadingBigger" }
    }, AnyCharExcept('\n').AtLeastOnceString()).Cast<Control>())).Labelled("header");

    private static readonly Parser<char, Control> SubHeaderControlParser = Try(String("##")).Then(SkipWhitespaces.Then(Map(text => new Label()
    {
        Text = text,
        StyleClasses = { "LabelHeading" }
    }, AnyCharExcept('\n').AtLeastOnceString()).Cast<Control>())).Labelled("subheader");

    private static readonly Parser<char, Control> TryHeaderControl = OneOf(SubHeaderControlParser, HeaderControlParser);
    #endregion

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

    //parse tag argument key. any normal text character up until we hit a "="
    private static readonly Parser<char, string> TagArgKey = LetterOrDigit.Until(Char('=')).Select(string.Concat).Labelled("tag argument key");

    // parser for a singular tag argument. Note that each TryQuoteOrChar will consume a whole quoted block before the Until() looks for whitespace
    private static readonly Parser<char, (string, string)> TagArgParser = Map((key, value) => (key, value), TagArgKey, QuotedText).Before(SkipWhitespaces);

    // parser for all tag arguments
    private static readonly Parser<char, IEnumerable<(string, string)>> TagArgsParser = TagArgParser.Until(TryLookTagEnd);

    // parser for an opening tag.
    private static readonly Parser<char, string> TryOpeningTag =
        Try(Char('<'))
        .Then(SkipWhitespaces)
        .Then(TextChar.Until(OneOf(Whitespace.SkipAtLeastOnce(), TryLookTagEnd)))
        .Select(string.Concat).Labelled($"opening tag");

    private static Parser<char, Dictionary<string, string>> ParseTagArgs(string tag)
    {
        return TagArgsParser.Labelled($"{tag} arguments")
            .Select(x => x.ToDictionary(y => y.Item1, y => y.Item2))
            .Before(SkipWhitespaces);
    }

    private static Parser<char, Unit> TryTagTerminator(string tag)
    {
        return Try(String("</"))
            .Then(SkipWhitespaces)
            .Then(String(tag))
            .Then(SkipWhitespaces)
            .Then(TagEnd)
            .Labelled($"closing {tag} tag");
    }
    #endregion
}
