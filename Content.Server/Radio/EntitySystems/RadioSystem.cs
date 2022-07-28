using System.Linq;
using System.Threading.Channels;
using Content.Server.Chat.Managers;
using Content.Server.Ghost.Components;
using Content.Server.Headset;
using Content.Server.Radio.Components;
using Content.Server.Radio.Components.Telecomms;
using Content.Server.RadioKey.Components;
using Content.Shared.Chat;
using Content.Shared.Radio;
using JetBrains.Annotations;
using Robust.Server.GameObjects;
using Robust.Shared.Containers;
using Robust.Shared.Map;
using Robust.Shared.Player;
using Robust.Shared.Players;
using Robust.Shared.Timing;

namespace Content.Server.Radio.EntitySystems;

[UsedImplicitly]
public sealed partial class RadioSystem : EntitySystem
{
    [Dependency] private readonly SharedRadioSystem _sharedRadioSystem = default!;
    [Dependency] private readonly EntityManager _entMan = default!;
    [Dependency] private readonly IChatManager _chatManager = default!;
    [Dependency] private readonly TransformSystem _transformSystem = default!;

    // thats right, linking is based on NETWORK not manualy doing it anymore :D
    private readonly Dictionary<string, List<EntityUid>> _tcommsMachine = new();


    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<TelecommsMachine, ComponentInit>(OnTelecommsMachineInit);
        SubscribeLocalEvent<TelecommsMachine, ComponentShutdown>(OnTelecommsMachineDelete);
    }

    /*
    Roughly speaking, radios attempt to make a subspace transmission (which
    is received, processed, and rebroadcast by the telecomms satellite) and
    if that fails, they send a mundane radio transmission.

    Headsets cannot send/receive mundane transmissions, only subspace.
    Syndicate radios can hear transmissions on all well-known frequencies.
    CentCom radios can hear the CentCom frequency no matter what.
    */
    public void SpreadMessage(MessagePacket packet)
    {
        if (packet.Channel == null)
        {
            return; // this is bad
        }

        var freq = (int)packet.Channel;
        // ok you have the key. Check if its a private network one (CC, CTF, Syndie)
        var chan = _sharedRadioSystem.GetChannel(freq);
        if (chan is { SkipTelecomms: true })
        {
            packet.IsFinished = true;
            BroadcastMessage(packet);
            return; // not intentional currently lazy.
        }

        SpreadMessageSubspace(packet);

        // set_interval(callback(SpreadMessageLocal, packet), 2 SECONDS)
        Timer.Spawn(TimeSpan.FromSeconds(2), () =>
        {
            SpreadMessageLocal(packet);
        });
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);
        TelecommsMachineUpdate(frameTime);
    }

    private void SpreadMessageSubspace(MessagePacket packet)
    {
        //SendToAllMachines<TelecommsReceiverComponent>(packet);
        SendToAllMachines<TelecommsAllInOneComponent>(packet);
    }

    /// <summary>
    /// Backup transmission but for handheld radios. Headsets & any radios sends this
    /// </summary>
    /// <param name="packet"></param>
    private void SpreadMessageLocal(MessagePacket packet)
    {
        if (packet.IsFinished) return;
        // handhelds exclusively
        BroadcastMessage(packet, true);

        packet.IsFinished = true;
    }

    private void BroadcastMessage(MessagePacket packet, bool isLocal = false)
    {
        if (packet.Channel == null) return;
        var channelint = (int) packet.Channel;
        var radio = new List<IRadio>();
        if (isLocal)
        {
            foreach (var radioComponent in EntityManager.EntityQuery<HandheldRadioComponent>(true))
            {
                if (!radioComponent.RXOn) continue;
                radio.Add(radioComponent);
            }
        }
        else
        {
            // TODO unfuck this holy shit it looks bad
            var hc = GetEntityQuery<HeadsetComponent>();
            var hr = GetEntityQuery<HandheldRadioComponent>();
            var rk = GetEntityQuery<RadioKeyComponent>();
            foreach (var iRadio in EntityManager.EntityQuery<IRadio>(true))
            {
                // if radio key exists, check it else check if its frequency is == to broadcasitng freq
                if (rk.TryGetComponent(iRadio.Owner, out var radioKeyComponent) && !radioKeyComponent.UnlockedFrequency.Contains(channelint)) continue;
                // radios always listen (atleast i think)
                if (hc.TryGetComponent(iRadio.Owner, out var hccomponent) && hccomponent.Frequency != packet.Channel)
                {
                    continue;
                }
                if (hr.TryGetComponent(iRadio.Owner, out var hrcomponent) && hrcomponent.Frequency != packet.Channel)
                {
                    continue;
                }
                // ghost radios get resolved down the line
                radio.Add(iRadio);
            }
        }

        // From the list of radios, find all mobs who can hear those.
        var sessions = new HashSet<ICommonSession>();

        GetMobsInRadioRange(radio, sessions);

        var channel = _sharedRadioSystem.GetChannel(channelint);
        var chanfreq = _sharedRadioSystem.StringifyFrequency(channelint);

        _chatManager.ChatMessageToMany(
            ChatChannel.Radio,
            packet.Message,
            Loc.GetString(
            "chat-radio-message-wrap",
            ("color", channel?.Color ?? Color.FromHex("#1ecc43")), ("channel", $"\\[{chanfreq}\\]"), ("name", _entMan.GetComponent<MetaDataComponent>(packet.Speaker).EntityName)),
            packet.Speaker,
            false,
            sessions.Select(s => s.ConnectedClient).ToList());
    }

    /// <summary>
    /// Same-ish implementation as how ss13 handles dedupe for radios <br/>
    /// Get all radios (special for ghosts, handled by the radioIn) -> Get all mobs within radio range (hashset so no dupes)
    /// then send the message
    /// </summary>
    /// <param name="radioIn"></param>
    /// <param name="clientsOut"></param>
    private void GetMobsInRadioRange(List<IRadio> radioIn, HashSet<ICommonSession> clientsOut)
    {
        var ghostRadio = GetEntityQuery<GhostRadioComponent>();
        var headset = GetEntityQuery<HeadsetComponent>();
        var xforms = GetEntityQuery<TransformComponent>();

        var zLevelWeCare = new HashSet<MapId>();

        foreach (var radio in radioIn)
        {
            // direct send. no expensive process
            var owner = radio.Owner;

            // snowflake for headsets/ghost headsets. pass owners directly
            if (ghostRadio.HasComponent(owner) || headset.HasComponent(owner))
            {
                if (!owner.TryGetContainer(out var container)) continue;
                if (!_entMan.TryGetComponent(container.Owner, out ActorComponent? actor)) continue;
                clientsOut.Add(actor.PlayerSession);
                continue;
            }

            zLevelWeCare.Add(_transformSystem.GetMapId(owner));
        }

        foreach (var mapId in zLevelWeCare)
        {
            foreach (var player in Filter.Empty().AddInMap(mapId).Recipients)
            {
                if (player.AttachedEntity is not {Valid: true} playerEntity)
                    continue;

                var playerPos = _transformSystem.GetWorldPosition(playerEntity, xforms);
                foreach (var radio in radioIn)
                {
                    var range = 8f;
                    // ignore not the same id
                    if (_transformSystem.GetMapId(radio.Owner) != mapId) continue;
                    //xform.WorldPosition - position.Position).Length < range,
                    if ((playerPos - _transformSystem.GetWorldPosition(radio.Owner, xforms)).Length > range)
                    {
                        continue;
                    }

                    clientsOut.Add(player);
                }
            }
        }
    }
}

/// <summary>
/// Used for logging in telecomms machine and also used for routing through simulated telecomms
/// </summary>
public sealed class MessagePacket
{
    public EntityUid Speaker = default!;
    public string Message = default!;
    public int? Channel;

    private bool _isFinished = false;

    /// <summary>
    /// Did the message get sent through the network completely? Stops dupes.
    /// </summary>
    public bool IsFinished
    {
        get => _isFinished;
        set => _isFinished = true;
    }
}
