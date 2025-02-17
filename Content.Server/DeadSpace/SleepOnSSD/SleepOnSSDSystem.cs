// Мёртвый Космос, Licensed under custom terms with restrictions on public hosting and commercial use, full text: https://raw.githubusercontent.com/dead-space-server/space-station-14-fobos/master/LICENSE.TXT

using Content.Shared.Bed.Sleep;
using Content.Shared.Mind.Components;
using Content.Shared.Mobs.Systems;
using Content.Shared.NPC;
using Content.Shared.SSDIndicator;
using Robust.Shared.Player;

namespace Content.Server.DeadSpace.SleepOnSSD;

public sealed class SleepOnSSDSystem : EntitySystem
{
    [Dependency] private readonly SleepingSystem _sleepingSystem = default!;
    [Dependency] private readonly MobStateSystem _mobStateSystem = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<SleepOnSSDComponent, PlayerDetachedEvent>(OnPlayerDetached);
    }

    private void OnPlayerDetached(EntityUid ent, SleepOnSSDComponent comp, PlayerDetachedEvent ev)
    {
        if (Terminating(ent))
            return;

        if (_mobStateSystem.IsAlive(ent)
            && !HasComp<ActiveNPCComponent>(ent)
            && HasComp<SSDIndicatorComponent>(ent)
            && TryComp<MindContainerComponent>(ent, out var mindContainer)
            && mindContainer.ShowExamineInfo)
        {
            _sleepingSystem.TrySleeping(ent);
        }
    }
}
