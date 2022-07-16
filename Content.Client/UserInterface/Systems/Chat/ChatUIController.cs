using System.Linq;
using Content.Shared.Chat;
using Robust.Client.UserInterface;

namespace Content.Client.UserInterface.Systems.Chat;
public sealed class ChatUIController : UIController
{
    private readonly List<ChatChannel> _selectableChannels = new ();
    private readonly List<ChatChannel> _filterableChannels = new ();
    private Action<ChatChannel[]>? _onChannelsAdd = null;
    private Action<ChatChannel[]>? _onChannelsRemove = null;


    public ChatSelectChannel GetNextSelectableChannel(ChatSelectChannel selector)
    {
        var index = _selectableChannels.IndexOf((ChatChannel)selector);
        if (index == -1 || index == _selectableChannels.Count-1) return (ChatSelectChannel)_selectableChannels.First();
        return (ChatSelectChannel) _selectableChannels[index + 1];
    }

    public void RegisterOnChannelsAdd(Action<ChatChannel[]> action)
    {
        if (_onChannelsAdd == null)
        {
            _onChannelsAdd = action;
            return;
        }
        _onChannelsAdd += action;
    }

    public void RegisterOnChannelsRemove(Action<ChatChannel[]> action)
    {
        if (_onChannelsRemove == null)
        {
            _onChannelsRemove = action;
            return;
        }
        _onChannelsRemove += action;
    }

    public void RemoveOnChannelsAdd(Action<ChatChannel[]> action)
    {
        if (_onChannelsRemove == null) return;
        _onChannelsRemove -= action;
    }

    public void RemoveOnChannelsRemove(Action<ChatChannel[]> action)
    {
        if (_onChannelsRemove == null) return;
        _onChannelsRemove -= action;
    }

    public void EnableChannels(params ChatChannel[] channels)
    {
        foreach (var channel in channels)
        {
            if (!ChatChannelSettings.Config.TryGetValue(channel, out var configData))
                throw new Exception("No config for specified channel");
            if (configData.CanBeFiltered) _filterableChannels.Add(channel);
            if (configData.CanBeSelected) _selectableChannels.Add(channel);
        }
        _onChannelsAdd?.Invoke(channels);
    }

    public void DisableChannels(params ChatChannel[] channels)
    {
        foreach (var channel in channels)
        {
            if (!ChatChannelSettings.Config.TryGetValue(channel, out var configData))
                throw new Exception("No config for specified channel");
            if (configData.CanBeFiltered) _filterableChannels.Remove(channel);
            if (configData.CanBeSelected) _selectableChannels.Remove(channel);
        }

        _onChannelsRemove?.Invoke(channels);
    }

    public static string GetChannelSelectorName(ChatSelectChannel channelSelector)
    {
        return channelSelector.ToString();
    }

    public static char GetChannelSelectorPrefix(ChatSelectChannel channelSelector)
    {
        return (char)(ChatPrefixes)channelSelector;
    }
}
