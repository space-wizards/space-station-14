using Content.Server.Power.Components;
using Content.Server.Telephone;
using Content.Shared.Holopad;
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
    [Dependency] private readonly IGameTiming _timing = default!;

    private float _updateTimer = 1.0f;
    private const float UpdateTime = 1.0f;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<HolopadComponent, HolopadStartNewCallMessage>(OnHolopadStartNewCallMessage);
        SubscribeLocalEvent<HolopadComponent, HolopadAnswerCallMessage>(OnHolopadAnswerCallMessage);
        SubscribeLocalEvent<HolopadComponent, HolopadHangUpOnCallMessage>(OnHolopadHangUpOnCallMessage);
    }

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
}
