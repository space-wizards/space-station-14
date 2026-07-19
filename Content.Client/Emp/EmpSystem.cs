using Content.Shared.Emp;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Client.Emp;

public sealed partial class EmpSystem : SharedEmpSystem
{
    private static readonly EntityTimerId EffectTimer = new("effect");

    [Dependency] private IRobustRandom _random = default!;
    [Dependency] private EntityTimerSystem _timers = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<EmpDisabledComponent, ComponentStartup>(OnStartup);
        SubscribeLocalEvent<EmpDisabledComponent, EntityTimerEvent>(OnTimer);
    }

    private void OnStartup(Entity<EmpDisabledComponent> ent, ref ComponentStartup args)
    {
        // EmpPulseEvent.Affected will spawn the first visual effect directly when the emp is used
        ent.Comp.TargetTime = Timing.CurTime + _random.NextFloat(0.8f, 1.2f) * ent.Comp.EffectCooldown;
        _timers.SetTimerAt(ent, EffectTimer, ent.Comp.TargetTime);
    }

    private void OnTimer(Entity<EmpDisabledComponent> ent, ref EntityTimerEvent args)
    {
        if (args.Id != EffectTimer)
            return;

        ent.Comp.TargetTime = args.FiredAt + _random.NextFloat(0.8f, 1.2f) * ent.Comp.EffectCooldown;
        _timers.SetTimerAt(ent, EffectTimer, ent.Comp.TargetTime);
        Spawn(EmpDisabledEffectPrototype, Transform(ent).Coordinates);
    }
}
