using System.Numerics;
using Content.Client.Resources;
using Robust.Client.ResourceManagement;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Input;

namespace Content.Client.UserInterface.Systems.Chat.Controls;

public sealed class ChannelFilterButton : ContainerButton
{
    private static readonly Color ColorNormal = Color.FromHex("#7b7e9e");
    private static readonly Color ColorHovered = Color.FromHex("#9699bb");
    private static readonly Color ColorPressed = Color.FromHex("#789B8C");
    private readonly TextureRect? _textureRect;
    public readonly ChannelFilterPopup ChatFilterPopup;
    private readonly ChatUIController _chatUIController;
    private const int FilterDropdownOffset = 120;

    public ChannelFilterButton()
    {
        _chatUIController = UserInterfaceManager.GetUIController<ChatUIController>();
        var filterTexture = IoCManager.Resolve<IResourceCache>()
            .GetTexture("/Textures/Interface/Nano/filter.svg.96dpi.png");

        // needed for same reason as ChannelSelectorButton
        Mode = ActionMode.Press;
        EnableAllKeybinds = true;

        AddChild(
            (_textureRect = new TextureRect
            {
                Texture = filterTexture,
                HorizontalAlignment = HAlignment.Center,
                VerticalAlignment = VAlignment.Center
            })
        );
        ToggleMode = true;
        OnToggled += OnFilterButtonToggled;
        ChatFilterPopup = UserInterfaceManager.CreatePopup<ChannelFilterPopup>();
        ChatFilterPopup.OnVisibilityChanged += PopupVisibilityChanged;

        _chatUIController.FilterableChannelsChanged += ChatFilterPopup.SetChannels;
        _chatUIController.UnreadMessageCountsUpdated += ChatFilterPopup.UpdateUnread;
        ChatFilterPopup.SetChannels(_chatUIController.FilterableChannels);
    }

    private void PopupVisibilityChanged(Control control)
    {
        Pressed = control.Visible;
    }

    private void OnFilterButtonToggled(ButtonToggledEventArgs args)
    {
        if (args.Pressed)
        {
            var globalPos = GlobalPosition;
            var (minX, minY) = ChatFilterPopup.MinSize;
            var box = UIBox2.FromDimensions(globalPos - new Vector2(FilterDropdownOffset, 0),
                new Vector2(Math.Max(minX, ChatFilterPopup.MinWidth), minY));
            ChatFilterPopup.Open(box);
        }
        else
        {
            ChatFilterPopup.Close();
        }
    }

    protected override void KeyBindDown(GUIBoundKeyEventArgs args)
    {
        // needed since we need EnableAllKeybinds - don't double-send both UI click and Use
        if (args.Function == EngineKeyFunctions.Use) return;
        base.KeyBindDown(args);
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

        _chatUIController.FilterableChannelsChanged -= ChatFilterPopup.SetChannels;
        _chatUIController.UnreadMessageCountsUpdated -= ChatFilterPopup.UpdateUnread;
    }
}
