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
// <summary> resolved LinkData</summary>
public readonly record struct LinkData(string Link, Color? Color, bool Clickable);

/// <summary>Which attribute a [textlink] node carries, i.e. which resolver handles it.</summary>
internal enum TextLinkKind : byte
{
    None,
    Entity, // entity="<NetEntity>" — clickable chat name
    Plain,  // link="<string>" — always-clickable plain link (e.g. guidebook)
}

/// <summary>
/// Markup tag handler for <c>[textlink="LinkText"]</c> nodes. Renders a link
/// <see cref="Label"/> in rich text, covering two types:
/// plain links (<c>link=</c>) and entity links (<c>entity=</c>).
/// optional <c>color=</c> and <c>entnamecolor=</c> parameters
/// allow setting a color override and opting into using entity name colors for entity links
/// </summary>
[UsedImplicitly]
public sealed partial class TextLinkTag : IMarkupTagHandler
{
    [Dependency] private IEntityManager _entity = default!;
    [Dependency] private IUserInterfaceManager _ui = default!;

    public static Color DefaultLinkColor => Color.CornflowerBlue;

    public string Name => "textlink";

    private const string EntityAttributeName = "entity";
    private const string LinkAttributeName = "link";
    private const string ColorOverrideAttributeName = "color"; // LinkColor override
    private const string UseEntityNameColorAttributeName = "entitynamecolor"; // entity links only: opt into per-entity name coloring

    public bool TryCreateControl(MarkupNode node, [NotNullWhen(true)] out Control? control)
    {
        if (!node.Value.TryGetString(out var text))
        {
            control = null;
            return false;
        }

        LinkData data;

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

        // color= > resolver-supplied color > default
        var linkColor = ResolveColorOverride(node) ?? data.Color ?? DefaultLinkColor;

        var label = new Label { Text = text };
        label.FontColorOverride = linkColor;

        if (data.Clickable)
        {
            label.MouseFilter = Control.MouseFilterMode.Stop;
            label.DefaultCursorShape = Control.CursorShape.Hand;
            label.OnMouseEntered += _ => label.FontColorOverride = Color.LightSkyBlue;
            label.OnMouseExited += _ => label.FontColorOverride = linkColor;
            label.OnKeyBindDown += args => OnKeybindDown(args, data.Link, label);
        }

        control = label;
        return true;
    }

    private static TextLinkKind GetLinkKind(MarkupNode node)
    {
        if (node.Attributes.ContainsKey(EntityAttributeName))
            return TextLinkKind.Entity;

        if (node.Attributes.ContainsKey(LinkAttributeName))
            return TextLinkKind.Plain;

        return TextLinkKind.None;
    }

    private static Color? ResolveColorOverride(MarkupNode node)
    {
        if (!node.Attributes.TryGetValue(ColorOverrideAttributeName, out var colorParam) ||
            !colorParam.TryGetString(out var colorStr))
        {
            return null;
        }

        return Color.TryFromHex(colorStr, out var color) ? color : null;
    }

    // Delegates to the nearest ancestor ILinkClickHandler; this class has no
    // idea what a click actually does.
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

/// <summary>Implement on a control to receive clicks on nested [textlink] nodes.</summary>
public interface ILinkClickHandler
{
    public void HandleClick(string link);
}
