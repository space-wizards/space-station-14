using System.Numerics;
using Content.Client.Resources;
using Robust.Client.ResourceManagement;
using Robust.Client.UserInterface.Controls;

namespace Content.Client.UserInterface.Systems.Chat.Controls;

public sealed class ChannelFilterButton : ChatPopupButton<ChannelFilterPopup>
{
    private static readonly Color ColorNormal = Color.FromHex("#7b7e9e");
    private static readonly Color ColorHovered = Color.FromHex("#9699bb");
    private static readonly Color ColorPressed = Color.FromHex("#789B8C");
    private readonly TextureRect? _textureRect;
    private readonly ChatUIController _chatUIController;

    private const int FilterDropdownOffset = 120;

    public ChannelFilterButton()
    {
        _chatUIController = UserInterfaceManager.GetUIController<ChatUIController>();
        var filterTexture = IoCManager.Resolve<IResourceCache>()
            .GetTexture("/Textures/Interface/Nano/filter.svg.96dpi.png");

        AddChild(
            (_textureRect = new TextureRect
            {
                Texture = filterTexture,
                HorizontalAlignment = HAlignment.Center,
                VerticalAlignment = VAlignment.Center
            })
        );

        _chatUIController.FilterableChannelsChanged += Popup.SetChannels;
        _chatUIController.UnreadMessageCountsUpdated += Popup.UpdateUnread;
        Popup.SetChannels(_chatUIController.FilterableChannels);
    }

    protected override UIBox2 GetPopupPosition()
    {
        var globalPos = GlobalPosition;
        var (minX, minY) = Popup.MinSize;
        return UIBox2.FromDimensions(
            globalPos - new Vector2(FilterDropdownOffset, 0),
            new Vector2(Math.Max(minX, Popup.MinWidth), minY));
    }

    private void UpdateChildColors()
    {
        if (_textureRect == null) return;
        switch (DrawMode)
        {
            case DrawModeEnum.Normal:
                _textureRect.ModulateSelfOverride = ColorNormal;
                break;

            case DrawModeEnum.Pressed:
                _textureRect.ModulateSelfOverride = ColorPressed;
                break;

            case DrawModeEnum.Hover:
                _textureRect.ModulateSelfOverride = ColorHovered;
                break;

            case DrawModeEnum.Disabled:
                break;
        }
    }

    protected override void DrawModeChanged()
    {
        base.DrawModeChanged();
        UpdateChildColors();
    }

    protected override void StylePropertiesChanged()
    {
        base.StylePropertiesChanged();
        UpdateChildColors();
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);

        if (!disposing)
            return;

        _chatUIController.FilterableChannelsChanged -= Popup.SetChannels;
        _chatUIController.UnreadMessageCountsUpdated -= Popup.UpdateUnread;
    }
}
