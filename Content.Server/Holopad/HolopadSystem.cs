using Content.Server.Power.Components;
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
    [Dependency] private readonly UserInterfaceSystem _userInterfaceSystem = default!;
    [Dependency] private readonly TransformSystem _xformSystem = default!;
    [Dependency] private readonly AppearanceSystem _appearanceSystem = default!;
    [Dependency] private readonly IGameTiming _timing = default!;

    private float _updateTimer = 1.0f;
    private const float UpdateTime = 1.0f;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<HolopadComponent, HolopadMessage>(OnHolopadMessage);
    }

    private void OnHolopadMessage(EntityUid uid, HolopadComponent component, HolopadMessage args)
    {

    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        _updateTimer += frameTime;

        if (_updateTimer >= UpdateTime)
        {
            _updateTimer -= UpdateTime;

            var query = AllEntityQuery<HolopadComponent>();
            while (query.MoveNext(out var ent, out var holopad))
            {
                if (!_userInterfaceSystem.IsUiOpen(ent, HolopadUiKey.Key))
                    continue;

                UpdateUIState(ent, holopad);
            }
        }
    }

    public void UpdateUIState(EntityUid uid, HolopadComponent component)
    {
        var holopads = new Dictionary<NetEntity, string>();

        var query = AllEntityQuery<HolopadComponent, ApcPowerReceiverComponent, TransformComponent>();
        while (query.MoveNext(out var ent, out var entHolopad, out var apcPowerReceiver, out var xform))
        {
            if (uid == ent)
                continue;

            if (!CanHolopadBeUsed(ent, entHolopad, apcPowerReceiver, xform))
                continue;

            holopads.Add(GetNetEntity(ent), MetaData(ent).EntityName);
        }

        // Set the UI state
        _userInterfaceSystem.SetUiState(uid, HolopadUiKey.Key, new HolopadBoundInterfaceState(holopads));
    }

    public void CallHolopad(EntityUid uid, HolopadComponent component, EntityUid recipient)
    {
        if (!TryComp<TelephoneComponent>(uid, out var telephone))
            return;

        // Update holopad appearance
        _appearanceSystem.SetData(recipient, HolopadVisualState.State, telephone.CurrentState);
    }

    public void AnswerHoloCall(EntityUid uid, HolopadComponent component)
    {
        if (!TryComp<TelephoneComponent>(uid, out var telephone))
            return;

        // Update holopad appearance
        _appearanceSystem.SetData(uid, HolopadVisualState.State, telephone.CurrentState);
    }

    public void TerminateHoloCall(EntityUid uid, HolopadComponent component)
    {
        if (!TryComp<TelephoneComponent>(uid, out var telephone))
            return;

        // Update holopad appearance
        _appearanceSystem.SetData(uid, HolopadVisualState.State, telephone.CurrentState);
    }

    private void GenerateCrewHologram(EntityUid uid, HolopadComponent component)
    {
        // Spawn hologram entity

        // Move user into world PVS so they can be visually duplicated across clients
    }

    public bool CanHolopadBeUsed(EntityUid uid, HolopadComponent component, ApcPowerReceiverComponent? apcPowerReceiver = null, TransformComponent? xform = null)
    {
        if (!Resolve(uid, ref xform, ref apcPowerReceiver, false))
            return false;

        if (!xform.Anchored)
            return false;

        if (!apcPowerReceiver.Powered)
            return false;

        return true;
    }

    public bool HolopadIsInUse(EntityUid uid, HolopadComponent component)
    {
        if (component.CurrentState == HolopadState.Inactive ||
            component.CurrentState == HolopadState.HangingUp)
            return false;

        if (component.CurrentUser == null)
            return false;

        var distance = (_xformSystem.GetWorldPosition(uid) - _xformSystem.GetWorldPosition(component.CurrentUser.Value)).Length();

        if (distance > component.InteractionDistance)
            return false;

        return true;
    }
}
