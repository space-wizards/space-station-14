using Content.Server.Interaction;
using Content.Server.Power.EntitySystems;
using Content.Server.Telephone;
using Content.Shared.Audio;
using Content.Shared.Chat.TypingIndicator;
using Content.Shared.Holopad;
using Content.Shared.Inventory;
using Content.Shared.Labels.Components;
using Content.Shared.Silicons.StationAi;
using Content.Shared.Telephone;
using Content.Shared.UserInterface;
using Robust.Server.GameObjects;
using Robust.Shared.Containers;
using Robust.Shared.Timing;
using Robust.Shared.Utility;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Content.Server.Holopad;

public sealed class HolopadSystem : SharedHolopadSystem
{
    [Dependency] private readonly TelephoneSystem _telephoneSystem = default!;
    [Dependency] private readonly UserInterfaceSystem _userInterfaceSystem = default!;
    [Dependency] private readonly TransformSystem _xformSystem = default!;
    [Dependency] private readonly AppearanceSystem _appearanceSystem = default!;
    [Dependency] private readonly SharedPointLightSystem _pointLightSystem = default!;
    [Dependency] private readonly SharedAmbientSoundSystem _ambientSoundSystem = default!;
    [Dependency] private readonly InventorySystem _inventorySystem = default!;
    [Dependency] private readonly InteractionSystem _interactionSystem = default!;
    [Dependency] private readonly EyeSystem _eyeSystem = default!;
    [Dependency] private readonly SharedStationAiSystem _stationAiSystem = default!;
    [Dependency] private readonly SharedContainerSystem _containerSystem = default!;
    [Dependency] private readonly IGameTiming _timing = default!;

    private float _updateTimer = 1.0f;

    private const float UpdateTime = 1.0f;
    private const float MinTimeBetweenSyncRequests = 0.5f;
    private TimeSpan _minTimeSpanBetweenSyncRequests;

    private HashSet<EntityUid> _pendingRequestsForSpriteState = new();
    private HashSet<EntityUid> _recentlyUpdatedHolograms = new();

    public override void Initialize()
    {
        base.Initialize();

        _minTimeSpanBetweenSyncRequests = TimeSpan.FromSeconds(MinTimeBetweenSyncRequests);

        // Holopad UI and bound user interface messages
        SubscribeLocalEvent<HolopadComponent, BeforeActivatableUIOpenEvent>(OnUIOpen);
        SubscribeLocalEvent<HolopadComponent, HolopadStartNewCallMessage>(OnHolopadStartNewCall);
        SubscribeLocalEvent<HolopadComponent, HolopadAnswerCallMessage>(OnHolopadAnswerCall);
        SubscribeLocalEvent<HolopadComponent, HolopadEndCallMessage>(OnHolopadEndCall);
        SubscribeLocalEvent<HolopadComponent, HolopadActivateProjectorMessage>(OnHolopadActivateProjector);
        SubscribeLocalEvent<HolopadComponent, HolopadStartBroadcastMessage>(OnHolopadStartBroadcast);
        SubscribeLocalEvent<HolopadComponent, HolopadStationAiRequestMessage>(OnHolopadStationAiRequest);

        // Holopad telephone events
        SubscribeLocalEvent<HolopadComponent, TelephoneStateChangeEvent>(OnTelephoneStateChange);
        SubscribeLocalEvent<HolopadComponent, TelephoneCallCommencedEvent>(OnHoloCallCommenced);
        SubscribeLocalEvent<HolopadComponent, TelephoneCallEndedEvent>(OnHoloCallEnded);
        SubscribeLocalEvent<HolopadComponent, TelephoneMessageSentEvent>(OnTelephoneMessageSent);

        // Networked events
        SubscribeNetworkEvent<HolopadUserTypingChangedEvent>(OnTypingChanged);
        SubscribeNetworkEvent<PlayerSpriteStateMessage>(OnPlayerSpriteStateMessage);

        // Component start/shutdown events
        SubscribeLocalEvent<HolopadComponent, ComponentInit>(OnComponentInit);
        SubscribeLocalEvent<HolopadComponent, ComponentShutdown>(OnComponentShutdown);
        SubscribeLocalEvent<HolopadUserComponent, ComponentInit>(OnComponentInit);
    }

