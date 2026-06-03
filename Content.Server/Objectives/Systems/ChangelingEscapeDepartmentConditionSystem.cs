using System.Diagnostics;
using System.Linq;
using Content.Server.Objectives.Components;
using Content.Server.Shuttles.Systems;
using Content.Shared.Access.Systems;
using Content.Shared.Changeling.Systems;
using Content.Shared.Cuffs;
using Content.Shared.Mind;
using Content.Shared.Objectives.Components;
using Content.Shared.Roles.Jobs;
using Robust.Shared.Random;

namespace Content.Server.Objectives.Systems;

/// <summary>
/// Makes it so that <see cref="ChangelingEscapeDepartmentConditionComponent"/> works.
/// </summary>
public sealed partial class ChangelingEscapeDepartmentConditionSystem : EntitySystem
{
    [Dependency] private EmergencyShuttleSystem _emergencyShuttle = default!;
    [Dependency] private SharedChangelingIdentitySystem _changelingIdentity = default!;
    [Dependency] private SharedCuffableSystem _cuffable = default!;
    [Dependency] private SharedIdCardSystem _idCard = default!;
    [Dependency] private SharedMindSystem _mind = default!;
    [Dependency] private TargetObjectiveSystem _target = default!;
    [Dependency] private SharedJobSystem _job = default!;
    [Dependency] private IRobustRandom _random = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ChangelingEscapeDepartmentConditionComponent, ObjectiveGetProgressEvent>(OnGetProgress);
        SubscribeLocalEvent<ChangelingEscapeDepartmentConditionComponent, ObjectiveAfterAssignEvent>(OnAfterAssign);
    }

    private void OnAfterAssign(Entity<ChangelingEscapeDepartmentConditionComponent> ent, ref ObjectiveAfterAssignEvent args)
    {
        if (!_target.GetTarget(ent, out var target))
            return;

        // Does our target have a valid job?
        if (!_job.MindTryGetJobId(target.Value, out var jobPrototype) || !jobPrototype.HasValue)
            return;

        // We get the department of the job for our target, prioritizing the secondary departments (such as Command)
        // We do this to allow the objective to roll Command.
        // Otherwise RD/HoS/QM would return Science/Security/Cargo instead of Command.
        if (!_job.TryGetSecondaryDepartmentsOrFallback(jobPrototype.Value, out var departmentPrototypes))
            return;

        Debug.Assert(departmentPrototypes.Count > 0, $"Attempted to assign a job without departments to objective {MetaData(ent).EntityPrototype}!");

        // Get a random department.
        // This is all done on the server, so no need to worry about mispredictions.
        ent.Comp.Department = _random.Pick(departmentPrototypes);
    }

    private void OnGetProgress(Entity<ChangelingEscapeDepartmentConditionComponent> ent, ref ObjectiveGetProgressEvent args)
    {
        args.Progress = GetProgress(ent, (args.MindId, args.Mind));
    }

    private float GetProgress(Entity<ChangelingEscapeDepartmentConditionComponent> ent, Entity<MindComponent> mind)
    {
        var ownedEntity = mind.Comp.OwnedEntity;
        if (ownedEntity == null || _mind.IsCharacterDeadIc(mind))
            return 0f;

        // Check 1: Must have transformed into a target with a job that is in the department
        if (!_changelingIdentity.TryGetCurrentIdentityData(ownedEntity.Value, out var identityData))
            return 0f; // this should not happen

        // Our identity must have a job.
        var job = identityData.OriginalJob;
        if (!job.HasValue)
            return 0f;

        if (!_job.JobIsInDepartment(job.Value, ent.Comp.Department))
            return 0f; // The job of our identity is not in the target department.

        // Check 2: Must escape alive.
        if (!_emergencyShuttle.IsTargetEscaping(ownedEntity.Value))
            return 0.5f;

        if (_cuffable.IsCuffed(ownedEntity.Value))
            return 0.5f;

        // Check 3: Must wear an ID card with the target's name on it.
        if (!_idCard.TryFindIdCard(ownedEntity.Value, out var idCard))
            return 0.75f; // is not wearing an id

        if (idCard.Comp.JobDepartments.All(k => k.Id != ent.Comp.Department))
            return 0.75f; // found ID is not of the correct department.

        return 1f;
    }
}
