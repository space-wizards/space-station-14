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
            if (severity.ShowDepartments && ent.Comp is { AllowedDepartments: not null })
            {
                // TODO shouldn't department prototypes have a localized name instead of just using the ID for this?
                var list = ContentLocalizationManager.FormatList(ent.Comp.AllowedDepartments.Select(p => Loc.GetString($"department-{p.Id}")).ToList());

                // department restricted text
                args.PushMarkup(Loc.GetString("contraband-examine-text-Restricted-department", ("departments", list)));
            }
            else
            {
                args.PushMarkup(Loc.GetString(severity.ExamineText));
            }

            // text based on ID card
            List<ProtoId<DepartmentPrototype>>? departments = null;
            if (_id.TryFindIdCard(args.Examiner, out var id))
            {
                departments = id.Comp.JobDepartments;
            }

            // either its fully restricted, you have no departments, or your departments dont intersect with the restricted departments
            if (ent.Comp.AllowedDepartments is null
                || departments is null
                || !departments.Intersect(ent.Comp.AllowedDepartments).Any())
            {
                args.PushMarkup(Loc.GetString("contraband-examine-text-avoid-carrying-around"));
                return;
            }

            // otherwise fine to use :tm:
            args.PushMarkup(Loc.GetString("contraband-examine-text-in-the-clear"));
        }
    }
}
