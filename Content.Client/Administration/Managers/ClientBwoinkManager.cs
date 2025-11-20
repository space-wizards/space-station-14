using System.Linq;
using Content.Shared.Administration.Managers.Bwoink;
using Content.Shared.Administration.Managers.Bwoink.Features;
using Robust.Client.Audio;
using Robust.Client.ResourceManagement;
using Robust.Shared.Audio.Sources;
using Robust.Shared.ContentPack;
using Robust.Shared.Network;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.Manager;
using Robust.Shared.Serialization.Markdown;
using Robust.Shared.Serialization.Markdown.Mapping;
using Robust.Shared.Timing;
using Robust.Shared.Utility;

namespace Content.Client.Administration.Managers;

/// <summary>
/// Handles the client side APIs for bwoinking.
/// </summary>
/// <seealso cref="SharedBwoinkManager"/>
public sealed partial class ClientBwoinkManager : SharedBwoinkManager
{
    public const string CustomActionRowPrefix = "bwoink_row_";

    [Dependency] private readonly INetManager _netManager = default!;
    [Dependency] private readonly ISharedPlayerManager _playerManager = default!;
    [Dependency] private readonly IResourceCache _res = default!;
    [Dependency] private readonly IAudioManager _audio = default!;
    [Dependency] private readonly IClientAdminManager _adminManager = default!;
    [Dependency] private readonly IGameTiming _gameTiming = default!;
    [Dependency] private readonly IResourceManager _resourceManager = default!;
    [Dependency] private readonly ISerializationManager _serializationManager = default!;

    /// <summary>
    /// Our fancy cache of player channels. This keeps track of unread messages and when we last received a message.
    /// </summary>
    [ViewVariables]
    public readonly Dictionary<ProtoId<BwoinkChannelPrototype>, Dictionary<NetUserId, PlayerChannelProperties>>
        PlayerChannels = new();

    /// <summary>
    /// Contains the ratelimits for each userchannel we send a typing status to.
    /// </summary>
    private readonly Dictionary<NetUserId, TimeSpan> _typingRateLimits = new();

    /// <summary>
    /// Dictionary that contains the sounds to play for a specified channel, source may be null.
    /// </summary>
    public readonly Dictionary<ProtoId<BwoinkChannelPrototype>, IAudioSource?> CachedSounds = new();

    /// <summary>
    /// Dictionary that contains the user defined action rows for a given channel.
    /// </summary>
    public readonly Dictionary<ProtoId<BwoinkChannelPrototype>, ActionRow> CustomChannelActionRows = new();

    /// <summary>
    /// Called whenever we receive a <see cref="MsgBwoinkTypings"/>
    /// </summary>
    public event Action? TypingsUpdated;

    public override void Initialize()
    {
        base.Initialize();
        _netManager.RegisterNetMessage<MsgBwoinkNonAdmin>(BwoinkAttempted);
        _netManager.RegisterNetMessage<MsgBwoink>(AdminBwoinkAttempted);
        _netManager.RegisterNetMessage<MsgBwoinkSyncRequest>();
        _netManager.RegisterNetMessage<MsgBwoinkSync>(SyncBwoinks);
        _netManager.RegisterNetMessage<MsgBwoinkTypingUpdate>();
        _netManager.RegisterNetMessage<MsgBwoinkTypings>(SyncTypings);
        _netManager.RegisterNetMessage<MsgBwoinkSyncChannelsRequest>();
        _netManager.RegisterNetMessage<MsgBwoinkSyncChannels>(SyncChannels);

        _adminManager.AdminStatusUpdated += StatusUpdated;
    }

    private void SyncTypings(MsgBwoinkTypings message)
    {
        TypingStatuses[message.Channel] = message.Typings;
        TypingsUpdated?.Invoke();
    }

    private void StatusUpdated()
    {
        RequestChannels();
        RequestSync();
    }

    /// <summary>
    /// Gets the player channel properties for a given channel and user channel. If they don't already exist, a new one will be created and returned.
    /// </summary>
    public PlayerChannelProperties GetOrCreatePlayerPropertiesForChannel(ProtoId<BwoinkChannelPrototype> channel, NetUserId userId)
    {
        PlayerChannels.TryAdd(channel, new Dictionary<NetUserId, PlayerChannelProperties>());

        if (PlayerChannels[channel].TryGetValue(userId, out var value))
            return value;

        PlayerChannels[channel].Add(userId, new PlayerChannelProperties());
        return PlayerChannels[channel][userId];
    }

