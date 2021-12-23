using System;
using Content.Server.Atmos;
using Content.Server.Atmos.Components;
using Content.Server.Atmos.EntitySystems;
using Content.Server.Body.Components;
using Content.Server.Popups;
using Content.Shared.Atmos;
using Content.Shared.Body.Components;
using Content.Shared.Body.Events;
using Content.Shared.MobState.Components;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Localization;
using Robust.Shared.Player;
using Robust.Shared.Timing;

namespace Content.Server.Body.Systems;

public class LungSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _gameTiming = default!;
    [Dependency] private readonly AtmosphereSystem _atmosSys = default!;
    [Dependency] private readonly BloodstreamSystem _bloodstreamSystem = default!;
    [Dependency] private readonly PopupSystem _popupSystem = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<LungComponent, AddedToBodyEvent>(OnAddedToBody);
    }

    private void OnAddedToBody(EntityUid uid, LungComponent component, AddedToBodyEvent args)
    {
        Inhale(uid, component.CycleDelay);
    }

    public void Gasp(EntityUid uid,
        LungComponent? lung=null,
        SharedMechanismComponent? mech=null)
    {
        if (!Resolve(uid, ref lung, ref mech))
            return;

        if (_gameTiming.CurTime >= lung.LastGaspPopupTime + lung.GaspPopupCooldown)
        {
            lung.LastGaspPopupTime = _gameTiming.CurTime;
            _popupSystem.PopupEntity(Loc.GetString("lung-behavior-gasp"), uid, Filter.Pvs(uid));
        }

        if (mech.Body != null && TryComp((mech.Body).Owner, out MobStateComponent? mobState) && !mobState.IsAlive())
            return;

        Inhale(uid, lung.CycleDelay);
    }

    public void UpdateLung(EntityUid uid,
        LungComponent? lung=null,
        SharedMechanismComponent? mech=null)
    {
        if (!Resolve(uid, ref lung, ref mech))
            return;

        if (mech.Body != null && EntityManager.TryGetComponent((mech.Body).Owner, out MobStateComponent? mobState) && mobState.IsCritical())
        {
            return;
        }

        if (lung.Status == LungStatus.None)
        {
            lung.Status = LungStatus.Inhaling;
        }

        lung.AccumulatedFrametime += lung.Status switch
        {
            LungStatus.Inhaling => 1,
            LungStatus.Exhaling => -1,
            _ => throw new ArgumentOutOfRangeException()
        };

        var absoluteTime = Math.Abs(lung.AccumulatedFrametime);
        var delay = lung.CycleDelay;

        if (absoluteTime < delay)
        {
            return;
        }

        switch (lung.Status)
        {
            case LungStatus.Inhaling:
                Inhale(uid, absoluteTime);
                lung.Status = LungStatus.Exhaling;
                break;
            case LungStatus.Exhaling:
                Exhale(uid, absoluteTime);
                lung.Status = LungStatus.Inhaling;
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }

        lung.AccumulatedFrametime = absoluteTime - delay;
    }

    /// <summary>
    ///     Tries to find an air mixture to inhale from, then inhales from it.
    /// </summary>
    public void Inhale(EntityUid uid, float frameTime,
        LungComponent? lung=null,
        SharedMechanismComponent? mech=null)
    {
        if (!Resolve(uid, ref lung, ref mech))
            return;

        // TODO Jesus Christ make this event based.
        if (mech.Body != null &&
            EntityManager.TryGetComponent((mech.Body).Owner, out InternalsComponent? internals) &&
            internals.BreathToolEntity != null &&
            internals.GasTankEntity != null &&
            EntityManager.TryGetComponent(internals.BreathToolEntity, out BreathToolComponent? breathTool) &&
            breathTool.IsFunctional &&
            EntityManager.TryGetComponent(internals.GasTankEntity, out GasTankComponent? gasTank))
        {
            TakeGasFrom(uid, frameTime, gasTank.RemoveAirVolume(Atmospherics.BreathVolume), lung);
            return;
        }

        if (_atmosSys.GetTileMixture(EntityManager.GetComponent<TransformComponent>(uid).Coordinates, true) is not { } tileAir)
        {
            return;
        }

        TakeGasFrom(uid, frameTime, tileAir, lung);
    }

    /// <summary>
    ///     Inhales directly from a given mixture.
    /// </summary>
    public void TakeGasFrom(EntityUid uid, float frameTime, GasMixture from,
        LungComponent? lung=null,
        SharedMechanismComponent? mech=null)
    {
        if (!Resolve(uid, ref lung, ref mech))
            return;

        var ratio = (Atmospherics.BreathVolume / from.Volume) * frameTime;

        _atmosSys.Merge(lung.Air, from.RemoveRatio(ratio));

        // Push to bloodstream
        if (mech.Body == null)
            return;

        if (!EntityManager.TryGetComponent((mech.Body).Owner, out BloodstreamComponent? bloodstream))
            return;

        var to = bloodstream.Air;

        _atmosSys.Merge(to, lung.Air);
        lung.Air.Clear();
    }

    /// <summary>
    ///     Tries to find a gas mixture to exhale to, then pushes gas to it.
    /// </summary>
    public void Exhale(EntityUid uid, float frameTime,
        LungComponent? lung=null,
        SharedMechanismComponent? mech=null)
    {
        if (!Resolve(uid, ref lung, ref mech))
            return;

        if (_atmosSys.GetTileMixture(EntityManager.GetComponent<TransformComponent>(uid).Coordinates, true) is not { } tileAir)
        {
            return;
        }

        PushGasTo(uid, tileAir, lung, mech);
    }

    /// <summary>
    ///     Pushes gas from the lungs to a gas mixture.
    /// </summary>
    public void PushGasTo(EntityUid uid, GasMixture to,
        LungComponent? lung=null,
        SharedMechanismComponent? mech=null)
    {
        if (!Resolve(uid, ref lung, ref mech))
            return;

        // TODO: Make the bloodstream separately pump toxins into the lungs, making the lungs' only job to empty.
        if (mech.Body == null)
            return;

        if (!EntityManager.TryGetComponent((mech.Body).Owner, out BloodstreamComponent? bloodstream))
            return;

        _bloodstreamSystem.PumpToxins((mech.Body).Owner, lung.Air, bloodstream);

        var lungRemoved = lung.Air.RemoveRatio(0.5f);
        _atmosSys.Merge(to, lungRemoved);
    }
}
