using System.Diagnostics.CodeAnalysis;
using JetBrains.Annotations;
using Content.Shared.Chat;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Client.UserInterface.RichText;
using Robust.Shared.Input;
using Robust.Shared.Utility;
using Content.Client.UserInterface.ControlExtensions;
using Content.Client.UserInterface.Systems.Chat;
using Robust.Shared.GameObjects.Components.Localization;

namespace Content.Client.UserInterface.RichText;

public readonly record struct TextLinkData(string Link, Color? Color, bool Clickable);

internal enum TextLinkKind
{
    None,
    Entity,
    Plain,
}

[UsedImplicitly]
public sealed partial class TextLinkTag : IMarkupTagHandler
{
    [Dependency] private IEntityManager _entity = default!;
    [Dependency] private IUserInterfaceManager _ui = default!;

    public static Color DefaultLinkColor => Color.CornflowerBlue;

    public string Name => "textlink";

    public const string EntAttributeName = "ent";
    public const string LinkAttributeName = "link";
    public const string ColorAttributeName = "color";
    public const string ColorableAttributeName = "colorable";

    public bool TryCreateControl(MarkupNode node, [NotNullWhen(true)] out Control? control)
    {
        if (!node.Value.TryGetString(out var text))
        {
            control = null;
            return false;
        }

        TextLinkData data;

        switch (GetLinkKind(node))
        {
            case TextLinkKind.Entity:
                if (!TryResolveEntityLink(node, out data))
                {
                    control = null;
                    return false;
                }
                break;

            case TextLinkKind.Plain:
                if (!TryResolvePlainLink(node, out data))
                {
                    control = null;
                    return false;
                }
                break;

            default:
                control = null;
                return false;
        }

        // Explicit color= always wins, regardless of link kind.
        var color = ResolveColorOverride(node) ?? data.Color ?? DefaultLinkColor;

        var label = new Label { Text = text };
        label.FontColorOverride = color;

        if (data.Clickable)
        {
            label.MouseFilter = Control.MouseFilterMode.Stop;
            label.DefaultCursorShape = Control.CursorShape.Hand;
            label.OnMouseEntered += _ => label.FontColorOverride = Color.LightSkyBlue;
            label.OnMouseExited += _ => label.FontColorOverride = color;
            label.OnKeyBindDown += args => OnKeybindDown(args, data.Link, label);
        }

        control = label;
        return true;
    }

    private static TextLinkKind GetLinkKind(MarkupNode node)
    {
        if (node.Attributes.ContainsKey(EntAttributeName))
            return TextLinkKind.Entity;

        if (node.Attributes.ContainsKey(LinkAttributeName))
            return TextLinkKind.Plain;

        return TextLinkKind.None;
    }

    private static Color? ResolveColorOverride(MarkupNode node)
    {
        if (!node.Attributes.TryGetValue(ColorAttributeName, out var colorParam) ||
            !colorParam.TryGetString(out var colorStr))
        {
            return null;
        }

        return Color.TryFromHex(colorStr, out var color) ? color : null;
    }

    private void OnKeybindDown(GUIBoundKeyEventArgs args, string link, Control? control)
    {
        if (args.Function != EngineKeyFunctions.UIClick)
            return;

        if (control == null)
            return;

        if (control.TryGetParentHandler<ILinkClickHandler>(out var handler))
            handler.HandleClick(link);
        else
            Logger.Warning("Warning! No valid ILinkClickHandler found.");
    }
}

public interface ILinkClickHandler
{
    public void HandleClick(string link);
}
