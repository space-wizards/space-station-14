using Content.Server.Interaction;
using Content.Server.Power.EntitySystems;
using Content.Server.Telephone;
using Content.Shared.Audio;
using Content.Shared.Chat.TypingIndicator;
using Content.Shared.Holopad;
using Content.Shared.Inventory;
using Content.Shared.Telephone;
using Robust.Server.GameObjects;
using Robust.Shared.Timing;
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
    [Dependency] private readonly IGameTiming _timing = default!;

    private float _updateTimer = 1.0f;

    private const float UpdateTime = 1.0f;
    private const float MinTimeBetweenSyncRequests = 0.5f;
    private TimeSpan _minTimeSpanBetweenSyncRequests;

    private HashSet<EntityUid> _pendingRequestsForSpriteState = new();
    private HashSet<EntityUid> _recentlyUpdatedHolograms = new(); // Clear on frame update

    public override void Initialize()
    {
        base.Initialize();

        _minTimeSpanBetweenSyncRequests = TimeSpan.FromSeconds(MinTimeBetweenSyncRequests);

        // Holopad bound user interface messages
        SubscribeLocalEvent<HolopadComponent, HolopadStartNewCallMessage>(OnHolopadStartNewCall);
        SubscribeLocalEvent<HolopadComponent, HolopadAnswerCallMessage>(OnHolopadAnswerCall);
        SubscribeLocalEvent<HolopadComponent, HolopadEndCallMessage>(OnHolopadEndCall);

        // Holopad -> telephone events
        SubscribeLocalEvent<HolopadComponent, TelephoneCallEvent>(OnHoloCall);
        SubscribeLocalEvent<HolopadComponent, TelephoneCallCommencedEvent>(OnHoloCallCommenced);
        SubscribeLocalEvent<HolopadComponent, TelephoneCallEndedEvent>(OnHoloCallEnded);
        SubscribeLocalEvent<HolopadComponent, TelephoneCallTerminatedEvent>(OnHoloCallTerminated);
        SubscribeLocalEvent<HolopadComponent, TelephoneMessageSentEvent>(OnTelephoneMessageSent);

        // Holopad start/shutdown events
        SubscribeLocalEvent<HolopadComponent, ComponentInit>(OnComponentInit);
        SubscribeLocalEvent<HolopadComponent, ComponentShutdown>(OnComponentShutdown);

        // Holopad user events
        SubscribeLocalEvent<HolopadUserComponent, ComponentInit>(OnComponentInit);

        // Networked events
        SubscribeNetworkEvent<HolopadUserTypingChangedEvent>(OnTypingChanged);
        SubscribeNetworkEvent<PlayerSpriteStateMessage>(OnPlayerSpriteStateMessage);
    }

    #region: Events

    private void OnHolopadStartNewCall(Entity<HolopadComponent> holopad, ref HolopadStartNewCallMessage args)
    {
        if (!TryComp<TelephoneComponent>(holopad, out var holopadTelephone))
            return;

        var receiver = GetEntity(args.Receiver);

        if (!TryComp<TelephoneComponent>(receiver, out var receiverTelephone))
            return;

        LinkHolopadToUser(holopad, args.Actor);

        _telephoneSystem.CallTelephone((holopad, holopadTelephone), (receiver, receiverTelephone), args.Actor);
    }

    private void OnHolopadAnswerCall(Entity<HolopadComponent> holopad, ref HolopadAnswerCallMessage args)
    {
        if (!TryComp<TelephoneComponent>(holopad, out var holopadTelephone))
            return;

        LinkHolopadToUser(holopad, args.Actor);

        _telephoneSystem.AnswerTelephone((holopad, holopadTelephone), args.Actor);
    }

    private void OnHolopadEndCall(Entity<HolopadComponent> holopad, ref HolopadEndCallMessage args)
    {
        if (!TryComp<TelephoneComponent>(holopad, out var holopadTelephone))
            return;

        _telephoneSystem.EndTelephoneCalls((holopad, holopadTelephone));
    }

    private void OnHoloCall(Entity<HolopadComponent> holopad, ref TelephoneCallEvent args)
    {
        SetHolopadEnviron(holopad, this.IsPowered(holopad, EntityManager));
    }

    private void OnHoloCallCommenced(Entity<HolopadComponent> holopad, ref TelephoneCallCommencedEvent args)
    {
        SetHolopadEnviron(holopad, this.IsPowered(holopad, EntityManager));

        if (holopad.Comp.Hologram == null)
            GenerateHologram(holopad);

        if (holopad.Comp.User != null)
        {
            RequestHolopadUserSpriteUpdate(holopad.Comp.User.Value);
        }

        else
        {
            var ev = new PlayerSpriteStateMessage(GetNetEntity(holopad), Array.Empty<SpriteLayerDatum>());
            RaiseNetworkEvent(ev);
        }
    }

    private void OnHoloCallEnded(Entity<HolopadComponent> holopad, ref TelephoneCallEndedEvent args)
    {
        ShutDownHolopad(holopad);
    }

    private void OnHoloCallTerminated(Entity<HolopadComponent> holopad, ref TelephoneCallTerminatedEvent args)
    {
        ShutDownHolopad(holopad);
        SetHolopadEnviron(holopad, false);
    }

    private void OnTelephoneMessageSent(Entity<HolopadComponent> holopad, ref TelephoneMessageSentEvent args)
    {
        LinkHolopadToUser(holopad, args.MessageSource);
    }

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

    private void OnComponentInit(Entity<HolopadComponent> holopad, ref ComponentInit args)
    {
        LinkHolopadToUser(holopad, holopad.Comp.User);
    }

    private void OnComponentInit(Entity<HolopadUserComponent> holopadUser, ref ComponentInit args)
    {
        foreach (var linkedHolopad in holopadUser.Comp.LinkedHolopads)
            LinkHolopadToUser(linkedHolopad, holopadUser);
    }

    private void OnComponentShutdown(Entity<HolopadComponent> holopad, ref ComponentShutdown args)
    {
        if (holopad.Comp.Hologram != null)
            DeleteHologram(holopad.Comp.Hologram.Value, holopad);
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

        SyncHolopadUserWithLinkedHolograms(uid.Value, holopadUser, ev.SpriteLayerData);
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
            while (query.MoveNext(out var ent, out var entHolopad, out var entTelephone, out var entXform))
            {
                if (_userInterfaceSystem.IsUiOpen(ent, HolopadUiKey.Key))
                    UpdateUIState((ent, entHolopad), entTelephone);

                if (entHolopad.User != null &&
                    !_xformSystem.InRange((entHolopad.User.Value, Transform(entHolopad.User.Value)), (ent, entXform), entTelephone.ListeningRange))
                    UnlinkHolopadFromUser((ent, entHolopad), entHolopad.User.Value);
            }
        }

        _recentlyUpdatedHolograms.Clear();
    }

    public void UpdateUIState(Entity<HolopadComponent> holopad, TelephoneComponent telephone)
    {
        var holopads = new Dictionary<NetEntity, string>();

        var query = AllEntityQuery<HolopadComponent, TelephoneComponent>();
        while (query.MoveNext(out var ent, out var entHolopad, out var entTelephone))
        {
            if (holopad.Owner == ent)
                continue;

            if (!this.IsPowered(holopad, EntityManager))
                continue;

            holopads.Add(GetNetEntity(ent), MetaData(ent).EntityName);
        }

        // Set the UI state
        _userInterfaceSystem.SetUiState(holopad.Owner, HolopadUiKey.Key, new HolopadBoundInterfaceState(telephone.CurrentState, holopads));
    }

    private void GenerateHologram(Entity<HolopadComponent> holopad)
    {
        var uid = Spawn(holopad.Comp.HologramProtoId, Transform(holopad).Coordinates);

        // Safeguard - spawned holograms must have this component
        if (!TryComp<HolopadHologramComponent>(uid, out var component))
        {
            Del(uid);
            return;
        }

        holopad.Comp.Hologram = new Entity<HolopadHologramComponent>(uid, component);
    }

    private void DeleteHologram(Entity<HolopadHologramComponent> hologram, Entity<HolopadComponent> attachedHolopad)
    {
        attachedHolopad.Comp.Hologram = null;

        QueueDel(hologram);
    }

    private void LinkHolopadToUser(Entity<HolopadComponent> holopad, EntityUid? user)
    {
        if (user == null)
            return;

        if (!TryComp<HolopadUserComponent>(user.Value, out var userComp))
            userComp = AddComp<HolopadUserComponent>(user.Value);

        if (user != holopad.Comp.User?.Owner)
        {
            UnlinkHolopadFromUser(holopad, holopad.Comp.User);
            userComp.LinkedHolopads.Add(holopad);
            holopad.Comp.User = (user.Value, userComp);
        }

        RequestHolopadUserSpriteUpdate((user.Value, userComp));
    }

    private void RequestHolopadUserSpriteUpdate(Entity<HolopadUserComponent> user)
    {
        if (_pendingRequestsForSpriteState.Add(user))
        {
            var ev = new PlayerSpriteStateRequest(GetNetEntity(user));
            RaiseNetworkEvent(ev);
        }
    }

    private void UnlinkHolopadFromUser(Entity<HolopadComponent> holopad, Entity<HolopadUserComponent>? user)
    {
        if (user == null)
            return;

        holopad.Comp.User = null;

        foreach (var linkedHolopad in GetLinkedHolopads(holopad))
        {
            if (linkedHolopad.Comp.Hologram != null)
            {
                _appearanceSystem.SetData(linkedHolopad.Comp.Hologram.Value.Owner, TypingIndicatorVisuals.IsTyping, false);

                var ev = new PlayerSpriteStateMessage(GetNetEntity(linkedHolopad.Comp.Hologram.Value), Array.Empty<SpriteLayerDatum>());
                RaiseNetworkEvent(ev);
            }
        }

        if (!HasComp<HolopadUserComponent>(user))
            return;

        user.Value.Comp.LinkedHolopads.Remove(holopad);

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
    }

    private void SyncHolopadUserWithLinkedHolograms(EntityUid uid, HolopadUserComponent component, SpriteLayerDatum[] spriteLayerData)
    {
        foreach (var linkedHolopad in component.LinkedHolopads)
        {
            foreach (var receivingHolopad in GetLinkedHolopads(linkedHolopad))
            {
                if (receivingHolopad.Comp.Hologram == null ||
                    !_recentlyUpdatedHolograms.Add(receivingHolopad.Comp.Hologram.Value))
                    continue;

                var netHologram = GetNetEntity(receivingHolopad.Comp.Hologram.Value);
                var ev = new PlayerSpriteStateMessage(netHologram, spriteLayerData);
                RaiseNetworkEvent(ev);
            }
        }
    }

    private HashSet<Entity<HolopadComponent>> GetLinkedHolopads(Entity<HolopadComponent> holopad)
    {
        var linkedHolopads = new HashSet<Entity<HolopadComponent>>();

        if (!TryComp<TelephoneComponent>(holopad, out var holopadTelephone))
            return linkedHolopads;

        foreach (var linkedEnt in holopadTelephone.LinkedTelephones)
        {
            if (!TryComp<HolopadComponent>(linkedEnt, out var linkedHolopad))
                continue;

            linkedHolopads.Add((linkedEnt, linkedHolopad));
        }

        return linkedHolopads;
    }

    private void SetHolopadEnviron(Entity<HolopadComponent> holopad, bool isEnabled)
    {
        if (TryComp<PointLightComponent>(holopad, out var pointLight))
            _pointLightSystem.SetEnabled(holopad, isEnabled, pointLight);

        if (TryComp<AmbientSoundComponent>(holopad, out var ambientSound))
            _ambientSoundSystem.SetAmbience(holopad, isEnabled, ambientSound);
    }
}
