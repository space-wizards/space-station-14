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

/// <summary>Resolved link data from a per-kind resolver.</summary>
public readonly record struct TextLinkData(string Link, Color? Color, bool Clickable);

/// <summary>Which attribute a [textlink] node carries, i.e. which resolver handles it.</summary>
internal enum TextLinkKind
{
    None,
    Entity, // ent="<NetEntity>" — clickable chat name
    Plain,  // link="<string>" — always-clickable plain link (e.g. guidebook)
}

/// <summary>
/// Covers plain links and clickable chat entity names, resolves via link= and ent=
/// </summary>
[UsedImplicitly]
public sealed partial class TextLinkTag : IMarkupTagHandler
{
    [Dependency] private IEntityManager _entity = default!;
    [Dependency] private IUserInterfaceManager _ui = default!;

    public static Color DefaultLinkColor => Color.CornflowerBlue;

    public string Name => "textlink";

    private const string EntAttributeName = "ent";
    private const string LinkAttributeName = "link";
    private const string ColorOverrideAttributeName = "color"; //
    private const string ColorableAttributeName = "colorable"; // entity links only: opt into per-entity name coloring

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

        // color= > resolver-supplied color > default
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
