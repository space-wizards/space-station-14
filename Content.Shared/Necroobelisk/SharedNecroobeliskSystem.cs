using System.Linq;
using System.Numerics;
using Content.Shared.Necroobelisk.Components;
using Content.Shared.Maps;
using Content.Shared.Physics;
using Robust.Shared.Map;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Components;
using Robust.Shared.Random;
using Robust.Shared.Timing;
using Content.Shared.Administration.Logs;
using Content.Shared.Database;
using Content.Shared.Interaction;
using Content.Shared.Popups;
using Robust.Shared.GameStates;
using Robust.Shared.Network;
using Robust.Shared.Utility;
using Robust.Shared.Audio;
using Content.Shared.Sanity.Components;
using Content.Shared.CCVar;
using Content.Shared.Materials;
using Content.Shared.Radio;
using Robust.Shared.Map.Components;


namespace Content.Shared.Necroobelisk;

public sealed class SharedNecroobeliskSystem : EntitySystem
{
    [Dependency] protected readonly IGameTiming Timing = default!;
    [Dependency] private readonly IMapManager _map = default!;
    [Dependency] private readonly ITileDefinitionManager _tiledef = default!;
    [Dependency] protected readonly SharedAudioSystem Audio = default!;
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] protected readonly IRobustRandom Random = default!;
    [Dependency] protected readonly ISharedAdminLogManager Log = default!;
    [Dependency] private readonly EntityLookupSystem _lookup = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;

    private ISawmill _sawmill = default!;

    /// <inheritdoc/>
    public override void Initialize()
    {
        SubscribeLocalEvent<NecroobeliskComponent, EntityUnpausedEvent>(OnNecroobeliskUnpause);

        _sawmill = Logger.GetSawmill("necroobelisk");
    }



    private void OnNecroobeliskUnpause(EntityUid uid, NecroobeliskComponent component, ref EntityUnpausedEvent args)
    {
        component.NextPulseTime += args.PausedTime;
        component.NextCheckTimeSanity += args.PausedTime;
        Dirty(component);
    }


    public void DoSanityCheck(EntityUid uid, NecroobeliskComponent? component = null)
    {
        if (!Resolve(uid, ref component))
            return;

        if (!Timing.IsFirstTimePredicted)
            return;

        // _appearance.SetData(uid, RevenantVisuals.Harvesting, true);
        var victims = _lookup.GetEntitiesInRange(uid, component.RangeSanity);

        foreach(var victinUID in victims)
        {
            if (EntityManager.HasComponent<SanityComponent>(victinUID))
            {
                if (!EntityManager.TryGetComponent<SanityComponent>(victinUID, out var xform))
                return;


                //_popup.PopupEntity(Loc.GetString(a.ToString()), victinUID);
                if(component.Active >= 1)
                {
                    xform.lvl -= 1;
                }
                else
                {
                    var ev = new NecroobeliskCheckStateEvent(Timing.CurTime);
                    RaiseLocalEvent(uid, ref ev);
                    return;
                }

                if (xform.lvl <= 0)
                {
                    var ev2 = new SanityCheckEvent(victinUID);
                    RaiseLocalEvent(uid, ref ev2);
                }
            }
        }
        component.NextCheckTimeSanity = Timing.CurTime + component.CheckDurationSanity;

    }

    public void DoNecroobeliskPulse(EntityUid uid, NecroobeliskComponent? component = null)
    {
        if (!Resolve(uid, ref component))
            return;

        if (!Timing.IsFirstTimePredicted)
            return;

        component.Pulselvl += 1;

        DebugTools.Assert(component.MinPulseLength > TimeSpan.FromSeconds(3)); // this is just to prevent lagspikes mispredicting pulses
        var variation = Random.NextFloat(-component.PulseVariation, component.PulseVariation) + 1;
        component.NextPulseTime = Timing.CurTime + GetPulseLength(component) * variation;

        if (_net.IsServer)
            _sawmill.Info($"Performing Necroobelisk pulse. Entity: {ToPrettyString(uid)}");

        Log.Add(LogType.Anomaly, LogImpact.Medium, $"Necroobelisk {ToPrettyString(uid)}.");
        if (_net.IsServer)
            Audio.PlayPvs("/Audio/DeadSpace/Necromorfs/Obelisk2.ogg", uid, AudioParams.Default.WithVariation(0.2f).WithVolume(15f));

        var ev1 = new NecroobeliskSpawnArmyEvent();
        RaiseLocalEvent(uid, ref ev1);

        var ev = new NecroobeliskPulseEvent();
        RaiseLocalEvent(uid, ref ev);
    }

    /// </remarks>
    /// <param name="component"></param>
    /// <returns>The length of time as a TimeSpan, not including random variation.</returns>
    public TimeSpan GetPulseLength(NecroobeliskComponent component)
    {
        DebugTools.Assert(component.MaxPulseLength > component.MinPulseLength);
        return (component.MaxPulseLength - component.MinPulseLength) * 1 + component.MinPulseLength;
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var necroobeliskQuery = EntityQueryEnumerator<NecroobeliskComponent>();
        while (necroobeliskQuery.MoveNext(out var ent, out var necroobelisk))
        {
            // if the stability is under the death threshold,
            // update it every second to start killing it slowly.
            if (Timing.CurTime > necroobelisk.NextPulseTime)
            {
                DoNecroobeliskPulse(ent, necroobelisk);
            }
            if (Timing.CurTime > necroobelisk.NextCheckTimeSanity)
            {
                DoSanityCheck(ent, necroobelisk);
            }
        }

    }

}
