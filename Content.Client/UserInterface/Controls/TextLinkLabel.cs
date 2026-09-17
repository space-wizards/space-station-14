using Content.Client.UserInterface.ControlExtensions;
using Content.Client.UserInterface.RichText;
using Content.Shared.Chat;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Input;
using Robust.Shared.Utility;

namespace Content.Client.UserInterface.Controls;

/// <summary>
/// Carries link data parsed and resolved from a TextLink <see cref="MarkupNode"/>,
/// only one LinkType field per link should be populated at a time.
/// </summary>
public sealed partial class TextLinkLabel : Label
{
    public string? LinkString { get; init; } // default links
    public NetEntity? LinkEntity { get; init; } // entity links
    public Color LinkColor { get; set; }

    [Dependency] private IEntityManager _entity = default!;

    private SharedChatSystem? _chat;
    private bool canClickLink;

    public TextLinkLabel()
    {
        IoCManager.InjectDependencies(this);
        OnKeyBindDown += OnKeybindDown;
        OnMouseEntered += _ => OnHoverChanged(true);
        OnMouseExited += _ => OnHoverChanged(false);
    }

    /// <summary>
    /// Refreshes this label's clickability, cursor/mouse behavior, visibility, and rendered
    /// color based on the current link target and the viewer's permission to follow it.
    /// Call after construction, and again whenever that permission could have changed
    /// (e.g. the viewer's attached entity changes).
    /// </summary>
    /// <param name="visible">Whether the label should be shown at all. Defaults to true.</param>
    /// <param name="clickable">Additional override on top of the underlying permission check —
    /// pass false to force the label non-clickable even when the link would otherwise be
    /// clickable. Defaults to true.</param>
    public void UpdateLabelProperties(bool? visible = null, bool? clickable = null)
    {
        visible ??= true;
        Visible = visible.Value;
        clickable ??= true;

        _chat ??= _entity.System<SharedChatSystem>();

        canClickLink = (LinkString != null || (LinkEntity is { } netEntity && _chat.CanClickMessageSender(netEntity))) && (bool)clickable;

        MouseFilter = canClickLink ? MouseFilterMode.Stop : MouseFilterMode.Ignore;
        DefaultCursorShape = canClickLink ? CursorShape.Hand : CursorShape.Arrow;

        OnHoverChanged(false);
    }

    private void OnHoverChanged(bool hovering)
    {
        FontColorOverride = (canClickLink, hovering) switch
        {
            (true, true) => Color.LightSkyBlue, // clickable and currently hovered
            _ => LinkColor,                                         // not clickable, or not hovered
        };
    }

    private void OnKeybindDown(GUIBoundKeyEventArgs args)
    {
        if (args.Function != EngineKeyFunctions.UIClick)
            return;

        if (LinkString is null && LinkEntity is null)
            return;

        if (LinkEntity is { } entity &&
            this.TryGetParentHandler<IEntityLinkClickHandler>(out var entityLinkClickHandler))
        {
            entityLinkClickHandler.HandleClick(entity);
        }
        else if (LinkString != null && this.TryGetParentHandler<ILinkClickHandler>(out var linkClickHandler))
        {
            linkClickHandler.HandleClick(LinkString);
        }
    }
}
