using Content.Server.Construction.Completions;
using Content.Server.Power.Components;
using Content.Server.Telephone;
using Content.Shared.Chat.TypingIndicator;
using Content.Shared.Doors.Components;
using Content.Shared.Holopad;
using Content.Shared.Telephone;
using JetBrains.FormatRipper.Elf;
using Robust.Server.GameObjects;
using Robust.Server.GameStates;
using Robust.Shared.GameObjects;
using Robust.Shared.Timing;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;

namespace Content.Server.Holopad;

public sealed class HolopadSystem : SharedHolopadSystem
{
    [Dependency] private readonly TelephoneSystem _telephoneSystem = default!;
    [Dependency] private readonly UserInterfaceSystem _userInterfaceSystem = default!;
    [Dependency] private readonly TransformSystem _xformSystem = default!;
    [Dependency] private readonly AppearanceSystem _appearanceSystem = default!;
    [Dependency] private readonly IGameTiming _timing = default!;

    private float _updateTimer = 1.0f;
    private const float UpdateTime = 1.0f;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<HolopadComponent, HolopadStartNewCallMessage>(OnHolopadStartNewCall);
        SubscribeLocalEvent<HolopadComponent, HolopadAnswerCallMessage>(OnHolopadAnswerCall);
        SubscribeLocalEvent<HolopadComponent, HolopadEndCallMessage>(OnHolopadEndCall);

        SubscribeLocalEvent<HolopadComponent, TelephoneCallCommencedEvent>(OnHoloCallCommenced);
        SubscribeLocalEvent<HolopadComponent, TelephoneCallEndedEvent>(OnHoloCallEnded);
        SubscribeLocalEvent<HolopadComponent, TelephoneCallTerminatedEvent>(OnHoloCallTerminated);

        SubscribeLocalEvent<HolopadComponent, ComponentInit>(OnComponentInit);
        SubscribeLocalEvent<HolopadComponent, ComponentShutdown>(OnComponentShutdown);

        SubscribeLocalEvent<ExpandPvsEvent>(OnExpandPvs);

        SubscribeNetworkEvent<HolopadUserAppearanceChangedEvent>(OnAppearanceChanged);
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

    private void OnHoloCallCommenced(Entity<HolopadComponent> holopad, ref TelephoneCallCommencedEvent args)
    {
        var targetHolopad = args.Source.Owner == holopad.Owner ? args.Receiver : args.Source;

        if (holopad.Comp.Hologram == null)
            GenerateHologram(holopad);

        if (!TryComp<HolopadComponent>(targetHolopad, out var targetHolopadComp) || targetHolopadComp.User == null)
            return;

        if (holopad.Comp.Hologram == null)
            return;

        holopad.Comp.Hologram.Value.Comp.LinkedEntities.Add(targetHolopadComp.User.Value);
        SyncHologramWithHolopadUsers(holopad.Comp.Hologram.Value);
    }

    private void OnHoloCallEnded(Entity<HolopadComponent> holopad, ref TelephoneCallEndedEvent args)
    {
        if (holopad.Comp.Hologram != null)
            DeleteHologram(holopad.Comp.Hologram.Value);

        if (holopad.Comp.User != null)
            UnlinkHolopadFromUser(holopad, holopad.Comp.User.Value);
    }

    private void OnHoloCallTerminated(Entity<HolopadComponent> holopad, ref TelephoneCallTerminatedEvent args)
    {
        if (holopad.Comp.Hologram != null)
            DeleteHologram(holopad.Comp.Hologram.Value);

        if (holopad.Comp.User != null)
            UnlinkHolopadFromUser(holopad, holopad.Comp.User.Value);
    }

    private void OnTypingChanged(HolopadUserTypingChangedEvent ev, EntitySessionEventArgs args)
    {
        var uid = args.SenderSession.AttachedEntity;

        if (!Exists(uid))
            return;

        if (!TryComp<HolopadUserComponent>(uid, out var holopadUser))
            return;

        foreach (var hologram in holopadUser.LinkedHolograms)
            _appearanceSystem.SetData(hologram, TypingIndicatorVisuals.IsTyping, ev.IsTyping);
    }

