using Content.Server.Power.Components;
using Content.Server.Telephone;
using Content.Shared.Holopad;
using Content.Shared.Telephone;
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

        SubscribeLocalEvent<HolopadComponent, HolopadStartNewCallMessage>(OnHolopadStartNewCallMessage);
        SubscribeLocalEvent<HolopadComponent, HolopadAnswerCallMessage>(OnHolopadAnswerCallMessage);
        SubscribeLocalEvent<HolopadComponent, HolopadHangUpOnCallMessage>(OnHolopadHangUpOnCallMessage);

        SubscribeLocalEvent<HolopadComponent, TelephoneCallCommencedEvent>(OnHoloCallCommenced);
        SubscribeLocalEvent<HolopadComponent, TelephoneHungUpEvent>(OnHoloCallEnded);
        SubscribeLocalEvent<HolopadComponent, TelephoneCallTerminatedEvent>(OnHoloCallTerminated);

        SubscribeLocalEvent<HolopadComponent, ComponentInit>(OnComponentInit);
        SubscribeLocalEvent<HolopadComponent, ComponentShutdown>(OnComponentShutdown);

        SubscribeLocalEvent<ExpandPvsEvent>(OnExpandPvs);
    }

    #region: Events

    private void OnHolopadStartNewCallMessage(EntityUid uid, HolopadComponent component, HolopadStartNewCallMessage args)
    {
        var receiver = GetEntity(args.Receiver);

        if (!TryComp<TelephoneComponent>(receiver, out var receiverTelephone))
            return;

        _telephoneSystem.CallTelephone(receiver, receiverTelephone, uid, args.Actor);
    }

    private void OnHolopadAnswerCallMessage(EntityUid uid, HolopadComponent component, HolopadAnswerCallMessage args)
    {
        if (!TryComp<TelephoneComponent>(uid, out var telephone))
            return;

        _telephoneSystem.AnswerTelephone(uid, telephone, args.Actor);
    }

    private void OnHolopadHangUpOnCallMessage(EntityUid uid, HolopadComponent component, HolopadHangUpOnCallMessage args)
    {
        if (!TryComp<TelephoneComponent>(uid, out var telephone))
            return;

        _telephoneSystem.HangUpTelephone(uid, telephone);
    }

    private void OnHoloCallCommenced(EntityUid uid, HolopadComponent component, TelephoneCallCommencedEvent args)
    {
        SyncHologramWithTarget(uid, component);
    }

    private void OnHoloCallEnded(EntityUid uid, HolopadComponent component, TelephoneHungUpEvent args)
    {
        TurnOffHologram(uid, component);
    }

    private void OnHoloCallTerminated(EntityUid uid, HolopadComponent component, TelephoneCallTerminatedEvent args)
    {
        TurnOffHologram(uid, component);
    }

    private void OnComponentInit(EntityUid uid, HolopadComponent component, ComponentInit args)
    {
        SyncHologramWithTarget(uid, component);
    }

    private void OnComponentShutdown(EntityUid uid, HolopadComponent component, ComponentShutdown args)
    {
        TurnOffHologram(uid, component);
    }

    private void OnExpandPvs(ref ExpandPvsEvent args)
    {
        var query = AllEntityQuery<HolopadComponent, TelephoneComponent>();
        while (query.MoveNext(out var ent, out var holopad, out var telephone))
        {
            if (telephone.User == null)
                continue;

            if (telephone.CurrentState != TelephoneState.InCall)
                continue;

            if (args.Entities == null)
                args.Entities = new();

            args.Entities.Add(telephone.User.Value);
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

                UpdateUIState(ent, holopad, telephone);
            }
        }
    }

    public void UpdateUIState(EntityUid uid, HolopadComponent holopad, TelephoneComponent telephone)
    {
        var holopads = new Dictionary<NetEntity, string>();

        var query = AllEntityQuery<HolopadComponent, TelephoneComponent>();
        while (query.MoveNext(out var ent, out var entHolopad, out var entTelephone))
        {
            if (uid == ent)
                continue;

            if (!_telephoneSystem.IsTelephoneReachable(ent, entTelephone))
                continue;

            holopads.Add(GetNetEntity(ent), MetaData(ent).EntityName);
        }

        // Set the UI state
        _userInterfaceSystem.SetUiState(uid, HolopadUiKey.Key, new HolopadBoundInterfaceState(telephone.CurrentState, holopads));
    }

    public void CallHolopad(EntityUid uid, HolopadComponent component, EntityUid recipient)
    {

    }

    public void AnswerHoloCall(EntityUid uid, HolopadComponent component)
    {

    }

    public void TerminateHoloCall(EntityUid uid, HolopadComponent component)
    {

    }

    private void GenerateCrewHologram(EntityUid uid, HolopadComponent component)
    {
        // Spawn hologram entity
        // Move user into world PVS so they can be visually duplicated across clients
    }

    private void TurnOffHologram(EntityUid uid, HolopadComponent component)
    {
        QueueDel(component.Hologram);
    }

    private void SyncHologramWithTarget(EntityUid uid, HolopadComponent component)
    {
        if (!TryComp<TelephoneComponent>(uid, out var telephone) ||
            !TryComp<TelephoneComponent>(telephone.LinkedTelephone, out var linkedTelephone))
            return;

        var target = linkedTelephone.User;

        if (target == null)
            return;
        component.Hologram = Spawn(component.HologramProtoId, Transform(uid).Coordinates);

        var netHologram = GetNetEntity(component.Hologram);
        var netTarget = GetNetEntity(target.Value);

        if (netHologram == null)
            return;

        var ev = new HolopadHologramVisualsUpdateEvent(netHologram.Value, netTarget);
        RaiseNetworkEvent(ev);
    }
}
