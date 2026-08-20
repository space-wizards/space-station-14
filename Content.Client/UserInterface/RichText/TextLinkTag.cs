using System.Diagnostics.CodeAnalysis;
using JetBrains.Annotations;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Client.UserInterface.RichText;
using Robust.Shared.Input;
using Robust.Shared.Utility;
using Content.Client.UserInterface.ControlExtensions;
using Content.Client.UserInterface.Controls;

namespace Content.Client.UserInterface.RichText;


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

    private const string EntityAttributeName = "entity";
    private const string LinkAttributeName = "link";
    private const string ColorOverrideAttributeName = "color"; // LinkColor override
    private const string UseEntityNameColorAttributeName = "entitynamecolor"; // entity links only: opt into per-entity name coloring

    private delegate bool TryResolveLink(MarkupNode node, out LinkData data);
    private readonly (string AttributeName, TryResolveLink Resolver)[] _resolvers; //lookup table for parsing link to correct solver

    private readonly record struct LinkData(string Link, Color? Color, bool Clickable);

    public TextLinkTag()
    {
        _resolvers =
        [
            (EntityAttributeName, TryResolveEntityLink),
            (LinkAttributeName, TryResolvePlainLink),
        ];
    }

    public string Name => "textlink";
    public static Color DefaultLinkColor => Color.CornflowerBlue;

    public bool TryCreateControl(MarkupNode node, [NotNullWhen(true)] out Control? control)
    {

        control = null;
        LinkData data = default;

        var linkTypeResolved = false;

        if (!node.Value.TryGetString(out var text))
        {
            return false;
        }

        foreach (var (attrname, resolver) in _resolvers)
        {
            if (node.Attributes.ContainsKey(attrname))
            {
                if(!resolver(node, out data))
                {
                    return false;
                }
                linkTypeResolved = true;
                break;
            }
        }
        if (!linkTypeResolved)
        {
            return false;
        }

        // color= > resolver-supplied color > default
        var linkColor = ResolveColorOverride(node) ?? data.Color ?? DefaultLinkColor;

        var link = new TextLink() { Text = text };
        link.FontColorOverride = linkColor;

        if (data.Clickable)
        {
            link.MouseFilter = Control.MouseFilterMode.Stop;
            link.DefaultCursorShape = Control.CursorShape.Hand;
            link.OnMouseEntered += _ => link.FontColorOverride = Color.LightSkyBlue;
            link.OnMouseExited += _ => link.FontColorOverride = linkColor;
            link.OnKeyBindDown += args => OnKeybindDown(args, data.Link, link);
        }

        control = link;
        return true;
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

    /// <summary>
    /// Delegates to the nearest ancestor ILinkClickHandler; this TextLinkTag has no
    /// idea what a click actually does.
    /// </summary>
    private void OnKeybindDown(GUIBoundKeyEventArgs args, string link, Control? control)
    {
        if (args.Function != EngineKeyFunctions.UIClick)
            return;

        if (control == null)
            return;

        if (control.TryGetParentHandler<ILinkClickHandler>(out var handler))
        {
            handler.HandleClick(link);
        }
    }
}

/// <summary>
/// Implement on a control to receive clicks on nested [textlink] nodes.
/// </summary>
public interface ILinkClickHandler
{
    public void HandleClick(string link);
}
