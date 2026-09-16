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
/// only one field per link should be populated at a time.
/// </summary>
public sealed partial class TextLinkLabel : Label
{
    public string? LinkString { get; init; }
    public NetEntity? LinkEntity { get; init; }

    [Dependency] private IEntityManager _entity = default!;

    private SharedChatSystem? _chat;

    public TextLinkLabel()
    {
        IoCManager.InjectDependencies(this);
        if (LinkEntity is not null)
        {
            UpdateEntityTextLinkLabelProperties();
        }
    }

    /// <summary>
    /// Checks if link is clickable by player and updates properties to reflect that.
    /// </summary>
    /// <param name="visible"> - optional: use to set the visibility of the label</param>
    public void UpdateEntityTextLinkLabelProperties(bool? visible = null)
    {
        _chat ??= _entity.System<SharedChatSystem>();
        var canClickLink = LinkEntity is { } netEntity && _chat.CanClickMessageSender(netEntity);
        var linkColor = FontColorOverride.GetValueOrDefault();
        if (canClickLink)
        {
            MouseFilter = MouseFilterMode.Stop;
            DefaultCursorShape = CursorShape.Hand;
            OnMouseEntered += _ => FontColorOverride = Color.LightSkyBlue;
            OnMouseExited += _ => FontColorOverride = linkColor;
            OnKeyBindDown += OnKeybindDown;
        }
        else
        {
            MouseFilter = MouseFilterMode.Ignore;
            DefaultCursorShape = CursorShape.Arrow;
            OnMouseEntered += _ => FontColorOverride = linkColor;
            OnMouseExited += _ => FontColorOverride = linkColor;
        }

        visible ??= true;
        Visible = visible.Value;
    }

    private void OnKeybindDown(GUIBoundKeyEventArgs args)
    {
        if (args.Function != EngineKeyFunctions.UIClick)
            return;

        if (LinkEntity is { } entity &&
            this.TryGetParentHandler<IEntityLinkClickHandler>(out var entityLinkClickHandler))
        {
            entityLinkClickHandler.HandleClick(entity);
        }
    }
}
