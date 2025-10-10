using Content.Shared.Examine;
using Content.Shared.Rejuvenate;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Map;
using Robust.Shared.Timing;
using Robust.Shared.Network;
using Robust.Shared.Prototypes;

namespace Content.Shared.Emp;

public abstract class SharedEmpSystem : EntitySystem
{
    [Dependency] protected readonly IGameTiming Timing = default!;
    [Dependency] private readonly EntityLookupSystem _lookup = default!;
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;

    private HashSet<EntityUid> _entSet = new();

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<EmpDisabledComponent, ExaminedEvent>(OnExamine);
        SubscribeLocalEvent<EmpDisabledComponent, ComponentRemove>(OnRemove);
        SubscribeLocalEvent<EmpDisabledComponent, RejuvenateEvent>(OnRejuvenate);
    }

    public static readonly EntProtoId EmpPulseEffectPrototype = "EffectEmpPulse";
    public static readonly EntProtoId EmpDisabledEffectPrototype = "EffectEmpDisabled";
    public static readonly SoundSpecifier EmpSound = new SoundPathSpecifier("/Audio/Effects/Lightning/lightningbolt.ogg");

    /// <summary>
    /// Triggers an EMP pulse at the given location, by first raising an <see cref="EmpAttemptEvent"/>, then by raising <see cref="EmpPulseEvent"/> on all entities in range.
    /// </summary>
    /// <param name="coordinates">The location to trigger the EMP pulse at.</param>
    /// <param name="range">The range of the EMP pulse.</param>
    /// <param name="energyConsumption">The amount of energy consumed by the EMP pulse. In Joule.</param>
    /// <param name="duration">The duration of the EMP effects.</param>
    /// <param name="user">The player that caused the effect. Used for predicted audio.</param>
    public void EmpPulse(MapCoordinates mapCoordinates, float range, float energyConsumption, TimeSpan duration, EntityUid? user = null)
    {
        foreach (var uid in _lookup.GetEntitiesInRange(mapCoordinates, range))
        {
            TryEmpEffects(uid, energyConsumption, duration, user);
        }
        // TODO: replace with PredictedSpawn once it works with animated sprites
        if (_net.IsServer)
            Spawn(EmpPulseEffectPrototype, mapCoordinates);

        var coordinates = _transform.ToCoordinates(mapCoordinates);
        _audio.PlayPredicted(EmpSound, coordinates, user);
    }

    /// <summary>
    /// Triggers an EMP pulse at the given location, by first raising an <see cref="EmpAttemptEvent"/>, then a raising <see cref="EmpPulseEvent"/> on all entities in range.
    /// </summary>
    /// <param name="coordinates">The location to trigger the EMP pulse at.</param>
    /// <param name="range">The range of the EMP pulse.</param>
    /// <param name="energyConsumption">The amount of energy consumed by the EMP pulse.</param>
    /// <param name="duration">The duration of the EMP effects.</param>
    /// <param name="user">The player that caused the effect. Used for predicted audio.</param>
    public void EmpPulse(EntityCoordinates coordinates, float range, float energyConsumption, TimeSpan duration, EntityUid? user = null)
    {
        _entSet.Clear();
        _lookup.GetEntitiesInRange(coordinates, range, _entSet);
        foreach (var uid in _entSet)
        {
            TryEmpEffects(uid, energyConsumption, duration, user);
        }
        // TODO: replace with PredictedSpawn once it works with animated sprites
        if (_net.IsServer)
            Spawn(EmpPulseEffectPrototype, coordinates);

        _audio.PlayPredicted(EmpSound, coordinates, user);
    }

    /// <summary>
    /// Attempts to apply the effects of an EMP pulse onto an entity by first raising an <see cref="EmpAttemptEvent"/>, followed by raising a <see cref="EmpPulseEvent"/> on it.
    /// </summary>
    /// <param name="uid">The entity to apply the EMP effects on.</param>
    /// <param name="energyConsumption">The amount of energy consumed by the EMP.</param>
    /// <param name="duration">The duration of the EMP effects.</param>
    /// <param name="user">The player that caused the EMP. For prediction purposes.</param>
    /// <returns>If the entity was affected by the EMP.</returns>
    public bool TryEmpEffects(EntityUid uid, float energyConsumption, TimeSpan duration, EntityUid? user = null)
    {
        var attemptEv = new EmpAttemptEvent();
        RaiseLocalEvent(uid, ref attemptEv);
        if (attemptEv.Cancelled)
            return false;

        return DoEmpEffects(uid, energyConsumption, duration, user);
    }

    /// <summary>
    /// Applies the effects of an EMP pulse onto an entity by raising a <see cref="EmpPulseEvent"/> on it.
    /// </summary>
    /// <param name="uid">The entity to apply the EMP effects on.</param>
    /// <param name="energyConsumption">The amount of energy consumed by the EMP.</param>
    /// <param name="duration">The duration of the EMP effects.</param>
    /// <param name="user">The player that caused the EMP. For prediction purposes.</param>
    /// <returns>If the entity was affected by the EMP.</returns>
    public bool DoEmpEffects(EntityUid uid, float energyConsumption, TimeSpan duration, EntityUid? user = null)
    {
        var ev = new EmpPulseEvent(energyConsumption, false, false, duration, user);
        RaiseLocalEvent(uid, ref ev);

        // TODO: replace with PredictedSpawn once it works with animated sprites
        if (ev.Affected && _net.IsServer)
            Spawn(EmpDisabledEffectPrototype, Transform(uid).Coordinates);

        if (!ev.Disabled)
            return ev.Affected;

        var disabled = EnsureComp<EmpDisabledComponent>(uid);
        disabled.DisabledUntil = Timing.CurTime + duration;
        Dirty(uid, disabled);

        return ev.Affected;
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var curTime = Timing.CurTime;
        var query = EntityQueryEnumerator<EmpDisabledComponent>();
        while (query.MoveNext(out var uid, out var comp))
        {
            if (curTime < comp.DisabledUntil)
                continue;

            RemComp<EmpDisabledComponent>(uid);
        }
    }

    private void OnExamine(Entity<EmpDisabledComponent> ent, ref ExaminedEvent args)
    {
        args.PushMarkup(Loc.GetString("emp-disabled-comp-on-examine"));
    }

    private void OnRemove(Entity<EmpDisabledComponent> ent, ref ComponentRemove args)
    {
        var ev = new EmpDisabledRemovedEvent();
        RaiseLocalEvent(ent, ref ev);
    }

    private void OnRejuvenate(Entity<EmpDisabledComponent> ent, ref RejuvenateEvent args)
    {
        RemCompDeferred<EmpDisabledComponent>(ent);
    }
}

/// <summary>
/// Raised on an entity before <see cref="EmpPulseEvent"/>. Cancel this to prevent the emp event being raised.
/// </summary>
[ByRefEvent]
public record struct EmpAttemptEvent(bool Cancelled);

/// <summary>
/// Raised on an entity when it gets hit by an EMP Pulse.
/// </summary>
/// <param name="EnergyConsumption">The amount of energy to remove from batteries. In Joule.</param>
/// <param name="Affected">Set this is true in the subscription to spawn a visual effect at the entity's location.</param>
/// <param name="Disabled">Set this to ture in the subscription to add <see cref="EmpDisabledComponent"/> to the entity.</param>
/// <param name="Duration">The duration the entity will be disabled.</param>
/// <param name="User">The player that caused the EMP. For prediction purposes.</param>

[ByRefEvent]
public record struct EmpPulseEvent(float EnergyConsumption, bool Affected, bool Disabled, TimeSpan Duration, EntityUid? User);

/// <summary>
/// Raised on an entity after <see cref="EmpDisabledComponent"/> is removed.
/// </summary>
[ByRefEvent]
public record struct EmpDisabledRemovedEvent();