    #region: Holopad UI bound user interface messages

    private void OnUIOpen(Entity<HolopadComponent> entity, ref BeforeActivatableUIOpenEvent args)
    {
        UpdateUIState(entity);
    }

    private void OnHolopadStartNewCall(Entity<HolopadComponent> source, ref HolopadStartNewCallMessage args)
    {
        if (IsHolopadControlLocked(source, args.Actor))
            return;

        if (!TryComp<TelephoneComponent>(source, out var sourceTelephone))
            return;

        var receiver = GetEntity(args.Receiver);

        if (!TryComp<TelephoneComponent>(receiver, out var receiverTelephone))
            return;

        LinkHolopadToUser(source, args.Actor);
        _telephoneSystem.CallTelephone((source, sourceTelephone), (receiver, receiverTelephone), args.Actor);
    }

    private void OnHolopadAnswerCall(Entity<HolopadComponent> receiver, ref HolopadAnswerCallMessage args)
    {
        if (IsHolopadControlLocked(receiver, args.Actor))
            return;

        if (!TryComp<TelephoneComponent>(receiver, out var receiverTelephone))
            return;

        if (TryComp<StationAiHeldComponent>(args.Actor, out var userAiHeld))
        {
            var source = GetLinkedHolopads(receiver).FirstOrNull();

            if (source != null)
                ActivateProjector(source.Value, args.Actor);

            return;
        }

        LinkHolopadToUser(receiver, args.Actor);
        _telephoneSystem.AnswerTelephone((receiver, receiverTelephone), args.Actor);
    }

    private void OnHolopadEndCall(Entity<HolopadComponent> entity, ref HolopadEndCallMessage args)
    {
        if (!TryComp<TelephoneComponent>(entity, out var entityTelephone))
            return;

        if (IsHolopadControlLocked(entity, args.Actor))
            return;

        _telephoneSystem.EndTelephoneCalls((entity, entityTelephone));
    }

    private void OnHolopadActivateProjector(Entity<HolopadComponent> entity, ref HolopadActivateProjectorMessage args)
    {
        ActivateProjector(entity, args.Actor);
    }

    private void OnHolopadStartBroadcast(Entity<HolopadComponent> source, ref HolopadStartBroadcastMessage args)
    {
        if (IsHolopadControlLocked(source, args.Actor) || IsHolopadBroadcastOnCoolDown(source))
            return;

        // If the broadcasting player is the AI, activate the projector
        // and switch the holopad source to the station AI core
        if (TryComp<StationAiHeldComponent>(args.Actor, out var stationAiHeld))
        {
            if (!_stationAiSystem.TryGetStationAiCore((args.Actor, stationAiHeld), out var stationAiCore) ||
                !TryComp<HolopadComponent>(stationAiCore, out var stationAiCoreHolopad))
                return;

            source = new Entity<HolopadComponent>(stationAiCore.Value, stationAiCoreHolopad);
        }

        if (!TryComp<TelephoneComponent>(source, out var sourceTelephone))
            return;

        var sourceTelephoneEntity = new Entity<TelephoneComponent>(source, sourceTelephone);

        if (_telephoneSystem.IsTelephoneEngaged(sourceTelephoneEntity))
            return;

        // Find all holopads in range of the source
        var sourceXform = Transform(source);
        var receivers = new HashSet<Entity<TelephoneComponent>>();

        var query = AllEntityQuery<HolopadComponent, TelephoneComponent, TransformComponent>();
        while (query.MoveNext(out var receiver, out var receiverHolopad, out var receiverTelephone, out var receiverXform))
        {
            var receiverTelephoneEntity = new Entity<TelephoneComponent>(receiver, receiverTelephone);

            if (sourceTelephoneEntity == receiverTelephoneEntity)
                continue;

            if (!_telephoneSystem.IsSourceAbleToReachReceiver(sourceTelephoneEntity, receiverTelephoneEntity))
                continue;

            if (receiverTelephone.UnlistedNumber)
                continue;

            // If any holopads in range are on broadcast cooldown, exit
            if (IsHolopadBroadcastOnCoolDown((receiver, receiverHolopad)))
                return;

            receivers.Add(receiverTelephoneEntity);
        }

        _telephoneSystem.BroadcastCallToTelephones(sourceTelephoneEntity, receivers, args.Actor, true);

        if (!_telephoneSystem.IsTelephoneEngaged(sourceTelephoneEntity))
            return;

        LinkHolopadToUser(source, args.Actor);

        // Lock out the controls of all involved holopads for a set duration
        source.Comp.ControlLockoutInitiator = args.Actor;
        source.Comp.ControlLockoutStartTime = _timing.CurTime;

        Dirty(source);

        foreach (var receiver in GetLinkedHolopads(source))
        {
            receiver.Comp.ControlLockoutInitiator = args.Actor;
            receiver.Comp.ControlLockoutStartTime = _timing.CurTime;

            Dirty(receiver);
        }
    }

