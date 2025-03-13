using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Content.Shared.Mind;
using Content.Shared.Players;
using Content.Shared.Players.PlayTimeTracking;
using Robust.Shared.GameObjects;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Shared.Roles.Jobs;

/// <summary>
///     Handles the job data on mind entities.
/// </summary>
public abstract class SharedJobSystem : EntitySystem
{
    [Dependency] private readonly SharedPlayerSystem _playerSystem = default!;
    [Dependency] private readonly IPrototypeManager _prototypes = default!;
    [Dependency] private readonly SharedRoleSystem _roles = default!;
    [Dependency] private readonly SharedMindSystem _mindSystem = default!;

    private readonly Dictionary<string, string> _inverseTrackerLookup = new();

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<PrototypesReloadedEventArgs>(OnProtoReload);
        SetupTrackerLookup();
    }

    private void OnProtoReload(PrototypesReloadedEventArgs obj)
    {
        if (obj.WasModified<JobPrototype>())
            SetupTrackerLookup();
    }

    private void SetupTrackerLookup()
    {
        _inverseTrackerLookup.Clear();

        // This breaks if you have N trackers to 1 JobId but future concern.
        foreach (var job in _prototypes.EnumeratePrototypes<JobPrototype>())
        {
            _inverseTrackerLookup.Add(job.PlayTimeTracker, job.ID);
        }
        // imp
        foreach (var antag in _prototypes.EnumeratePrototypes<AntagPrototype>())
        {
            _inverseTrackerLookup.Add(antag.PlayTimeTracker, antag.ID);
        } // end imp
    }

    /// <summary>
    /// Gets the corresponding Job Prototype to a <see cref="PlayTimeTrackerPrototype"/>
    /// </summary>
    /// <param name="trackerProto"></param>
    /// <returns></returns>
    public string GetJobPrototype(string trackerProto)
    {
        DebugTools.Assert(_prototypes.HasIndex<PlayTimeTrackerPrototype>(trackerProto));
        return _inverseTrackerLookup[trackerProto];
    }

    /// <summary>
    /// Tries to get the first corresponding department for this job prototype.
    /// </summary>
    public bool TryGetDepartment(string jobProto, [NotNullWhen(true)] out DepartmentPrototype? departmentPrototype)
    {
        // Not that many departments so we can just eat the cost instead of storing the inverse lookup.
        var departmentProtos = _prototypes.EnumeratePrototypes<DepartmentPrototype>().ToList();
        departmentProtos.Sort((x, y) => string.Compare(x.ID, y.ID, StringComparison.Ordinal));

        foreach (var department in departmentProtos)
        {
            if (department.Roles.Contains(jobProto))
            {
                departmentPrototype = department;
                return true;
            }
        }

        departmentPrototype = null;
        return false;
    }

    /// <summary>
    /// Like <see cref="TryGetDepartment"/> but ignores any non-primary departments.
    /// For example, with CE it will return Engineering but with captain it will
    /// not return anything, since Command is not a primary department.
    /// </summary>
    public bool TryGetPrimaryDepartment(string jobProto, [NotNullWhen(true)] out DepartmentPrototype? departmentPrototype)
    {
        // not sorting it since there should only be 1 primary department for a job.
        // this is enforced by the job tests.
        var departmentProtos = _prototypes.EnumeratePrototypes<DepartmentPrototype>();

        foreach (var department in departmentProtos)
        {
            if (department.Primary && department.Roles.Contains(jobProto))
            {
                departmentPrototype = department;
                return true;
            }
        }

        departmentPrototype = null;
        return false;
    }

    //imp addition
    public bool MindsHaveSameJobDept(EntityUid entID1, EntityUid entID2)
    {
        if (!_mindSystem.TryGetMind(entID1, out var mindId1, out _))
        {
            Log.Debug($"trygetmind for entid 1 {ToPrettyString(entID1)}  - failed, no Mind found");
            return false;
        }
        if (!_mindSystem.TryGetMind(entID2, out var mindId2, out _))
        {
            Log.Debug($"trygetmind for entid 2 {ToPrettyString(entID1)}  - failed, no Mind found");
            return false;
        }
        MindTryGetJob(mindId1, out var job1);
        MindTryGetJob(mindId2, out var job2);
        return JobsHaveSameDept(job1, job2);
    }

    public bool JobsHaveSameDept(JobPrototype? job1, JobPrototype? job2)
    {
        if (job1 == null || job2 == null)
            return false;
        TryGetPrimaryDepartment(job1.ID, out var dept1);
        TryGetPrimaryDepartment(job2.ID, out var dept2);
        if (dept1 == null || dept2 == null)
            return false;
        return (dept1.Equals(dept2));
    }

    public bool MindHasJobDept(EntityUid entID, JobPrototype? givenJob)
    {
        if (!_mindSystem.TryGetMind(entID, out var mindId, out _))
        {
            Log.Debug($"trygetmind for entid{ToPrettyString(entID)}  - failed, no Mind found");
            return false;
        }

        if (!MindTryGetJob(mindId, out var mindJob))
            return false;
        if (mindJob == null || givenJob == null)
            return false;
        TryGetPrimaryDepartment(mindJob.ID, out var dept1);
        TryGetPrimaryDepartment(givenJob.ID, out var dept2);
        if (dept1 == null || dept2 == null)
            return false;
        return (dept1.Equals(dept2));
    }

    //end imp additions

    public bool MindHasJobWithId(EntityUid? mindId, string prototypeId)
    {

        if (mindId is null)
            return false;

        _roles.MindHasRole<JobRoleComponent>(mindId.Value, out var role);

        if (role is null)
            return false;

        return role.Value.Comp1.JobPrototype == prototypeId;
    }

    public bool MindTryGetJob(
        [NotNullWhen(true)] EntityUid? mindId,
        [NotNullWhen(true)] out JobPrototype? prototype)
    {
        prototype = null;
        MindTryGetJobId(mindId, out var protoId);

        return _prototypes.TryIndex(protoId, out prototype) || prototype is not null;
    }

    public bool MindTryGetJobId(
        [NotNullWhen(true)] EntityUid? mindId,
        out ProtoId<JobPrototype>? job)
    {
        job = null;

        if (mindId is null)
            return false;

        if (_roles.MindHasRole<JobRoleComponent>(mindId.Value, out var role))
            job = role.Value.Comp1.JobPrototype;

        return job is not null;
    }

    /// <summary>
    ///     Tries to get the job name for this mind.
    ///     Returns unknown if not found.
    /// </summary>
    public bool MindTryGetJobName([NotNullWhen(true)] EntityUid? mindId, out string name)
    {
        if (MindTryGetJob(mindId, out var prototype))
        {
            name = prototype.LocalizedName;
            return true;
        }

        name = Loc.GetString("generic-unknown-title");
        return false;
    }

    /// <summary>
    ///     Tries to get the job name for this mind.
    ///     Returns unknown if not found.
    /// </summary>
    public string MindTryGetJobName([NotNullWhen(true)] EntityUid? mindId)
    {
        MindTryGetJobName(mindId, out var name);
        return name;
    }

    public bool CanBeAntag(ICommonSession player)
    {
        // If the player does not have any mind associated with them (e.g., has not spawned in or is in the lobby), then
        // they are eligible to be given an antag role/entity.
        if (_playerSystem.ContentData(player) is not { Mind: { } mindId })
            return true;

        if (!MindTryGetJob(mindId, out var prototype))
            return true;

        return prototype.CanBeAntag;
    }
}
