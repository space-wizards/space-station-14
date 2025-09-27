using Content.Shared.MassDriver.Components;
using Content.Shared.Throwing;
using Content.Shared.Power;
using Content.Shared.Power.EntitySystems;
using Content.Shared.Audio;
using Robust.Shared.Timing;

namespace Content.Shared.MassDriver.EntitySystems;

public abstract class SharedMassDriverSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly ThrowingSystem _throwing = default!;
    [Dependency] private readonly EntityLookupSystem _lookup = default!;
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
    [Dependency] private readonly SharedAmbientSoundSystem _audioSystem = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<MassDriverComponent, PowerChangedEvent>(OnPowerChanged);
    }

    #region Logic

    /// <summary>
    /// Handle power changing to disable or enable mass driver, so it can't work without power
    /// </summary>
    /// <param name="uid">Mass Driver</param>
    /// <param name="component">Mass Driver Component</param>
    /// <param name="args">Event arguments</param>
    private void OnPowerChanged(EntityUid uid, MassDriverComponent component, ref PowerChangedEvent args)
    {
        if (component.Mode != MassDriverMode.Auto)
            return;

        var hasComp = HasComp<ActiveMassDriverComponent>(uid);
        if (hasComp && !args.Powered)
            RemComp<ActiveMassDriverComponent>(uid);
        else if (!hasComp && args.Powered)
            EnsureComp<ActiveMassDriverComponent>(uid);
    }

    /// <summary>
    /// Update only active mass drivers, so we have it more optimized than conveyors
    /// </summary>
    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<ActiveMassDriverComponent, MassDriverComponent>();
        var entities = new HashSet<EntityUid>(); // Reuse this hashset for all drivers

        while (query.MoveNext(out var uid, out var activeMassDriver, out var massDriver))
        {
            if (_timing.CurTime < activeMassDriver.NextUpdateTime
                || (activeMassDriver.NextThrowTime != TimeSpan.Zero && _timing.CurTime < activeMassDriver.NextThrowTime))
                continue;

            activeMassDriver.NextUpdateTime = _timing.CurTime + activeMassDriver.UpdateDelay;
            Dirty(uid, massDriver);

            entities.Clear();
            _lookup.GetEntitiesIntersecting(uid, entities);

            // Remove anchored entities since we can't throw them
            entities.RemoveWhere(e => Transform(e).Anchored);
            int entitiesCount = entities.Count;

            if (entitiesCount == 0)
            {
                // Disable mass driver if we throw all entities
                if (activeMassDriver.NextThrowTime != TimeSpan.Zero)
                {
                    if (TryComp<AmbientSoundComponent>(uid, out var ambient))
                        _audioSystem.SetAmbience(uid, false, ambient);
                    activeMassDriver.NextThrowTime = TimeSpan.Zero;
                    _appearance.SetData(uid, MassDriverVisuals.Launching, false);
                    ChangePowerLoad(uid, massDriver, massDriver.MassDriverPowerLoad);
                    Dirty(uid, massDriver);
                }
                if (massDriver.Mode == MassDriverMode.Manual)
                    RemComp<ActiveMassDriverComponent>(uid);
                continue;
            }

            // If we find first entity, charge mass driver(wait n seconds setuped in ThrowDelay)
            if (activeMassDriver.NextThrowTime == TimeSpan.Zero)
            {
                activeMassDriver.NextThrowTime = _timing.CurTime + massDriver.ThrowDelay;
                Dirty(uid, massDriver);
                continue;
            }

            // Time to throw entities
            ChangePowerLoad(uid, massDriver, massDriver.LaunchPowerLoad);
            _appearance.SetData(uid, MassDriverVisuals.Launching, true);

            ThrowEntities(uid, massDriver, entities, entitiesCount);

            if (TryComp<AmbientSoundComponent>(uid, out var ambientSound))
                _audioSystem.SetAmbience(uid, true, ambientSound);
        }
    }

    /// <summary>
    /// Throws All entities in list.
    /// </summary>
    /// <param name="massDriver">Mass Driver</param>
    /// <param name="massDriverComponent">Mass Driver Component</param>
    /// <param name="targets">Targets List</param>
    /// <param name="targetCount">Count of target(added, because we can ignore some targets like anchored, etc.)</param>
    private void ThrowEntities(EntityUid massDriver, MassDriverComponent massDriverComponent, HashSet<EntityUid> targets, int targetCount)
    {
        var xform = Transform(massDriver);
        var throwing = xform.LocalRotation.ToWorldVec() * (massDriverComponent.CurrentThrowDistance - (massDriverComponent.ThrowCountDelta * (targets.Count - 1)));
        var direction = xform.Coordinates.Offset(throwing);
        var speed = massDriverComponent.Hacked ? massDriverComponent.HackedSpeedRewrite : massDriverComponent.CurrentThrowSpeed - (massDriverComponent.ThrowCountDelta * (targetCount - 1));

        foreach (var entity in targets)
            _throwing.TryThrow(entity, direction, speed);
    }

    public abstract void ChangePowerLoad(EntityUid uid, MassDriverComponent component, float powerLoad); // Server side implementation only

    #endregion

}
