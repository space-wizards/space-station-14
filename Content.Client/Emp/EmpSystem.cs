using Content.Shared.Emp;
using Robust.Shared.Random;

namespace Content.Client.Emp;

public sealed class EmpSystem : SharedEmpSystem
{
    [Dependency] private readonly IRobustRandom _random = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<EmpDisabledComponent, ComponentStartup>(OnStartup);
    }

    private void OnStartup(Entity<EmpDisabledComponent> ent, ref ComponentStartup args)
    {
        // EmpPulseEvent.Affected will spawn the first visual effect directly when the emp is used
        ent.Comp.TargetTime = Timing.CurTime + _random.NextFloat(0.8f, 1.2f) * ent.Comp.EffectCooldown;
    }
    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<EmpDisabledComponent, TransformComponent>();
        while (query.MoveNext(out var uid, out var comp, out var transform))
        {
            if (Timing.CurTime > comp.TargetTime)
            {
                comp.TargetTime = Timing.CurTime + _random.NextFloat(0.8f, 1.2f) * comp.EffectCooldown;
                Spawn(EmpDisabledEffectPrototype, transform.Coordinates);
            }
        }
    }
}
