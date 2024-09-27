using Content.Server.Interaction;
using Content.Server.Power.EntitySystems;
using Content.Server.Telephone;
using Content.Shared.Chat.TypingIndicator;
using Content.Shared.Holopad;
using Content.Shared.Inventory;
using Content.Shared.Mind.Components;
using Content.Shared.Telephone;
using Robust.Server.GameObjects;
using Robust.Server.GameStates;
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
    [Dependency] private readonly InventorySystem _inventorySystem = default!;
    [Dependency] private readonly InteractionSystem _interactionSystem = default!;
    [Dependency] private readonly IGameTiming _timing = default!;

    private float _updateTimer = 1.0f;
    private const float UpdateTime = 1.0f;

    public override void Initialize()
    {
        base.Initialize();

        // Holopad specific events
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

        // PVS events
        SubscribeLocalEvent<ExpandPvsEvent>(OnExpandPvs);

        // Networked events
        SubscribeNetworkEvent<HolopadUserTypingChangedEvent>(OnTypingChanged);
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
        if (!TryComp<TelephoneComponent>(holopad, out var telephone))
            return;

        LinkHolopadToUser(holopad, args.Actor);

        _telephoneSystem.AnswerTelephone((holopad, telephone));
    }

    private void OnHolopadEndCall(Entity<HolopadComponent> holopad, ref HolopadEndCallMessage args)
    {
        if (!TryComp<TelephoneComponent>(holopad, out var telephone))
            return;

        _telephoneSystem.EndTelephoneCalls((holopad, telephone));
    }

    private void OnHoloCall(Entity<HolopadComponent> holopad, ref TelephoneCallEvent args)
    {
        if (TryComp<PointLightComponent>(holopad, out var pointLight))
            _pointLightSystem.SetEnabled(holopad, this.IsPowered(holopad, EntityManager), pointLight);
    }

    private void OnHoloCallCommenced(Entity<HolopadComponent> holopad, ref TelephoneCallCommencedEvent args)
    {
        if (TryComp<PointLightComponent>(holopad, out var pointLight))
            _pointLightSystem.SetEnabled(holopad, this.IsPowered(holopad, EntityManager), pointLight);

        if (holopad.Comp.Hologram == null)
            GenerateHologram(holopad);

        SyncHolopadHologramWithMimicryTarget(holopad);
    }

    private void OnHoloCallEnded(Entity<HolopadComponent> holopad, ref TelephoneCallEndedEvent args)
    {
        ShutDownHolopad(holopad);
    }

    private void OnHoloCallTerminated(Entity<HolopadComponent> holopad, ref TelephoneCallTerminatedEvent args)
    {
        if (TryComp<PointLightComponent>(holopad, out var pointLight))
            _pointLightSystem.SetEnabled(holopad, false, pointLight);

        ShutDownHolopad(holopad);
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
        SyncHolopadHologramWithMimicryTarget(holopad);
    }

    private void OnComponentShutdown(Entity<HolopadComponent> holopad, ref ComponentShutdown args)
    {
        if (holopad.Comp.Hologram != null)
            DeleteHologram(holopad.Comp.Hologram.Value, holopad);
    }

    private void OnExpandPvs(ref ExpandPvsEvent args)
    {
        var query = AllEntityQuery<HolopadUserComponent>();
        while (query.MoveNext(out var ent, out var entHolopadUser))
        {
            /*

            // Unlink holopad users if they stray too far away
            foreach (var linkedHolopad in entHolopadUser.LinkedHolopads.ToList())
            {
                if (!TryComp<TelephoneComponent>(linkedHolopad, out var linkedTelephone))
                    continue;

                if (!_interactionSystem.InRangeAndAccessible(ent, linkedHolopad.Owner, linkedTelephone.ListeningRange))
                    UnlinkHolopadFromUser(linkedHolopad, (ent, entHolopadUser));
            }

            if (!HasComp<HolopadUserComponent>(ent) || !entHolopadUser.LinkedHolopads.Any())
                continue;*/

            if (args.Entities == null)
                args.Entities = new();

            // Add holopad users to PVS so that they can be rendered by distant holopads
            args.Entities.Add(ent);

            // Add their inventory items to PVS for the same reason
            if (_inventorySystem.TryGetSlots(ent, out var slots))
            {
                foreach (var slot in slots)
                {
                    _inventorySystem.TryGetSlotContainer(ent, slot.Name, out var container, out var definition);

                    if (container?.ContainedEntity != null)
                        args.Entities.Add(container.ContainedEntity.Value);
                }
            }
        }
    }

    #endregion

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        _updateTimer += frameTime;

        if (_updateTimer >= UpdateTime)
        {
            _updateTimer -= UpdateTime;

            var query = AllEntityQuery<HolopadComponent, TelephoneComponent>();
            while (query.MoveNext(out var ent, out var holopad, out var telephone))
            {
                if (!_userInterfaceSystem.IsUiOpen(ent, HolopadUiKey.Key))
                    continue;

                UpdateUIState((ent, holopad), telephone);
            }
        }
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

        if (user != holopad.Comp.User?.Owner)
        {
            UnlinkHolopadFromUser(holopad, holopad.Comp.User, false);

            if (!TryComp<HolopadUserComponent>(user, out var userComp))
                userComp = AddComp<HolopadUserComponent>(user.Value);

            userComp.LinkedHolopads.Add(holopad);
            holopad.Comp.User = (user.Value, userComp);
        }

        foreach (var linkedHolopad in GetLinkedHolopads(holopad))
            SyncHolopadHologramWithMimicryTarget(linkedHolopad);
    }

    private void UnlinkHolopadFromUser(Entity<HolopadComponent> holopad, Entity<HolopadUserComponent>? user, bool sync = true)
    {
        if (user == null)
            return;

        holopad.Comp.User = null;

        foreach (var linkedHolopad in GetLinkedHolopads(holopad))
        {
            if (linkedHolopad.Comp.Hologram != null)
                _appearanceSystem.SetData(linkedHolopad.Comp.Hologram.Value.Owner, TypingIndicatorVisuals.IsTyping, false);

            if (sync)
                SyncHolopadHologramWithMimicryTarget(linkedHolopad);
        }

        if (!HasComp<HolopadUserComponent>(user))
            return;

        user.Value.Comp.LinkedHolopads.Remove(holopad);

        if (!user.Value.Comp.LinkedHolopads.Any())
            RemComp<HolopadUserComponent>(user.Value);
    }

    private void ShutDownHolopad(Entity<HolopadComponent> holopad)
    {
        if (holopad.Comp.Hologram != null)
            DeleteHologram(holopad.Comp.Hologram.Value, holopad);

        if (holopad.Comp.User != null)
            UnlinkHolopadFromUser(holopad, holopad.Comp.User.Value);
    }

    private void SyncHolopadHologramWithMimicryTarget(Entity<HolopadComponent> holopad)
    {
        var netHologram = GetNetEntity(holopad.Comp.Hologram);

        if (netHologram == null)
            return;

        NetEntity? netTarget = null;
        var linkedHolopads = GetLinkedHolopads(holopad);

        // If the holopad has only one active link, mimic the user of that device
        if (linkedHolopads.Count == 1)
            netTarget = GetNetEntity(linkedHolopads.First().Comp.User);

        var ev = new HolopadHologramVisualsUpdateEvent(netHologram.Value, netTarget);
        RaiseNetworkEvent(ev);
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
}
