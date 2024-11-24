using System.Linq;
using Content.Shared.Access.Systems;
using Content.Shared.CCVar;
using Content.Shared.Examine;
using Content.Shared.Localizations;
using Content.Shared.Roles;
using Robust.Shared.Configuration;
using Robust.Shared.Prototypes;

namespace Content.Shared.Contraband;

/// <summary>
/// This handles showing examine messages for contraband-marked items.
/// </summary>
public sealed class ContrabandSystem : EntitySystem
{
    [Dependency] private readonly IConfigurationManager _configuration = default!;
    [Dependency] private readonly IPrototypeManager _proto = default!;
    [Dependency] private readonly SharedIdCardSystem _id = default!;

    private bool _contrabandExamineEnabled;

    /// <inheritdoc/>
    public override void Initialize()
    {
        SubscribeLocalEvent<ContrabandComponent, ExaminedEvent>(OnExamined);

        Subs.CVar(_configuration, CCVars.ContrabandExamine, SetContrabandExamine, true);
    }

    private void SetContrabandExamine(bool val)
    {
        _contrabandExamineEnabled = val;
    }

    public void CopyDetails(EntityUid uid, ContrabandComponent other, ContrabandComponent? contraband = null)
    {
        if (!Resolve(uid, ref contraband))
            return;

        contraband.Severity = other.Severity;
        contraband.AllowedDepartments = other.AllowedDepartments;
        contraband.AllowedJobs = other.AllowedJobs;
        Dirty(uid, contraband);
    }

    private void OnExamined(Entity<ContrabandComponent> ent, ref ExaminedEvent args)
    {
        if (!_contrabandExamineEnabled)
            return;

        // two strings:
        // one, the actual informative 'this is restricted'
        // then, the 'you can/shouldn't carry this around' based on the ID the user is wearing

        using (args.PushGroup(nameof(ContrabandComponent)))
        {
            var severity = _proto.Index(ent.Comp.Severity);
            var jobList = new List<string>();
            var departmentList = new List<string>();
            if (severity.ShowDepartmentsAndJobs)
            {
                if (ent.Comp is { AllowedJobs: not null })
                {
                    jobList = ent.Comp.AllowedJobs.Select(p => Loc.GetString($"{p.Id}")).ToList();
                }

                // TODO shouldn't department prototypes have a localized name instead of just using the ID for this?

                if (ent.Comp is { AllowedDepartments: not null })
                {
                    departmentList = ent.Comp.AllowedDepartments.Select(p => Loc.GetString($"department-{p.Id}")).ToList();
                }

                //creating a combined list of jobs and departments for the restricted text
                departmentList.AddRange(jobList);

                var list = ContentLocalizationManager.FormatList(departmentList);

                // department restricted text
                args.PushMarkup(Loc.GetString("contraband-examine-text-Restricted-department", ("departments", list)));
            }
            else
            {
                args.PushMarkup(Loc.GetString(severity.ExamineText));
            }


            // text based on ID card
            List<ProtoId<DepartmentPrototype>>? departments = null;
            var jobId = "";

            if (_id.TryFindIdCard(args.Examiner, out var id))
            {
                departments = id.Comp.JobDepartments;
                if (id.Comp.LocalizedJobTitle is not null)
                {
                    jobId = id.Comp.LocalizedJobTitle;
                }
            }

            var localizedContrabandAllowedJobs = "";
            if (ent.Comp.AllowedJobs is not null)
            {
                //grabbing the names of each job in the allowed jobs field
                foreach (var job in ent.Comp.AllowedJobs)
                {
                    //note this Id field means the name of the job, and is
                    //different from the "id" we're getting the examiner's job from (their Id card)
                    localizedContrabandAllowedJobs += (job.Id);
                }
                localizedContrabandAllowedJobs = Loc.GetString(localizedContrabandAllowedJobs);
            }

            // either its fully restricted, you have no departments, or your departments and job don't intersect with the restricted departments and jobs
            if (ent.Comp.AllowedDepartments is null || departments is null
                    || (!departments.Intersect(ent.Comp.AllowedDepartments).Any() && !localizedContrabandAllowedJobs.Contains(jobId)))
            {
                args.PushMarkup(Loc.GetString("contraband-examine-text-avoid-carrying-around"));
                return;
            }

            // otherwise fine to use :tm:
            args.PushMarkup(Loc.GetString("contraband-examine-text-in-the-clear"));
        }
    }
}
