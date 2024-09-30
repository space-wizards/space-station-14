using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Content.Shared.Players;
using Content.Shared.Players.PlayTimeTracking;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Shared.Roles.Jobs;

/// <summary>
///     Handles the job data on mind entities.
/// </summary>
public abstract class SharedJobSystem : EntitySystem
{
    [Dependency] private readonly IPrototypeManager _prototypes = default!;
    [Dependency] private readonly SharedPlayerSystem _playerSystem = default!;
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

    public bool MindHasJobWithId(EntityUid? mindId, string prototypeId)
    {
        return CompOrNull<JobComponent>(mindId)?.Prototype == prototypeId;
    }

    public bool MindTryGetJob(
        [NotNullWhen(true)] EntityUid? mindId,
        [NotNullWhen(true)] out JobComponent? comp,
        [NotNullWhen(true)] out JobPrototype? prototype)
    {
        comp = null;
        prototype = null;

        return TryComp(mindId, out comp) &&
               comp.Prototype != null &&
               _prototypes.TryIndex(comp.Prototype, out prototype);
    }

    public bool MindTryGetJobId([NotNullWhen(true)] EntityUid? mindId, out ProtoId<JobPrototype>? job)
    {
        if (!TryComp(mindId, out JobComponent? comp))
        {
            job = null;
            return false;
        }

        job = comp.Prototype;
        return true;
    }

    /// <summary>
    ///     Tries to get the job name for this mind.
    ///     Returns unknown if not found.
    /// </summary>
    public bool MindTryGetJobName([NotNullWhen(true)] EntityUid? mindId, out string name)
    {
        if (MindTryGetJob(mindId, out _, out var prototype))
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

        if (!MindTryGetJob(mindId, out _, out var prototype))
            return true;

        return prototype.CanBeAntag;
    }
}
