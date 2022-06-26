using Content.Client.Stylesheets;
using Content.Shared.Chat;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;

namespace Content.Client.UserInterface.Systems.Chat.Controls;
public sealed class ChannelFilterPopup : Popup
{
    private readonly PanelContainer _filterPopupPanel;
    private readonly BoxContainer _filterVBox;
    private readonly Dictionary<ChatChannel, ChannelFilterCheckbox> _filterStates = new();

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
        SetupChannels(ChatUIController.ChannelFilterSetup);
        AddChild(_filterPopupPanel);
    }

    private void SetupChannels(ChatUIController.ChannelFilterData[] defaultChannelData)
    {
        _filterStates.Clear();
        _filterVBox.DisposeAllChildren(); //cleanup old toggles
        foreach (var channelData in defaultChannelData)
        {
            var newFilter = new ChannelFilterCheckbox(channelData);
            newFilter.Pressed = channelData.InitialState;
            _filterStates.Add(channelData.Channel, newFilter);
            if (!newFilter.IsHidden) _filterVBox.AddChild(newFilter);
        }
    }
    public void HideChannels(params ChatChannel[] channels)
    {
        foreach (var channel in channels)
        {
            var filter = _filterStates[channel];
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
            var filter = _filterStates[channel];
            if (filter.IsHidden)
            {
                _filterVBox.AddChild(filter);
            }
        }
    }

}
