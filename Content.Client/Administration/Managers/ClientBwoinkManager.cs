using System.Linq;
using Content.Shared.Administration.Managers;
using Content.Shared.Administration.Managers.Bwoink;
using Content.Shared.Administration.Managers.Bwoink.Features;
using Robust.Client.Audio;
using Robust.Client.ResourceManagement;
using Robust.Shared.Audio.Sources;
using Robust.Shared.Network;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;

namespace Content.Client.Administration.Managers;

public sealed class ClientBwoinkManager : SharedBwoinkManager
{
    [Dependency] private readonly INetManager _netManager = default!;
    [Dependency] private readonly ISharedPlayerManager _playerManager = default!;
    [Dependency] private readonly IResourceCache _res = default!;
    [Dependency] private readonly IAudioManager _audio = default!;
    [Dependency] private readonly IClientAdminManager _adminManager = default!;

    /// <summary>
    /// Dictionary that contains the sounds to play for a specified channel, source may be null.
    /// </summary>
    public readonly Dictionary<ProtoId<BwoinkChannelPrototype>, IAudioSource?> CachedSounds = new();

    /// <summary>
    /// Called whenever our prototypes change, or a full state update is applied.
    /// </summary>
    public event Action? ReloadedData;

    public override void Initialize()
    {
        base.Initialize();
        _netManager.RegisterNetMessage<MsgBwoinkNonAdmin>(BwoinkAttempted);
        _netManager.RegisterNetMessage<MsgBwoink>(AdminBwoinkAttempted);
        _netManager.RegisterNetMessage<MsgBwoinkSyncRequest>();
        _netManager.RegisterNetMessage<MsgBwoinkSync>(SyncBwoinks);

        _adminManager.AdminStatusUpdated += StatusUpdated;
    }

    private void StatusUpdated()
    {
        RequestSync();
    }

    protected override void UpdatedChannels()
    {
        foreach (var (key, channel) in ProtoCache)
        {
            foreach (var feature in channel.Features)
            {
                if (feature is not SoundOnMessage soundOnMessage)
                    continue;

                if (CachedSounds.TryGetValue(key, out var cachedSound))
                    cachedSound?.Dispose();

                var sound = _audio.CreateAudioSource(_res.GetResource<AudioResource>(soundOnMessage.Sound.Path));
                if (sound != null)
                    sound.Global = true;

                CachedSounds[key] = sound;
                break;
            }
        }

        ReloadedData?.Invoke();
    }

    private void SyncBwoinks(MsgBwoinkSync message)
    {
        Log.Info($"Received full state! {message.Conversations.Count} channels with {message.Conversations.Values.Select(x => x.Count).Count()} conversations.");
        Conversations = message.Conversations;
        ReloadedData?.Invoke();
    }

    private void AdminBwoinkAttempted(MsgBwoink message)
    {
        InvokeMessageReceived(message.Channel,
            message.Target,
            message.Message.Content,
            message.Message.SenderId,
            message.Message.Sender,
            message.Message.Flags);
    }

    private void BwoinkAttempted(MsgBwoinkNonAdmin message)
    {
        // This one is targeted to us, so we use our local session as the target.
        // ReSharper disable once NullableWarningSuppressionIsUsed
        // "The user Id of the local player. This will be null on the server.".
        // Null suppression because we will only ever receive this while being connected. If it is null, something has gone wrong.
        InvokeMessageReceived(message.Channel,
            _playerManager.LocalUser!.Value,
            message.Message.Content,
            message.Message.SenderId,
            message.Message.Sender,
            message.Message.Flags);
    }

    public void SendMessageNonAdmin(ProtoId<BwoinkChannelPrototype> channel, string text)
    {
        _netManager.ClientSendMessage(new MsgBwoinkNonAdmin()
        {
            // We can leave all of this null since the server will set all of this anyways.
            Message = new BwoinkMessage(string.Empty, null, DateTime.UtcNow, text, MessageFlags.None),
            Channel = channel,
        });
    }

    public void SendMessageAdmin(BwoinkChannelPrototype channel, NetUserId user, string text)
    {
        _netManager.ClientSendMessage(new MsgBwoink()
        {
            // We can leave all of this null since the server will set all of this anyways.
            Message = new BwoinkMessage(string.Empty, null, DateTime.UtcNow, text, MessageFlags.None),
            Channel = channel,
            Target = user,
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
