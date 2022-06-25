using Content.Client.Stylesheets;
using Content.Shared.Chat;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;

namespace Content.Client.UserInterface.Systems.Chat.Controls;
[Friend(typeof(FilterButton))]
public sealed class ChannelFilterPopup : Popup
{
    private readonly PanelContainer _filterPopupPanel;
    private readonly BoxContainer _filterVBox;

    private static readonly Dictionary<ChatChannel, ChannelFilterCheckbox> FilterStates = new();

    public ChannelFilterPopup()
    {
        _filterPopupPanel = new PanelContainer
        {
            StyleClasses = {StyleNano.StyleClassBorderedWindowPanel},
            Children =
            {
                new BoxContainer
                {
                    Orientation = BoxContainer.LayoutOrientation.Horizontal,
                    Children =
                    {
                        new Control {MinSize = (4, 0)},
                        (_filterVBox = new BoxContainer
                        {
                            MinWidth = 110,
                            Margin = new Thickness(0, 10),
                            Orientation = BoxContainer.LayoutOrientation.Vertical,
                            SeparationOverride = 4
                        })
                    }
                }
            }
        };
        SetupChannels(ChatUIController.ChannelAttributes);
        AddChild(_filterPopupPanel);
    }

    private void SetupChannels(ChatUIController.ChannelData[] channelAttributes)
    {
        _filterVBox.DisposeAllChildren(); //cleanup old toggles
        foreach (var channelAttribute in channelAttributes)
        {
            var newFilter = new ChannelFilterCheckbox(
                channelAttribute.Channel,
                null,
                channelAttribute.ShowUnread,
                channelAttribute.Hidden);
            newFilter.Pressed = channelAttribute.Enabled;
            FilterStates.Add(channelAttribute.Channel, newFilter);
            if (!newFilter.IsHidden) _filterVBox.AddChild(newFilter);
        }
    }

    public void HideChannels(params ChatChannel[] channels)
    {
        foreach (var channel in channels)
        {
            var filter = FilterStates[channel];
            if (!filter.IsHidden)
            {
                _filterVBox.RemoveChild(filter);
            }
        }
    }

    public void ShowChannels(params ChatChannel[] channels)
    {
        foreach (var channel in channels)
        {
            var filter = FilterStates[channel];
            if (filter.IsHidden)
            {
                _filterVBox.AddChild(filter);
            }
        }
    }

}