    protected override void UpdatedChannels()
    {
        CustomChannelActionRows.Clear();
        foreach (var (key, channel) in ProtoCache)
        {
            foreach (var feature in channel.Features)
            {
                switch (feature)
                {
                    case SoundOnMessage soundOnMessage:
                        UpdateSoundOnMessage(key, soundOnMessage);
                        break;
                    case AllowClientActionRow:
                        UpdateCustomActionRow(key);
                        break;

                    default:
                        continue;
                }
            }
        }
        return;

        void UpdateSoundOnMessage(ProtoId<BwoinkChannelPrototype> key, SoundOnMessage soundOnMessage)
        {
            if (CachedSounds.TryGetValue(key, out var cachedSound))
                cachedSound?.Dispose();

            var sound = _audio.CreateAudioSource(_res.GetResource<AudioResource>(soundOnMessage.Sound.Path));
            if (sound != null)
                sound.Global = true;

            CachedSounds[key] = sound;
        }

        void UpdateCustomActionRow(ProtoId<BwoinkChannelPrototype> key)
        {
            var path = new ResPath($"/{CustomActionRowPrefix}{key.Id}.yml");
            Log.Info($"Checking: {path} for custom action buttons!");
            if (!_resourceManager.UserData.Exists(path))
                return;

            using var stream = _resourceManager.UserData.OpenText(path);
            try
            {
                var documents = DataNodeParser.ParseYamlStream(stream).First();
                var mapping = documents.Root;
                var buttons = _serializationManager.Read<ActionButton[]>(mapping, notNullableOverride: true);
                CustomChannelActionRows.Add(key, new ActionRow()
                {
                    Buttons = buttons.ToList(),
                });
                Log.Info("Loaded custom action buttons!");
            }
            catch (Exception e)
            {
                Log.Error($"Failed loading custom action buttons! {e}");
            }
        }
    }

    private void SyncBwoinks(MsgBwoinkSync message)
    {
        Log.Info($"Received full state! {message.Conversations.Count} channels with {message.Conversations.Values.Select(x => x.Count).Count()} conversations.");
        Conversations = message.Conversations;
        InvokeReloadedData();
    }

    private void AdminBwoinkAttempted(MsgBwoink message)
    {
        InvokeMessageReceived(message.Channel, message.Target, message.Message);
    }

    private void BwoinkAttempted(MsgBwoinkNonAdmin message)
    {
        // This one is targeted to us, so we use our local session as the target.
        // ReSharper disable once NullableWarningSuppressionIsUsed
        // "The user Id of the local player. This will be null on the server.".
        // Null suppression because we will only ever receive this while being connected. If it is null, something has gone wrong.
        InvokeMessageReceived(message.Channel, _playerManager.LocalUser!.Value, message.Message);
    }

    /// <summary>
    /// Attempts to send a message to a channel as a non-admin.
    /// </summary>
    public void SendMessageNonAdmin(ProtoId<BwoinkChannelPrototype> channel, string text)
    {
        var gameTickerNonsense = GetRoundIdAndTime();

        _netManager.ClientSendMessage(new MsgBwoinkNonAdmin()
        {
            // We can leave all of this null since the server will set all of this anyways.
            Message = new BwoinkMessage(string.Empty, null, DateTime.UtcNow, text, MessageFlags.None, gameTickerNonsense.roundTime, gameTickerNonsense.roundId),
            Channel = channel,
        });
    }

    /// <summary>
    /// Attempts to send a message *as* an admin. If called when you are not a manager of the channel, this does nothing but explode you (server rejects the packet).
    /// </summary>
    public void SendMessageAdmin(BwoinkChannelPrototype channel, NetUserId user, string text, MessageFlags flags)
    {
        var gameTickerNonsense = GetRoundIdAndTime();

        _netManager.ClientSendMessage(new MsgBwoink()
        {
            // We can leave all of this null since the server will set all of this anyways.
            Message = new BwoinkMessage(string.Empty, null, DateTime.UtcNow, text, flags, gameTickerNonsense.roundTime, gameTickerNonsense.roundId),
            Channel = channel,
            Target = user,
        });
    }

    /// <summary>
    /// Updates your typing status for the client, this results in a <see cref="MsgBwoinkTypingUpdate"/> message.
    /// </summary>
    /// <remarks>
    /// This method has an internal ratelimit of 8 seconds. This ratelimit only applies for setting the typing state to true.
    /// </remarks>
    public void SetTypingStatus(ProtoId<BwoinkChannelPrototype> channel, bool typing, NetUserId? userChannel)
    {
        const int rateLimit = 3;

        userChannel ??= _playerManager.LocalUser!.Value;
        // Check ratelimit
        if (_typingRateLimits.TryGetValue(userChannel.Value, out var limit))
        {
            if (typing && _gameTiming.RealTime < limit)
                return;

            if (!typing)
            {
                // We are sending a "stopped typing" message. So: Remove current rate limit key so the next "i am typing" message can get through.
                _typingRateLimits.Remove(userChannel.Value);
            }
            else
            {
                _typingRateLimits[userChannel.Value] = _gameTiming.RealTime + TimeSpan.FromSeconds(rateLimit);
            }
        }
        else
        {
            _typingRateLimits.Add(userChannel.Value, _gameTiming.RealTime + TimeSpan.FromSeconds(rateLimit));
        }

        _netManager.ClientSendMessage(new MsgBwoinkTypingUpdate()
        {
            IsTyping = typing,
            Channel = channel,
            ChannelUserId = userChannel.Value,
        });
    }

    /// <summary>
    /// Requests a full re-sync of all conversations we have. There is no locking so calling this while conversations are on-going may result in dropped or duplicated messages.
    /// </summary>
    public void RequestSync()
    {
        // TODO: Maybe locking???
        Log.Info("Resetting Bwoink state!");
        _netManager.ClientSendMessage(new MsgBwoinkSyncRequest());
    }
}

/// <summary>
/// Properties to keep track of when we last received a message on a channel.
/// </summary>
public sealed class PlayerChannelProperties
{
    public DateTime LastMessage { get; set; } = DateTime.MinValue;
    public int Unread { get; set; } = 0;
}
