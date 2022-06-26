using System.Linq;
using Content.Shared.Chat;
using Robust.Client.UserInterface.Controls;

namespace Content.Client.UserInterface.Systems.Chat.Controls;

public sealed class ChannelSelectorPopup : Popup
{
    private readonly BoxContainer _channelSelectorHBox;
    private ChatSelectChannel _activeSelector = ChatSelectChannel.Local;
    private readonly Dictionary<ChatSelectChannel, ChannelSelectorItemButton> _selectorStates = new();

    public ChannelSelectorPopup()
    {
        _channelSelectorHBox = new BoxContainer
        {
            Orientation = BoxContainer.LayoutOrientation.Horizontal,
            SeparationOverride = 1
        };
        SetupChannels(ChatUIController.ChannelSelectorSetup);
        AddChild(_channelSelectorHBox);
    }

    private void SetupChannels(ChatUIController.ChannelSelectorData[] selectorData)
    {
        _channelSelectorHBox.DisposeAllChildren(); //cleanup old toggles
        _selectorStates.Clear();
        foreach (var channelSelectorData in selectorData)
        {
            var newSelectorButton = new ChannelSelectorItemButton(channelSelectorData);
            _selectorStates.Add(channelSelectorData.Selector, newSelectorButton);
            if (!newSelectorButton.IsHidden)
            {
                _channelSelectorHBox.AddChild(newSelectorButton);
            }
        }
    }

    public void HideChannels(params ChatSelectChannel[] channelSelectors)
    {
        foreach (var channel in channelSelectors)
        {
            var selector = _selectorStates[channel];
            if (!selector.IsHidden)
            {
                _channelSelectorHBox.RemoveChild(selector);
                if (_activeSelector != channel) continue; // do nothing
                if (_channelSelectorHBox.Children.First() is ChannelSelectorItemButton button)
                {
                    _activeSelector = button.Channel;
                }
                else
                {
                    _activeSelector = ChatSelectChannel.None;
                }
            }
        }
    }

    public void ShowChannels(params ChatSelectChannel[] channelSelectors)
    {
        foreach (var channel in channelSelectors)
        {
            var selector = _selectorStates[channel];
            if (selector.IsHidden)
            {
                _channelSelectorHBox.AddChild(selector);
            }
        }
    }
}
