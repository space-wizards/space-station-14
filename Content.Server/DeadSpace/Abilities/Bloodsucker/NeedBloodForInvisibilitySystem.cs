using Robust.Shared.Timing;
using Content.Shared.DeadSpace.Abilities.Invisibility;
using Content.Shared.DeadSpace.Abilities.Invisibility.Components;
using Content.Shared.DeadSpace.Abilities.Bloodsucker.Components;
using Content.Shared.DeadSpace.Abilities.Bloodsucker;

namespace Content.Server.DeadSpace.Abilities.Bloodsucker;

public sealed partial class NeededBloodForInvisibilitySystem : SharedBloodsuckerSystem
{
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly SharedInvisibilitySystem _invis = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<NeededBloodForInvisibilityComponent, ComponentInit>(OnComponentInit);
        SubscribeLocalEvent<NeededBloodForInvisibilityComponent, EntityUnpausedEvent>(OnUnpause);
    }

    private void OnComponentInit(EntityUid uid, NeededBloodForInvisibilityComponent component, ComponentInit args)
    {
        component.NextTick = _timing.CurTime + TimeSpan.FromSeconds(1);
    }

    private void OnUnpause(EntityUid uid, NeededBloodForInvisibilityComponent component, ref EntityUnpausedEvent args)
    {
        component.NextTick += args.PausedTime;
        Dirty(uid, component);
    }
    public override void Update(float frameTime)
    {
        base.Update(frameTime);


        var invisibility = EntityQueryEnumerator<BloodsuckerComponent, InvisibilityComponent, NeededBloodForInvisibilityComponent>();
        while (invisibility.MoveNext(out var uid, out var blood, out var invis, out var needBlood))
        {
            if (needBlood.NextTick > _timing.CurTime)
                continue;

            if (invis.IsInvisible)
            {
                if (blood.CountReagent - needBlood.CostPerSecond > 0)
                {
                    SetReagentCount(uid, -needBlood.CostPerSecond, blood);
                    needBlood.NextTick = _timing.CurTime + TimeSpan.FromSeconds(1);
                }
                else
                {
                    _invis.TogleInvisibility(uid, invis);
                }
            }
        }
    }
}
