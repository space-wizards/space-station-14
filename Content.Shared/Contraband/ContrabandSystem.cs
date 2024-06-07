using System.Linq;
using Content.Shared.Access.Systems;
using Content.Shared.Examine;
using Content.Shared.Localizations;
using Content.Shared.Roles;
using Robust.Shared.Prototypes;

namespace Content.Shared.Contraband;

/// <summary>
/// This handles showing examine messages for contraband-marked items.
/// </summary>
public sealed class ContrabandSystem : EntitySystem
{
    [Dependency] private readonly IPrototypeManager _proto = default!;
    [Dependency] private readonly SharedIdCardSystem _id = default!;

    /// <inheritdoc/>
    public override void Initialize()
    {
        SubscribeLocalEvent<ContrabandComponent, ExaminedEvent>(OnExamined);
    }

    private void OnExamined(EntityUid uid, ContrabandComponent component, ExaminedEvent args)
    {
        // two strings:
        // one, the actual informative 'this is restricted'
        // then, the 'you can/shouldn't carry this around' based on the ID the user is wearing

        using (args.PushGroup(nameof(ContrabandComponent)))
        {
            if (component is { Severity: ContrabandSeverity.Restricted, AllowedDepartments: not null })
            {
                // TODO shouldn't department prototypes have a localized name instead of just using the ID for this?
                var list = ContentLocalizationManager.FormatList(component.AllowedDepartments.Select(p => Loc.GetString($"department-{p.Id}")).ToList());

                // department restricted text
                args.PushMarkup(Loc.GetString("contraband-examine-text-Restricted-department", ("departments", list)));
            }
            else
            {
                args.PushMarkup(Loc.GetString($"contraband-examine-text-{component.Severity.ToString()}"));
            }

            // text based on ID card
            List<ProtoId<DepartmentPrototype>>? departments = null;
            if (_id.TryFindIdCard(args.Examiner, out var id))
            {
                departments = id.Comp.JobDepartments;
            }

            // either its fully restricted, you have no departments, or your departments dont intersect with the restricted departments
            if (component.AllowedDepartments is null
                || departments is null
                || !departments.Intersect(component.AllowedDepartments).Any())
            {
                args.PushMarkup(Loc.GetString("contraband-examine-text-avoid-carrying-around"));
                return;
            }

            // otherwise fine to use :tm:
            args.PushMarkup(Loc.GetString("contraband-examine-text-in-the-clear"));
        }
    }
}
