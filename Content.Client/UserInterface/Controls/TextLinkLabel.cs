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
    /// Refreshes the label's properties based on the current link target
    /// and the viewer's permission to follow it. <br />Call after construction,
    /// and whenever that permission could have changed.
    /// </summary>
    /// <param name="visible">Whether the label should be shown at all. Defaults to true.</param>
    /// <param name="clickable">Additional override to force the label non-clickable. Defaults to true.</param>
    public void UpdateLabelProperties(SharedChatSystem chatSystem, bool? visible = null, bool? clickable = null)
    {
        visible ??= true;
        Visible = visible.Value;
        clickable ??= true;

        canClickLink = (LinkString != null || (LinkEntity is { } netEntity && chatSystem.CanClickMessageSender(netEntity))) && (bool)clickable;

        MouseFilter = canClickLink ? MouseFilterMode.Stop : MouseFilterMode.Ignore;
        DefaultCursorShape = canClickLink ? CursorShape.Hand : CursorShape.Arrow;

        OnHoverChanged(false);
    }

    private void OnHoverChanged(bool hovering)
    {
        FontColorOverride = (canClickLink, hovering) switch
        {
            (true, true) => Color.LightSkyBlue, // clickable and currently hovered
            _ => LinkColor, // not clickable, or not hovered
        };
    }

    /// <summary>
    /// Delegates click to the nearest ancestor ILinkClickHandler or IEntityLinkClickHandler;
    /// TextLinkLabel has no idea what a click actually does.
    /// </summary>
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
