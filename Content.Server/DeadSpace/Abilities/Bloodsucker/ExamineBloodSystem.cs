using Content.Shared.DeadSpace.Abilities.Bloodsucker.Components;
using Content.Shared.NPC.Systems;
using Content.Server.Body.Components;
using Content.Shared.DeadSpace.Abilities.Bloodsucker;
using Content.Server.DeadSpace.Abilities.Bloodsucker.Components;
using Content.Shared.Examine;
using Content.Shared.Chemistry.Components;
using Content.Server.DeadSpace.Abilities.Cocoon.Components;
using Content.Shared.Mobs.Systems;

namespace Content.Server.DeadSpace.Abilities.Bloodsucker;

public sealed partial class ExamineBloodSystem : SharedBloodsuckerSystem
{
    [Dependency] private readonly NpcFactionSystem _npcFaction = default!;
    [Dependency] private readonly MobStateSystem _mobState = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ExamineBloodComponent, ExaminedEvent>(OnExamine);
    }

    private void OnExamine(EntityUid uid, ExamineBloodComponent component, ExaminedEvent args)
    {
        var target = args.Examined;

        if (TryComp<CocoonComponent>(target, out var cocoonComp))
        {
            if (cocoonComp.Stomach.ContainedEntities.Count > 0)
            {
                var firstEntity = cocoonComp.Stomach.ContainedEntities[0];
                target = firstEntity;

                if (_mobState.IsDead(target))
                    args.PushMarkup(Loc.GetString(" Существо в коконе мёртвое."));
                if (_mobState.IsAlive(target))
                    args.PushMarkup(Loc.GetString(" Существо в коконе живое."));
                if (_mobState.IsCritical(target))
                    args.PushMarkup(Loc.GetString(" Существо в коконе в критическом состоянии."));
            }
        }

        if (HasComp<BloodsuckerComponent>(args.Examiner))
        {
            if (!_npcFaction.IsEntityFriendly(args.Examiner, target))
            {
                if (TryComp<BloodstreamComponent>(target, out var bloodstreamComponent) && bloodstreamComponent.BloodSolution != null)
                {
                    if (TryComp<SolutionComponent>(bloodstreamComponent.BloodSolution.Value, out var bloodSolution))
                    {
                        var bloodVolume = bloodSolution.Solution.Volume;
                        args.PushMarkup(Loc.GetString($"Содержит [color=red]{bloodVolume} крови[/color]."));
                    }
                }
            }
        }
    }
}