    private void OnHolopadStationAiRequest(Entity<HolopadComponent> entity, ref HolopadStationAiRequestMessage args)
    {
        if (IsHolopadControlLocked(entity, args.Actor))
            return;

        if (!TryComp<TelephoneComponent>(entity, out var telephone))
            return;

        var source = new Entity<TelephoneComponent>(entity, telephone);

        var query = AllEntityQuery<StationAiCoreComponent, TelephoneComponent>();
        while (query.MoveNext(out var receiverUid, out var receiverStationAiCore, out var receiverTelephone))
        {
            var receiver = new Entity<TelephoneComponent>(receiverUid, receiverTelephone);

            if (!_telephoneSystem.IsSourceAbleToReachReceiver(source, receiver))
                continue;

            _telephoneSystem.CallTelephone(source, receiver, args.Actor);

            if (!_telephoneSystem.IsSourceConnectedToReceiver(source, receiver))
                continue;

            if (!_stationAiSystem.TryGetInsertedAI((receiver, receiverStationAiCore), out var insertedAi))
                continue;

            LinkHolopadToUser(entity, args.Actor);

            if (_userInterfaceSystem.TryOpenUi(receiverUid, HolopadUiKey.AiRequestWindow, insertedAi.Value.Owner))
            {
                string? callerId = null;

                if (receiverTelephone.CurrentState == TelephoneState.Ringing && receiverTelephone.LastCaller != null)
                    callerId = _telephoneSystem.GetFormattedCallerIdForEntity(receiverTelephone.LastCaller.Value, Color.White, "Default", 11);

                _userInterfaceSystem.SetUiState(receiverUid, HolopadUiKey.AiRequestWindow, new HolopadBoundInterfaceState(new(), callerId));
            }

            break;
        }
    }

    #endregion

    #region: Holopad telephone events

    private void OnTelephoneStateChange(Entity<HolopadComponent> holopad, ref TelephoneStateChangeEvent args)
    {
        // Update holopad visual and ambient states
        switch (args.NewState)
        {
            case TelephoneState.Idle:
                ShutDownHolopad(holopad);
                SetHolopadAmbientState(holopad, false);
                break;

            case TelephoneState.EndingCall:
                ShutDownHolopad(holopad);
                break;

            default:
                SetHolopadAmbientState(holopad, this.IsPowered(holopad, EntityManager));
                break;
        }
    }

    private void OnHoloCallCommenced(Entity<HolopadComponent> source, ref TelephoneCallCommencedEvent args)
    {
        if (source.Comp.Hologram == null)
            GenerateHologram(source);

        // Receiver holopad holograms have to be generated now instead of waiting for their own event
        // to fire because holographic avatars get synced immediately
        if (TryComp<HolopadComponent>(args.Receiver, out var receivingHolopad) && receivingHolopad.Hologram == null)
            GenerateHologram((args.Receiver, receivingHolopad));

        if (source.Comp.User != null)
        {
            var user = source.Comp.User;

            if (TryComp<HolographicAvatarComponent>(user, out var avatar))
                SyncHolopadUserWithLinkedHolograms(user.Value, avatar.LayerData);

            else
                RequestHolopadUserSpriteUpdate(user.Value);
        }
    }

