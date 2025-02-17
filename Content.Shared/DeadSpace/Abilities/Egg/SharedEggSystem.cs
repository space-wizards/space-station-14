// Мёртвый Космос, Licensed under custom terms with restrictions on public hosting and commercial use, full text: https://raw.githubusercontent.com/dead-space-server/space-station-14-fobos/master/LICENSE.TXT

using Content.Shared.DeadSpace.Abilities.Egg.Components;
using Robust.Shared.Timing;
using Content.Shared.Zombies;
using Content.Shared.DeadSpace.Necromorphs.InfectionDead.Components;
using Content.Shared.Actions;

namespace Content.Shared.DeadSpace.Abilities.Egg;

public abstract class SharedEggSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly SharedActionsSystem _actionsSystem = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<EggComponent, ComponentInit>(OnComponentInit);
        SubscribeLocalEvent<EggComponent, ComponentShutdown>(OnShutdown);
        SubscribeLocalEvent<EggComponent, EntityUnpausedEvent>(OnEggUnpause);
        SubscribeLocalEvent<EggComponent, HatchActionEvent>(OnHatch);
    }

    private void OnShutdown(EntityUid uid, EggComponent component, ComponentShutdown args)
    {
        _actionsSystem.RemoveAction(uid, component.ActionHatchEntity);
    }

    private void OnComponentInit(EntityUid uid, EggComponent component, ComponentInit args)
    {
        if (component.CanHatch)
            _actionsSystem.AddAction(uid, ref component.ActionHatchEntity, component.ActionHatch, uid);

        component.TimeUntilSpawn = _timing.CurTime + TimeSpan.FromSeconds(component.Duration);
        component.TimeUntilPlaySound = TimeSpan.FromSeconds(component.DurationPlayEggSound) + _timing.CurTime;
    }

    private void OnHatch(EntityUid uid, EggComponent component, HatchActionEvent args)
    {
        if (args.Handled)
            return;

        var eggSpawnEvent = new EggSpawnEvent();
        RaiseLocalEvent(uid, ref eggSpawnEvent);

        args.Handled = true;
    }

    private void OnEggUnpause(EntityUid uid, EggComponent component, ref EntityUnpausedEvent args)
    {
        component.TimeUntilSpawn += args.PausedTime;
        component.TimeUntilPlaySound += args.PausedTime;
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);
        var curTime = _timing.CurTime;

        var eggQuery = EntityQueryEnumerator<EggComponent>();
        while (eggQuery.MoveNext(out var uid, out var comp))
        {
            if (curTime > comp.TimeUntilSpawn)
            {
                var eggSpawnEvent = new EggSpawnEvent();
                RaiseLocalEvent(uid, ref eggSpawnEvent);
            }

            if (curTime > comp.TimeUntilPlaySound)
            {
                var playEggSoundEvent = new PlayEggSoundEvent();
                RaiseLocalEvent(uid, ref playEggSoundEvent);
            }
        }
    }

    public bool IsInfectPossible(EntityUid target)
    {
        if (HasComp<ImmunEggComponent>(target))
            return false;

        if (HasComp<EggComponent>(target))
            return false;

        if (HasComp<InfectionDeadComponent>(target) || HasComp<NecromorfComponent>(target))
            return false;

        if (HasComp<ZombieComponent>(target) || HasComp<PendingZombieComponent>(target))
            return false;

        return true;
    }

    public void Postpone(float duration, EggComponent component)
    {
        var oldTime = component.TimeUntilSpawn;
        component.TimeUntilSpawn = _timing.CurTime + TimeSpan.FromSeconds(duration);

        if (component.TimeUntilSpawn <= _timing.CurTime)
            component.TimeUntilSpawn = oldTime;
    }
}
