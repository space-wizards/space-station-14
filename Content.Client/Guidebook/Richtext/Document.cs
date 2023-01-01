using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Reflection;
using Robust.Shared.Sandboxing;
using Robust.Shared.Utility;

namespace Content.Client.Guidebook.Richtext;

/// <summary>
/// A document, containing arbitrary text and UI elements.
/// </summary>
public sealed class Document : BoxContainer
{
    [Dependency] private readonly IReflectionManager _reflectionManager = default!;
    [Dependency] private readonly ISandboxHelper _sandboxHelper = default!;

    private Dictionary<string, Type> _tagTypes = new();

    public Document()
    {
        IoCManager.InjectDependencies(this);
        Orientation = LayoutOrientation.Vertical;
        foreach (var typ in _reflectionManager.GetAllChildren<ITag>())
        {
            _tagTypes[typ.Name] = typ;
        }
    }

    public Document(string contents) : this()
    {
        TryAddMarkup(contents);
        //if (!)
        //    throw new Exception("Failed to initialize a document, it was provided with bad contents!");

    }

    private enum ParserMode
    {
        Seeking,
        Text,
        List,
        Tag,
        Header,
    }

    private static bool ModeTextAlike(ParserMode mode)
    {
        return mode is ParserMode.Text or ParserMode.List;
    }

