using Content.Shared.Chat;
using Robust.Client.UserInterface.Controls;
using static Robust.Client.UserInterface.Controls.BaseButton;

namespace Content.Client.UserInterface.Systems.Chat.Controls;

public sealed class ChannelSelectorPopup : Popup
{
    // order in which the channels show up in the channel selector
    public static readonly ChatSelectChannel[] ChannelSelectorOrder =
    {
        ChatSelectChannel.Local,
        ChatSelectChannel.Whisper,
        ChatSelectChannel.Emotes,
        ChatSelectChannel.Radio,
        ChatSelectChannel.LOOC,
        ChatSelectChannel.OOC,
        ChatSelectChannel.Dead,
        ChatSelectChannel.Admin
        // NOTE: Console is not in there and it can never be permanently selected.
        // You can, however, still submit commands as console by prefixing with /.
    };

    private readonly BoxContainer _channelSelectorHBox;
    private readonly Dictionary<ChatSelectChannel, ChannelSelectorItemButton> _selectorStates = new();
    private readonly ChatUIController _chatUIController;

    public event Action<ChatSelectChannel>? Selected;

    public ChannelSelectorPopup()
    {
        _channelSelectorHBox = new BoxContainer
        {
            Orientation = BoxContainer.LayoutOrientation.Horizontal,
            SeparationOverride = 1
        };

        _chatUIController = UserInterfaceManager.GetUIController<ChatUIController>();
        _chatUIController.SelectableChannelsChanged += SetChannels;
        SetChannels(_chatUIController.SelectableChannels);

        AddChild(_channelSelectorHBox);
    }

    public ChatSelectChannel? FirstChannel
    {
        get
        {
            foreach (var selector in _selectorStates.Values)
            {
                if (!selector.IsHidden)
                    return selector.Channel;
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

    private bool IsPreferredAvailable()
    {
        var preferred = _chatUIController.MapLocalIfGhost(_chatUIController.GetPreferredChannel());
        return _selectorStates.TryGetValue(preferred, out var selector) &&
               !selector.IsHidden;
    }

    public void SetChannels(ChatSelectChannel channels)
    {
        var wasPreferredAvailable = IsPreferredAvailable();

        _channelSelectorHBox.RemoveAllChildren();

        foreach (var channel in ChannelSelectorOrder)
        {
            if (!_selectorStates.TryGetValue(channel, out var selector))
            {
                selector = new ChannelSelectorItemButton(channel);
                _selectorStates.Add(channel, selector);
                selector.OnPressed += OnSelectorPressed;
            }

            if ((channels & channel) == 0)
            {
                if (selector.Parent == _channelSelectorHBox)
                {
                    _channelSelectorHBox.RemoveChild(selector);
                }
            }
            else if (selector.IsHidden)
            {
                _channelSelectorHBox.AddChild(selector);
            }
        }

        var isPreferredAvailable = IsPreferredAvailable();
        if (!wasPreferredAvailable && isPreferredAvailable)
        {
            Select(_chatUIController.GetPreferredChannel());
        }
        else if (wasPreferredAvailable && !isPreferredAvailable)
        {
            Select(ChatSelectChannel.OOC);
        }
    }

    private void OnSelectorPressed(ButtonEventArgs args)
    {
        var button = (ChannelSelectorItemButton) args.Button;
        Select(button.Channel);
    }

    private void Select(ChatSelectChannel channel)
    {
        Selected?.Invoke(channel);
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);

        if (!disposing)
            return;

        _chatUIController.SelectableChannelsChanged -= SetChannels;
    }
}
