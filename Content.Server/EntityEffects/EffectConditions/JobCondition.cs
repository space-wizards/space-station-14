using System.Linq;
using Content.Shared.EntityEffects;
using Content.Shared.Localizations;
using Content.Shared.Mind;
using Content.Shared.Mind.Components;
using Content.Shared.Roles;
using Content.Shared.Roles.Jobs;
using Robust.Shared.Prototypes;

namespace Content.Server.EntityEffects.EffectConditions;

public sealed partial class JobCondition : EntityEffectCondition
{
    private const string SawmillName = "job_condition";
    private ISawmill Sawmill => _log ??= IoCManager.Resolve<ILogManager>().GetSawmill(SawmillName);
    private ISawmill? _log;

    [DataField(required: true)] public List<ProtoId<JobPrototype>> Job;

    public override bool Condition(EntityEffectBaseArgs args)
    {
        args.EntityManager.TryGetComponent<MindContainerComponent>(args.TargetEntity, out var mindContainer);

        if ( mindContainer is null
             || !args.EntityManager.TryGetComponent<MindComponent>(mindContainer.Mind, out var mind))
            return false;

        foreach (var roleId in mind.MindRoles)
        {
            if(!args.EntityManager.HasComponent<JobRoleComponent>(roleId))
                continue;

            if (!args.EntityManager.TryGetComponent<MindRoleComponent>(roleId, out var mindRole))
            {
                Sawmill.Error($"Encountered job mind role entity {roleId} without a {nameof(MindRoleComponent)}");
                continue;
            }

            if (mindRole.JobPrototype == null)
            {
                Sawmill.Error($"Encountered job mind role entity {roleId} without a {nameof(JobPrototype)}");
                continue;
            }

            if (Job.Contains(mindRole.JobPrototype.Value))
                return true;
        }

        return false;
    }

    public override string GuidebookExplanation(IPrototypeManager prototype)
    {
        var localizedNames = Job.Select(jobId => prototype.Index(jobId).LocalizedName).ToList();
        return Loc.GetString("reagent-effect-condition-guidebook-job-condition", ("job", ContentLocalizationManager.FormatListToOr(localizedNames)));
    }
}
