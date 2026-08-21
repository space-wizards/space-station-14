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
/// optional <c>color=</c> and <c>entitynamecolor=</c> parameters
/// allow setting a color override and opting into using entity name colors for entity links
/// </summary>
[UsedImplicitly]
public sealed partial class TextLinkTag : IMarkupTagHandler
{
    [Dependency] private IEntityManager _entity = default!;
    [Dependency] private IUserInterfaceManager _ui = default!;

    public string Name => "textlink";
    public static Color DefaultLinkColor => Color.CornflowerBlue;

    private const string EntityAttributeName = "entity";
    private const string LinkAttributeName = "link";
    private const string ColorOverrideAttributeName = "color"; // DefaultLinkColor override
    private const string UseEntityNameColorAttributeName = "entitynamecolor"; // entity links only: opt into per-entity name coloring

    private delegate bool TryResolveLink(MarkupNode node, out LinkData data);
    private readonly (string AttributeName, TryResolveLink Resolver)[] _resolvers; // for parsing link to correct resolver
    /// <summary>
    /// Resolved Link Data, LinkString and LinkEntity should not be populated at the same time
    /// </summary>
    private readonly record struct LinkData(string? LinkString, NetEntity? LinkEntity, Color? Color, bool Clickable);

    public TextLinkTag()
    {
        _resolvers =
        [
            (EntityAttributeName, TryResolveEntityLink),
            (LinkAttributeName, TryResolvePlainLink),
        ];
    }

    /// <summary>
    ///  Takes a TextLink <see cref="MarkupNode"/>, parses it and creates a TextLink <see cref="Label"/>.
    /// Fails if it cannot parse link content, or if the resolver cannot create valid <see cref="LinkData"/>.
    /// </summary>
    public bool TryCreateControl(MarkupNode node, [NotNullWhen(true)] out Control? control)
    {

        control = null;
        LinkData linkData = default;

        var linkTypeResolved = false;

        if (!node.Value.TryGetString(out var text))
        {
            return false;
        }

        foreach (var (attrname, resolver) in _resolvers)
        {
            if (node.Attributes.ContainsKey(attrname))
            {
                if(!resolver(node, out linkData))
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
        var linkColor = ResolveColorOverride(node) ?? linkData.Color ?? DefaultLinkColor;

        var linkLabel = new TextLinkLabel() { Text = text, LinkString = linkData.LinkString, LinkEntity = linkData.LinkEntity };
        linkLabel.FontColorOverride = linkColor;

        if (linkData.Clickable)
        {
            linkLabel.MouseFilter = Control.MouseFilterMode.Stop;
            linkLabel.DefaultCursorShape = Control.CursorShape.Hand;
            linkLabel.OnMouseEntered += _ => linkLabel.FontColorOverride = Color.LightSkyBlue;
            linkLabel.OnMouseExited += _ => linkLabel.FontColorOverride = linkColor;
            linkLabel.OnKeyBindDown += args => OnKeybindDown(args, linkLabel);
        }

        control = linkLabel;
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
    /// Delegates to the nearest ancestor ILinkClickHandler or IEntityLinkClickHandler;
    /// TextLinkTag has no idea what a click actually does.
    /// </summary>
    private void OnKeybindDown(GUIBoundKeyEventArgs args, TextLinkLabel? control)
    {
        if (args.Function != EngineKeyFunctions.UIClick)
            return;

        if (control == null)
            return;

        if (control.LinkEntity is { } entity && control.TryGetParentHandler<IEntityLinkClickHandler>(out var entityLinkClickHandler))
        {
                entityLinkClickHandler.HandleClick(entity);
        }
        else if (control.LinkString != null && control.TryGetParentHandler<ILinkClickHandler>(out var linkClickHandler))
        {
            linkClickHandler.HandleClick(control.LinkString);
        }
    }
}

/// <summary>
/// Implement on a control to receive clicks on nested [textlink link=] nodes.
/// </summary>
public interface ILinkClickHandler
{
    public void HandleClick(string link);
}

/// <summary>
/// Implement on a control to receive clicks on nested [textlink entity=] nodes.
/// </summary>
public interface IEntityLinkClickHandler
{
    public void HandleClick(NetEntity netEntity);
}