    public bool TryAddMarkup(string markup)
    {
        var state = ParserMode.Text;

        StringBuilder buffer = new();
        var parentStack = new List<Control> {this};
        var tagStack = new List<string>();

        var newLine = true;
        var priorEscape = false;

        var tagArgList = new List<string>();
        var tagParamList = new Dictionary<string, string>();
        bool noAppend = false;

        foreach (var (idx, rune) in markup.EnumerateRunes().Select(((rune, i) => (i, rune))))
        {
            tagArgList.Clear();
            tagParamList.Clear();
            var initialState = state;

            if (rune.Value == '\r')
                continue; // Go away, windows.

            var nextNewline = rune.Value == '\n';

            // Find state changes.
            switch (rune.Value)
            {
                case '#' when newLine && (ModeTextAlike(state) || state == ParserMode.Seeking) && !priorEscape:
                    state = ParserMode.Header;
                    noAppend = true;
                    break;
                case '-' when newLine && (ModeTextAlike(state) || state == ParserMode.Seeking) && !priorEscape:
                    state = ParserMode.List;
                    noAppend = true;
                    break;
                case '<' when (ModeTextAlike(state) || state == ParserMode.Seeking) && !priorEscape:
                    state = ParserMode.Tag;
                    noAppend = true;
                    break;
                case '>' when state == ParserMode.Tag:
                    state = ParserMode.Seeking;
                    noAppend = true;
                    break;
                case var _ when ((rune.Value != '\n') || (newLine && nextNewline)) && state == ParserMode.Seeking:
                    state = ParserMode.Text;
                    break;
                default:
                    state = state;
                    break;
            }

            if (rune.Value == '\\' && !priorEscape)
            {
                priorEscape = true;
                continue; // Just move on.
            }

            switch (rune.Value)
            {
                // Exiting header mode, add header to the output.
                case var _ when initialState == ParserMode.Header && nextNewline:
                {
                    var header = new Label()
                        {Text = buffer.ToString(), StyleClasses = {"LabelHeading"}};
                    parentStack[^1].AddChild(header);
                    buffer.Clear();
                    state = ParserMode.Text;
                    break;
                }
                // New paragraph.
                case var _ when (((newLine && nextNewline) || markup.Length == idx-1) && state == ParserMode.Text)
                                || (initialState == ParserMode.Text && state != ParserMode.Text):
                {
                    if ((buffer.Length == 0 && state != ParserMode.Text))
                    {
                        buffer.Clear();
                        break; // No stray whitespace in this case.
                    }

                    if (buffer.Length == 0 && !(initialState == ParserMode.Text && ModeTextAlike(state)))
                    {
                        buffer.Append(' ');
                    }

                    var rt = new RichTextLabel()
                    {
                        HorizontalExpand = true,
                        Margin = new Thickness(0, 0, 0, 15.0f), // TODO This should be font based!
                    };
                    var msg = new FormattedMessage();
                    // THANK YOU RICHTEXT VERY COOL
                    msg.PushColor(Color.White);
                    msg.AddMarkup(buffer.ToString());
                    msg.Pop();
                    rt.SetMessage(msg);
                    parentStack[^1].AddChild(rt);
                    buffer.Clear();

                    if (!(initialState == ParserMode.Text && state != ParserMode.Text))
                        state = ParserMode.Seeking;
                    break;
                }
                // New list entry.
                case var _ when (((newLine && nextNewline) || markup.Length == idx-1) && state == ParserMode.List)
                                || (newLine && rune.Value == '-' && initialState == ParserMode.List)
                                || (initialState == ParserMode.List && state != ParserMode.List):
                {
                    if (buffer.Length == 0)
                    {
                        buffer.Clear();
                        break; // No stray whitespace in this case.
                    }

                    var rt = new RichTextLabel()
                    {
                        HorizontalExpand = true,
                        Margin = new Thickness(0, 0, 0, 15.0f), // TODO This should be font based!
                    };
                    var msg = new FormattedMessage();
                    // THANK YOU RICHTEXT VERY COOL
                    msg.PushColor(Color.White);
                    msg.AddMarkup(buffer.ToString().TrimStart());
                    msg.Pop();
                    rt.SetMessage(msg);
                    parentStack[^1].AddChild(new BoxContainer()
                    {
                        Children =
                        {
                            new Label()
                            {
                                Text = "  › ",
                                VerticalAlignment = VAlignment.Top,
                            },
                            rt
                        },
                        Orientation = BoxContainer.LayoutOrientation.Horizontal,
                    });

                    buffer.Clear();

                    if (!(newLine && rune.Value == '-' && state == ParserMode.List) && !(initialState == ParserMode.List && state != ParserMode.List))
                        state = ParserMode.Seeking;

                    break;
                }
                case var _ when state != ParserMode.Tag && initialState == ParserMode.Tag:
                {
                    var tagText = buffer.ToString();
                    DigestIntoArgs(tagText, out var tag, ref tagArgList, ref tagParamList);
                    buffer.Clear();

                    if (tag is null)
                    {
                        Logger.Error($"Got an empty tag in a document at index {idx}!");
                        return false;
                    }

                    if (!_tagTypes.ContainsKey(tag))
                    {
                        Logger.Error($"Got the non-existent tag {tag} at {idx} in a document!");
                        return false;
                    }

                    if (priorEscape)
                    {
                        if (tagStack[^1] != tag)
                        {
                            Logger.Error($"Mismatched tag, ending at {idx}, the mismatch being {tag}");
                            return false;
                        }

                        tagStack.Pop();

                        var ctrl = parentStack.Pop();
                        parentStack[^1].AddChild(ctrl);
                        break;
                    }

                    var tagT = (ITag)_sandboxHelper.CreateInstance(_tagTypes[tag]);
                    if (!tagT.TryParseTag(tagArgList, tagParamList, out var control, out var instant))
                    {
                        Logger.Error($"Failed to parse {tag} with text <{tagText}> at {idx}.");
                        return false;
                    }

                    if (!instant)
                    {
                        tagStack.Add(tag);
                        parentStack.Add(control);
                    }
                    else
                    {
                        parentStack[^1].AddChild(control);
                    }

                    break;
                }
                default:
                {
                    if (nextNewline && state == ParserMode.Text)
                        buffer.Append(' '); // Paragraph spacing rules.

                    if (nextNewline)
                        break;

                    if (state != ParserMode.Seeking && !noAppend)
                        buffer.Append(rune);

                    break;
                }
            }

            newLine = nextNewline || (newLine && Rune.IsWhiteSpace(rune));
            priorEscape = false;
            noAppend = false;
        }

        return true;
    }

    private void DigestIntoArgs(string input, out string? tag, ref List<string> args,
        ref Dictionary<string, string> param)
    {
        var buffer = new StringBuilder(input.Length);
        var escaped = false;
        var quoted = false;
        tag = null;
        Rune.TryCreate(' ', out var augh);
        foreach (var rune in input.EnumerateRunes().Append(augh))
        {
            if (rune.Value == '\\' && !escaped)
            {
                escaped = true;
                continue;
            }

            if (rune.Value == '"' && !escaped)
            {
                quoted = !quoted;
                continue;
            }

            if (Rune.IsWhiteSpace(rune) && !quoted)
            {
                // cool clear our buffer.
                var str = buffer.ToString();
                buffer.Clear();
                if (tag is null)
                {
                    tag = str;
                    continue;
                }

                var idx = str.IndexOf('=');
                if (idx != -1 && str[idx-1] != '\\')
                {
                    var prop = str.Remove(idx);
                    var value = str.Remove(0, idx+1);
                    param[prop] = value;
                    continue;
                }

                args.Add(str);
                continue;
            }

            buffer.Append(rune);
        }
    }
}

public interface ITag
{
    public bool TryParseTag(List<string> args, Dictionary<string, string> param, [NotNullWhen(true)] out Control? control, out bool instant);
}
