using System.Text;
using Content.Client.EscapeMenu.UI;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Utility;

namespace Content.Client.Guidebook;

public sealed partial class GuidebookWindow
{
    private void LayoutGuidebook(string inputData, Control initialParent)
    {
        var buffer = new StringBuilder();
        var parentStack = new List<Control> {initialParent};

        var newLine = true;
        var layoutMode = LayoutMode.RichText;
        foreach (var c in inputData)
        {
            if (newLine)
            {
                switch (c)
                {
                    case '?': // Directive.
                    {
                        layoutMode = LayoutMode.Directive;
                        newLine = false;
                        continue;
                    }

                    case '#':
                    {
                        layoutMode = LayoutMode.Header;
                        newLine = false;
                        continue;
                    }

                    /*case var ws when Char.IsWhiteSpace(ws):
                    {
                        continue; // Keep in NL mode.
                    }*/

                    default:
                    {
                        newLine = false;
                        break;
                    }
                }
            }

            if (c == '\n')
            {
                switch (layoutMode)
                {
                    case LayoutMode.RichText:
                    {
                        var rt = new RichTextLabel()
                        {
                            HorizontalExpand = false
                        };
                        var msg = new FormattedMessage();
                        // THANK YOU RICHTEXT VERY COOL
                        msg.PushColor(Color.White);
                        msg.AddMarkup(buffer.ToString());
                        msg.Pop();
                        rt.SetMessage(msg);
                        parentStack[^1].AddChild(rt);
                        break;
                    }
                    case LayoutMode.Header:
                    {
                        var header = new Label()
                            {Text = buffer.ToString(), StyleClasses = {"LabelHeading"}};
                        parentStack[^1].AddChild(header);
                        break;
                    }
                    case LayoutMode.Directive:
                    {
                        ExecuteDirective(buffer.ToString(), out var ctrl, parentStack);

                        if (ctrl is not null)
                            parentStack[^1].AddChild(ctrl);
                        break;
                    }
                    default:
                        throw new ArgumentOutOfRangeException();
                }
                buffer = new StringBuilder();
                layoutMode = LayoutMode.RichText;
                newLine = true;
                continue;
            }

            buffer.Append(c);
        }
    }

    private void ExecuteDirective(string directive, out Control? control, List<Control> parentStack)
    {
        var args = directive.Split(":");
        switch (args[0].TrimEnd())
        {
            case "{#":
            {
                if (args.Length > 3 || args.Length < 2)
                {
                    Logger.Warning($"`{{#` directive didn't get expected arguments, see {directive}");
                    break;
                }

                parentStack.Add(new GridContainer()
                {
                    HorizontalExpand = true,
                    Columns = int.Parse(args[1]),
                    HorizontalAlignment = args.Length >= 3
                        ? Enum.Parse<HAlignment>(args[2])
                        : HAlignment.Left,
                });

                break;
            }
            case "{|":
            {
                if (args.Length > 3)
                {
                    Logger.Warning($"`{{|` directive didn't get expected arguments, see {directive}");
                    break;
                }

                parentStack.Add(new BoxContainer()
                {
                    HorizontalExpand = true,
                    Orientation = args.Length >= 2
                        ? Enum.Parse<BoxContainer.LayoutOrientation>(args[1])
                        : BoxContainer.LayoutOrientation.Horizontal,
                    HorizontalAlignment = args.Length >= 3
                        ? Enum.Parse<HAlignment>(args[2])
                        : HAlignment.Left,
                });

                break;
            }
            case "|}":
            {
                if (args.Length != 1)
                {
                    Logger.Warning($"`|}}` directive didn't get expected arguments, see {directive}");
                    break;
                }

                if (parentStack.Count == 1)
                {
                    Logger.Warning("Tried to remove the guide container from the parent stack. Do you have an extra `?}`?");
                    break;
                }

                control = parentStack.Pop();
                return;
            }
            case var _ when args[0].StartsWith("embedEntity"):
            {
                if (args.Length != 2 && args.Length != 3 && args.Length != 4)
                {
                    Logger.Warning($"`embedEntity` directive didn't get expected arguments, see: {directive}");
                    break;
                }

                var ent = args[1];
                var scale = args.Length >= 2 ? float.Parse(args[2]) : 1.0f;
                control = new GuideEntityEmbed(ent, args[0].Contains("Caption"), args[0].Contains("Interactive"))
                {
                    HorizontalAlignment = HAlignment.Center,
                    Scale = new Vector2(scale, scale),
                    HorizontalExpand = args.Length >= 4 ? bool.Parse(args[3]) : false,
                };
                return;
            }
            case "controlsButton":
            {
                var button = new Button()
                {
                    Text = Loc.GetString("ui-info-button-controls"),
                };
                button.OnPressed += _ => new OptionsMenu().Open();
                control = button;
                return;
            }
            default:
            {
                Logger.Warning($"Unknown guide directive {directive}");
                break;
            }
        }

        control = null;
    }
}