    private void OnHoloCallEnded(Entity<HolopadComponent> entity, ref TelephoneCallEndedEvent args)
    {
        if (!TryComp<StationAiCoreComponent>(entity, out var stationAiCore))
            return;

        if (!_stationAiSystem.TryGetInsertedAI((entity, stationAiCore), out var insertedAi))
            return;

        _userInterfaceSystem.CloseUi(entity.Owner, HolopadUiKey.AiRequestWindow, insertedAi.Value.Owner);
    }

    private void OnTelephoneMessageSent(Entity<HolopadComponent> holopad, ref TelephoneMessageSentEvent args)
    {
        LinkHolopadToUser(holopad, args.MessageSource);
    }

    #endregion

    #region: Networked events

    private void OnTypingChanged(HolopadUserTypingChangedEvent ev, EntitySessionEventArgs args)
    {
        var uid = args.SenderSession.AttachedEntity;

        if (!Exists(uid))
            return;

        if (!TryComp<HolopadUserComponent>(uid, out var holopadUser))
            return;

        foreach (var linkedHolopad in holopadUser.LinkedHolopads)
        {
            var receiverHolopads = GetLinkedHolopads(linkedHolopad);

            foreach (var receiverHolopad in receiverHolopads)
            {
                if (receiverHolopad.Comp.Hologram == null)
                    continue;

                _appearanceSystem.SetData(receiverHolopad.Comp.Hologram.Value.Owner, TypingIndicatorVisuals.IsTyping, ev.IsTyping);
            }
        }
    }

    private void OnPlayerSpriteStateMessage(PlayerSpriteStateMessage ev, EntitySessionEventArgs args)
    {
        var uid = args.SenderSession.AttachedEntity;

        if (!Exists(uid))
            return;

        if (!_pendingRequestsForSpriteState.Remove(uid.Value))
            return;

        if (!TryComp<HolopadUserComponent>(uid, out var holopadUser))
            return;

        SyncHolopadUserWithLinkedHolograms((uid.Value, holopadUser), ev.SpriteLayerData);
    }

    #endregion

    #region: Component start/shutdown events

    private void OnComponentInit(Entity<HolopadComponent> entity, ref ComponentInit args)
    {
        LinkHolopadToUser(entity, entity.Comp.User);
    }

    private void OnComponentInit(Entity<HolopadUserComponent> entity, ref ComponentInit args)
    {
        foreach (var linkedHolopad in entity.Comp.LinkedHolopads)
            LinkHolopadToUser(linkedHolopad, entity);
    }

    private void OnComponentShutdown(Entity<HolopadComponent> entity, ref ComponentShutdown args)
    {
        ShutDownHolopad(entity);
        SetHolopadAmbientState(entity, false);
    }

    #endregion

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        _updateTimer += frameTime;

        if (_updateTimer >= UpdateTime)
        {
            _updateTimer -= UpdateTime;

            var query = AllEntityQuery<HolopadComponent, TelephoneComponent, TransformComponent>();
            while (query.MoveNext(out var uid, out var holopad, out var telephone, out var xform))
            {
                //if (_userInterfaceSystem.IsUiOpen(ent, HolopadUiKey.Key))
                UpdateUIState((uid, holopad), telephone);

                if (holopad.User != null &&
                    !HasComp<IgnoreUIRangeComponent>(holopad.User) &&
                    !_xformSystem.InRange((holopad.User.Value, Transform(holopad.User.Value)), (uid, xform), telephone.ListeningRange))
                {
                    UnlinkHolopadFromUser((uid, holopad), holopad.User.Value);
                }
            }
        }