    private void OnAppearanceChanged(HolopadUserAppearanceChangedEvent ev, EntitySessionEventArgs args)
    {
        var uid = args.SenderSession.AttachedEntity;

        if (!Exists(uid))
            return;

        if (!TryComp<HolopadUserComponent>(uid, out var holopadUser))
            return;

        foreach (var hologram in holopadUser.LinkedHolograms)
            SyncHologramWithHolopadUsers(hologram);
    }

    private void OnComponentInit(Entity<HolopadComponent> holopad, ref ComponentInit args)
    {
        if (holopad.Comp.Hologram != null)
            SyncHologramWithHolopadUsers(holopad.Comp.Hologram.Value);
    }

    private void OnComponentShutdown(Entity<HolopadComponent> holopad, ref ComponentShutdown args)
    {
        if (holopad.Comp.Hologram != null)
            DeleteHologram(holopad.Comp.Hologram.Value);
    }

    private void OnExpandPvs(ref ExpandPvsEvent args)
    {
        var query = AllEntityQuery<HolopadUserComponent>();
        while (query.MoveNext(out var ent, out var holopadUser))
        {
            if (!holopadUser.LinkedHolograms.Any())
                continue;

            if (args.Entities == null)
                args.Entities = new();

            args.Entities.Add(ent);
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

            if (!_telephoneSystem.IsTelephonePowered((ent, entTelephone)))
                continue;

            holopads.Add(GetNetEntity(ent), MetaData(ent).EntityName);
        }

        // Set the UI state
        _userInterfaceSystem.SetUiState(holopad.Owner, HolopadUiKey.Key, new HolopadBoundInterfaceState(telephone.CurrentState, holopads));
    }

    private void GenerateHologram(Entity<HolopadComponent> holopad)
    {
        var uid = Spawn(holopad.Comp.HologramProtoId, Transform(holopad).Coordinates);

        if (!TryComp<HolopadHologramComponent>(uid, out var component))
        {
            QueueDel(uid);
            return;
        }

        holopad.Comp.Hologram = new Entity<HolopadHologramComponent>(uid, component);
    }

    private void DeleteHologram(Entity<HolopadHologramComponent> hologram)
    {
        foreach (var ent in hologram.Comp.LinkedEntities)
        {
            if (!TryComp<HolopadUserComponent>(ent, out var holopadUser))
                continue;

            holopadUser.LinkedHolograms.Remove(hologram);
        }

        QueueDel(hologram);
    }

    private void LinkHolopadToUser(Entity<HolopadComponent> holopad, EntityUid user)
    {
        if (!TryComp<HolopadUserComponent>(user, out var userComp))
            userComp = AddComp<HolopadUserComponent>(user);

        userComp.LinkedHolopads.Add(holopad);
        holopad.Comp.User = (user, userComp);
    }

    private void UnlinkHolopadFromUser(Entity<HolopadComponent> holopad, Entity<HolopadUserComponent> user)
    {
        holopad.Comp.User = null;

        if (!HasComp<HolopadUserComponent>(user))
            return;

        user.Comp.LinkedHolopads.Remove(holopad);

        if (!user.Comp.LinkedHolopads.Any())
            RemComp<HolopadUserComponent>(user);
    }

    private void SyncHologramWithHolopadUsers(Entity<HolopadHologramComponent> hologram)
    {
        var netHologram = GetNetEntity(hologram);
        var entCount = hologram.Comp.LinkedEntities.Count;

        switch (entCount)
        {
            // TODO: add a generic sprite for indicating no linked entities
            case 0:
                break;

            case 1:
                var netEnt = GetNetEntity(hologram.Comp.LinkedEntities.First());
                var ev = new HolopadHologramVisualsUpdateEvent(netHologram, netEnt);
                RaiseNetworkEvent(ev);
                break;

            // TODO: add a generic sprite for indicating multiple linked entities
            default:
                break;
        }
    }
}
