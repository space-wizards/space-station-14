using System.Linq;
using System.Threading.Channels;
using Content.Shared.Chat;
using Robust.Client.UserInterface.Controls;

namespace Content.Client.UserInterface.Systems.Chat.Controls;

public sealed class ChannelSelectorPopup : Popup
{
    private readonly BoxContainer _channelSelectorHBox;
    private ChatSelectChannel _activeSelector = ChatSelectChannel.Local;
    private ChannelSelectorButton? _selectorButton;
    private readonly List<ChannelSelectorItemButton> _selectorStates = new();
    public ChannelSelectorPopup()
    {
        _channelSelectorHBox = new BoxContainer
        {
            Orientation = BoxContainer.LayoutOrientation.Horizontal,
            SeparationOverride = 1
        };
        //SetupChannels(ChatUIController.ChannelSelectorConfig);
        AddChild(_channelSelectorHBox);
    }

    public ChatSelectChannel? FirstChannel
    {
        get
        {
            foreach (var selectorControl in _selectorStates)
            {
                if (!selectorControl.IsHidden) return selectorControl.Channel;
            }
            return null;
        }
    }

    /*public ChatSelectChannel NextChannel()
    {
        var nextChannel = ChatUIController.GetNextChannelSelector(_activeSelector);
        var index = 0;
        while (_selectorStates[(int)nextChannel].IsHidden && index <= _selectorStates.Count)
        {
            nextChannel =  ChatUIController.GetNextChannelSelector(nextChannel);
            index++;
        }
        _activeSelector = nextChannel;
        return nextChannel;
    }


    private void SetupChannels(ChatUIController.ChannelSelectorSetup[] selectorData)
    {
        _channelSelectorHBox.DisposeAllChildren(); //cleanup old toggles
        _selectorStates.Clear();
        foreach (var channelSelectorData in selectorData)
        {
            var newSelectorButton = new ChannelSelectorItemButton(channelSelectorData);
            _selectorStates.Add(newSelectorButton);
            if (!newSelectorButton.IsHidden)
            {
                _channelSelectorHBox.AddChild(newSelectorButton);
            }
            newSelectorButton.OnPressed += OnSelectorPressed;
        }
    }

    private void OnSelectorPressed(BaseButton.ButtonEventArgs args)
    {
        if (_selectorButton == null) return;
        _selectorButton.SelectedChannel = ((ChannelSelectorItemButton) args.Button).Channel;
    }

    public void HideChannels(params ChatChannel[] channels)
    {
        foreach (var channel in channels)
        {
            if (!ChatUIController.ChannelToSelector.TryGetValue(channel, out var selector)) continue;
            var selectorbutton = _selectorStates[(int)selector];
            if (!selectorbutton.IsHidden)
            {
                _channelSelectorHBox.RemoveChild(selectorbutton);
                if (_activeSelector != selector) continue; // do nothing
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

    */
    //run this only once
    public void SetSelectorButton(ChannelSelectorButton button)
    {
        _selectorButton = button;
    }
    public void ShowChannels(params ChatChannel[] channels)
    {
        foreach (var channel in channels)
        {
            var selectorbutton = _selectorStates[(int)channel];
            if (selectorbutton.IsHidden)
            {
                _channelSelectorHBox.AddChild(selectorbutton);
            }
        }
    }
}
