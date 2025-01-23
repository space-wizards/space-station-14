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
            var localizedDepartments = ent.Comp.AllowedDepartments.Select(p => Loc.GetString("contraband-department-plural", ("department", Loc.GetString(_proto.Index(p).Name))));
            var localizedJobs = ent.Comp.AllowedJobs.Select(p => Loc.GetString("contraband-job-plural", ("job", _proto.Index(p).LocalizedName)));

            var severity = _proto.Index(ent.Comp.Severity);
            if (severity.ShowDepartmentsAndJobs)
            {
                //creating a combined list of jobs and departments for the restricted text
                var list = ContentLocalizationManager.FormatList(localizedDepartments.Concat(localizedJobs).ToList());

                // department restricted text
                args.PushMarkup(Loc.GetString("contraband-examine-text-Restricted-department", ("departments", list)));
            }
            else
            {
                args.PushMarkup(Loc.GetString(severity.ExamineText));
            }

            // text based on ID card
            List<ProtoId<DepartmentPrototype>> departments = new();
            var jobId = "";

            if (_id.TryFindIdCard(args.Examiner, out var id))
            {
                departments = id.Comp.JobDepartments;
                if (id.Comp.LocalizedJobTitle is not null)
                {
                    jobId = id.Comp.LocalizedJobTitle;
                }
            }

            // for the jobs we compare the localized string in case you use an agent ID or custom job name that is not a prototype
            if (departments.Intersect(ent.Comp.AllowedDepartments).Any()
                || localizedJobs.Contains(jobId))
            {
                // you are allowed to use this!
                args.PushMarkup(Loc.GetString("contraband-examine-text-in-the-clear"));
            }
            else
            {
                // straight to jail!
                args.PushMarkup(Loc.GetString("contraband-examine-text-avoid-carrying-around"));
            }
        }
    }
}
