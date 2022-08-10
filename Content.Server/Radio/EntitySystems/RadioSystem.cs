using System.Linq;
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
    [Dependency] private readonly UserInterfaceSystem _userInterfaceSystem = default!;

    // thats right, linking is based on NETWORK not manualy doing it anymore :D
    private readonly Dictionary<string, List<EntityUid>> _tcommsMachine = new();


    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<TelecommsMachine, ComponentInit>(OnTelecommsMachineInit);
        SubscribeLocalEvent<TelecommsMachine, ComponentShutdown>(OnTelecommsMachineDelete);
        SubscribeLocalEvent<IRadio, MapInitEvent>(OnRadioInit);
        SubscribeLocalEvent<HandheldRadioComponent, RadioToggleTX>(OnRadioToggleTx);
        SubscribeLocalEvent<HandheldRadioComponent, RadioToggleRX>(OnRadioToggleRx);
        SubscribeLocalEvent<HandheldRadioComponent, RadioChangeFrequency>(OnFrequencyChange);
    }

    private void OnFrequencyChange(EntityUid uid, HandheldRadioComponent component, RadioChangeFrequency args)
    {
        component.Frequency = _sharedRadioSystem.SanitizeFrequency(args.Frequency);
        UpdateUIState(uid, component);
    }

    private void OnRadioInit(EntityUid uid, IRadio component, MapInitEvent args)
    {
        UpdateUIState(uid, component);
    }

    private void OnRadioToggleTx(EntityUid uid, HandheldRadioComponent component, RadioToggleTX args)
    {
        component.Send = !component.Send;
        UpdateUIState(uid, component);
    }

    private void OnRadioToggleRx(EntityUid uid, HandheldRadioComponent component, RadioToggleRX args)
    {
        component.Receive = !component.Receive;
        UpdateUIState(uid, component);
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

        //SendToAllMachines<TelecommsReceiverComponent>(packet);
        SendToAllMachines<TelecommsAllInOneComponent>(packet);

        // set_interval(callback(SpreadMessageLocal, packet), 2 SECONDS)
        Timer.Spawn(TimeSpan.FromSeconds(2), () =>
        {
            if (packet.IsFinished) return;
            // handhelds exclusively
            BroadcastMessage(packet, true);

            packet.IsFinished = true;
        });
    }

    private void BroadcastMessage(MessagePacket packet, bool isLocal = false)
    {
        if (packet.IsFinished || packet.Channel == null)
            return;
        var channelint = (int) packet.Channel;
        var radio = new HashSet<IRadio>();
        if (isLocal)
        {
            foreach (var radioComponent in EntityManager.EntityQuery<HandheldRadioComponent>(true))
            {
                if (!radioComponent.Receive) continue;
                radio.Add(radioComponent);
            }
            radio.UnionWith(EntityManager.EntityQuery<IntrinsicRadioComponent>(true));
        }
        else
        {
            // TODO unfuck this holy shit it looks bad
            var hc = GetEntityQuery<HeadsetComponent>();
            var hr = GetEntityQuery<HandheldRadioComponent>();
            var rk = GetEntityQuery<RadioKeyComponent>();
            var ghostRadio = GetEntityQuery<IntrinsicRadioComponent>();
            var outsideFreeFreq = _sharedRadioSystem.IsOutsideFreeFreq(channelint);
            foreach (var iRadio in EntityManager.EntityQuery<IRadio>(true))
            {
                if (ghostRadio.HasComponent(iRadio.Owner))
                {
                    // bypass
                    radio.Add(iRadio);
                    continue;
                }

                // dont bother transmitting if the receiving end has no radiokey & it's outside free freq.
                if (!outsideFreeFreq)
                {
                    // radios always listen (atleast i think)
                    if (hc.TryGetComponent(iRadio.Owner, out var hccomponent) && hccomponent.Frequency != packet.Channel)
                    {
                        continue;
                    }
                    if (hr.TryGetComponent(iRadio.Owner, out var hrcomponent) &&
                        (!hrcomponent.Receive || hrcomponent.Frequency != packet.Channel))
                    {
                        continue;
                    }
                }
                else if (!rk.HasComponent(iRadio.Owner))
                {
                    continue;
                }
                else
                {
                    // do the radiokey processing
                    if (rk.TryGetComponent(iRadio.Owner, out var radioKeyComponent)
                        && (!radioKeyComponent.UnlockedFrequency.Contains(channelint)
                            || radioKeyComponent.BlockedFrequency.Contains(channelint)))
                        continue;
                }

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
    private void GetMobsInRadioRange(HashSet<IRadio> radioIn, HashSet<ICommonSession> clientsOut)
    {
        var ghostRadio = GetEntityQuery<IntrinsicRadioComponent>();
        var headset = GetEntityQuery<HeadsetComponent>();
        var xforms = GetEntityQuery<TransformComponent>();

        var zLevelWeCare = new HashSet<MapId>();
        var radioXFormsCache = new Dictionary<EntityUid, TransformComponent>();

        foreach (var radio in radioIn)
        {
            // direct send. no expensive process
            var owner = radio.Owner;

            // snowflake for headsets/ghost headsets. pass owners directly
            if (ghostRadio.HasComponent(owner) || headset.HasComponent(owner))
            {
                if (!owner.TryGetContainer(out var container))
                    continue;
                if (!_entMan.TryGetComponent(container.Owner, out ActorComponent? actor))
                    continue;
                clientsOut.Add(actor.PlayerSession);
                continue;
            }
            radioXFormsCache.Add(owner, xforms.GetComponent(owner));
            zLevelWeCare.Add(radioXFormsCache.GetValueOrDefault(owner, xforms.GetComponent(owner)).MapID);
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
                    // TODO this
                    var range = 8f;
                    // ignore not the same id
                    var owner = radio.Owner;
                    if (radioXFormsCache.GetValueOrDefault(owner, xforms.GetComponent(owner)).MapID != mapId)
                        continue;
                    //xform.WorldPosition - position.Position).Length < range,
                    if ((playerPos - radioXFormsCache.GetValueOrDefault(owner, xforms.GetComponent(owner)).WorldPosition).Length > range)
                    {
                        continue;
                    }

                    clientsOut.Add(player);
                }
            }
        }
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);
        TelecommsMachineUpdate(frameTime);
    }

    public void UpdateUIState(EntityUid uid,
        IRadio? radio = null,
        RadioKeyComponent? key = null,
        ServerUserInterfaceComponent? ui = null)
    {
        // we dont want ghost radios
        if (!Resolve(uid, ref radio, ref ui, logMissing: false))
            return;

        // radio key is optional
        key ??= CompOrNull<RadioKeyComponent>(uid);

        if (_userInterfaceSystem.GetUiOrNull(uid, RadioUiKey.Key, ui) is not { } bui) return;
        // bump type
        var TX = false;
        var RX = false;
        var freq = 1459;
        if (TryComp<HandheldRadioComponent>(uid, out var hrc))
        {
            TX = hrc.Send;
            RX = hrc.Receive;
            freq = hrc.Frequency;
        }
        if (TryComp<HeadsetComponent>(uid, out var hc))
        {
            // always on
            TX = true;
            RX = true;
            freq = hc.Frequency;
        }

        if (key != null)
        {
            // radio facts: you can only filter radios which you CAN send
            var dict = new Dictionary<int, string>();
            foreach (var freqint in key.UnlockedFrequency)
            {
                var freqProto = _sharedRadioSystem.GetChannel(freqint);
                dict.Add(freqint, freqProto?.Name ?? _sharedRadioSystem.StringifyFrequency(freqint));
            }
            bui.SetState(new RadioBoundInterfaceState(TX, RX, freq, dict, key.BlockedFrequency));
            return;
        }
        bui.SetState(new RadioBoundInterfaceState(TX, RX, freq, new Dictionary<int, string>(), new HashSet<int>()));
    }
}

/// <summary>
/// Used for routing through simulated telecomms. Also stops duped sends
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