        _recentlyUpdatedHolograms.Clear();
    }

    public void UpdateUIState(Entity<HolopadComponent> entity, TelephoneComponent? telephone = null)
    {
        if (!Resolve(entity.Owner, ref telephone, false))
            return;

        var source = new Entity<TelephoneComponent>(entity, telephone);
        var holopads = new Dictionary<NetEntity, string>();

        var query = AllEntityQuery<HolopadComponent, TelephoneComponent>();
        while (query.MoveNext(out var receiverUid, out var _, out var receiverTelephone))
        {
            var receiver = new Entity<TelephoneComponent>(receiverUid, receiverTelephone);

            if (receiverTelephone.UnlistedNumber)
                continue;

            if (source == receiver)
                continue;

            if (!_telephoneSystem.IsSourceInRangeOfReceiver(source, receiver))
                continue;

            var name = MetaData(receiverUid).EntityName;

            if (TryComp<LabelComponent>(receiverUid, out var label) && !string.IsNullOrEmpty(label.CurrentLabel))
                name = label.CurrentLabel;

            holopads.Add(GetNetEntity(receiverUid), name);
        }

        string? callerId = null;

        if (telephone.CurrentState == TelephoneState.Ringing && telephone.LastCaller != null)
            callerId = _telephoneSystem.GetFormattedCallerIdForEntity(telephone.LastCaller.Value, Color.White, "Default", 11);

        var uiKey = HasComp<StationAiCoreComponent>(entity) ? HolopadUiKey.AiActionWindow : HolopadUiKey.InteractionWindow;
        _userInterfaceSystem.SetUiState(entity.Owner, uiKey, new HolopadBoundInterfaceState(holopads, callerId));
    }

    private void GenerateHologram(Entity<HolopadComponent> entity)
    {
        if (entity.Comp.Hologram != null ||
            entity.Comp.HologramProtoId == null)
            return;

        var uid = Spawn(entity.Comp.HologramProtoId, Transform(entity).Coordinates);

        // Safeguard - spawned holograms must have this component
        if (!TryComp<HolopadHologramComponent>(uid, out var component))
        {
            Del(uid);
            return;
        }

        entity.Comp.Hologram = new Entity<HolopadHologramComponent>(uid, component);
    }

    private void DeleteHologram(Entity<HolopadHologramComponent> hologram, Entity<HolopadComponent> attachedHolopad)
    {
        attachedHolopad.Comp.Hologram = null;

        QueueDel(hologram);
    }

    private void LinkHolopadToUser(Entity<HolopadComponent> entity, EntityUid? user)
    {
        if (user == null)
            return;

        if (!TryComp<HolopadUserComponent>(user.Value, out var userComp))
            userComp = AddComp<HolopadUserComponent>(user.Value);

        if (user != entity.Comp.User?.Owner)
        {
            // Removes the old user from the holopad
            UnlinkHolopadFromUser(entity, entity.Comp.User);

            // Assigns the new user in their place
            userComp.LinkedHolopads.Add(entity);
            entity.Comp.User = (user.Value, userComp);
        }

        if (!HasComp<HolographicAvatarComponent>(user.Value))
            RequestHolopadUserSpriteUpdate((user.Value, userComp));
    }

    private void UnlinkHolopadFromUser(Entity<HolopadComponent> entity, Entity<HolopadUserComponent>? user)
    {
        if (user == null)
            return;

        entity.Comp.User = null;

        foreach (var linkedHolopad in GetLinkedHolopads(entity))
        {
            if (linkedHolopad.Comp.Hologram != null)
            {
                _appearanceSystem.SetData(linkedHolopad.Comp.Hologram.Value.Owner, TypingIndicatorVisuals.IsTyping, false);

                var ev = new PlayerSpriteStateMessage(GetNetEntity(linkedHolopad.Comp.Hologram.Value), Array.Empty<PrototypeLayerData>());
                RaiseNetworkEvent(ev);
            }
        }

        if (!HasComp<HolopadUserComponent>(user))
            return;

        user.Value.Comp.LinkedHolopads.Remove(entity);

        if (!user.Value.Comp.LinkedHolopads.Any())
        {
            _pendingRequestsForSpriteState.Remove(user.Value);
            RemComp<HolopadUserComponent>(user.Value);
        }
    }

    private void ShutDownHolopad(Entity<HolopadComponent> holopad)
    {
        if (holopad.Comp.Hologram != null)
            DeleteHologram(holopad.Comp.Hologram.Value, holopad);

        if (holopad.Comp.User != null)
            UnlinkHolopadFromUser(holopad, holopad.Comp.User.Value);

        if (TryComp<StationAiCoreComponent>(holopad, out var stationAiCore))
        {
            _stationAiSystem.SwitchRemoteMode((holopad.Owner, stationAiCore), true);

            if (TryComp<TelephoneComponent>(holopad, out var stationAiCoreTelphone))
                _telephoneSystem.EndTelephoneCalls((holopad, stationAiCoreTelphone));
        }
    }

    private void RequestHolopadUserSpriteUpdate(Entity<HolopadUserComponent> user)
    {
        if (!_pendingRequestsForSpriteState.Add(user))
            return;

        var ev = new PlayerSpriteStateRequest(GetNetEntity(user));
        RaiseNetworkEvent(ev);
    }

    private void SyncHolopadUserWithLinkedHolograms(Entity<HolopadUserComponent> entity, PrototypeLayerData[] spriteLayerData)
    {
        foreach (var linkedHolopad in entity.Comp.LinkedHolopads)
        {
            foreach (var receivingHolopad in GetLinkedHolopads(linkedHolopad))
            {
                if (receivingHolopad.Comp.Hologram == null || !_recentlyUpdatedHolograms.Add(receivingHolopad.Comp.Hologram.Value))
                    continue;

                var netHologram = GetNetEntity(receivingHolopad.Comp.Hologram.Value);
                var ev = new PlayerSpriteStateMessage(netHologram, spriteLayerData);
                RaiseNetworkEvent(ev);
            }
        }
    }

    private void ActivateProjector(Entity<HolopadComponent> entity, EntityUid user)
    {
        if (!TryComp<TelephoneComponent>(entity, out var receiverTelephone))
            return;

        var receiver = new Entity<TelephoneComponent>(entity, receiverTelephone);

        if (!TryComp<StationAiHeldComponent>(user, out var userAiHeld))
            return;

        if (!_stationAiSystem.TryGetStationAiCore((user, userAiHeld), out var stationAi) ||
            stationAi.Value.Comp.RemoteEntity == null)
            return;

        if (!TryComp<TelephoneComponent>(stationAi, out var stationAiTelephone))
            return;

        if (!TryComp<HolopadComponent>(stationAi, out var stationAiHolopad))
            return;

        var source = new Entity<TelephoneComponent>(stationAi.Value, stationAiTelephone);

        var callOptions = new TelephoneCallOptions()
        {
            ForceConnect = true,
            MuteReceiver = true
        };

        LinkHolopadToUser((stationAi.Value, stationAiHolopad), user);

        // Terminate any calls that the core is hosting and immediately connect to the receiver
        _telephoneSystem.TerminateTelephoneCalls(source);
        _telephoneSystem.CallTelephone(source, receiver, user, callOptions);

        if (!_telephoneSystem.IsSourceConnectedToReceiver(source, receiver))
            return;

        // Switch the AI's perspective from free roaming to the target holopad
        _xformSystem.SetCoordinates(stationAi.Value.Comp.RemoteEntity.Value, Transform(entity).Coordinates);
        _stationAiSystem.SwitchRemoteMode(stationAi.Value, false);
    }

    private HashSet<Entity<HolopadComponent>> GetLinkedHolopads(Entity<HolopadComponent> entity)
    {
        var linkedHolopads = new HashSet<Entity<HolopadComponent>>();

        if (!TryComp<TelephoneComponent>(entity, out var holopadTelephone))
            return linkedHolopads;

        foreach (var linkedEnt in holopadTelephone.LinkedTelephones)
        {
            if (!TryComp<HolopadComponent>(linkedEnt, out var linkedHolopad))
                continue;

            linkedHolopads.Add((linkedEnt, linkedHolopad));
        }

        return linkedHolopads;
    }

    private void SetHolopadAmbientState(Entity<HolopadComponent> entity, bool isEnabled)
    {
        if (TryComp<PointLightComponent>(entity, out var pointLight))
            _pointLightSystem.SetEnabled(entity, isEnabled, pointLight);

        if (TryComp<AmbientSoundComponent>(entity, out var ambientSound))
            _ambientSoundSystem.SetAmbience(entity, isEnabled, ambientSound);
    }
}
