using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Content.Shared.Players.PlayTimeTracking;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Shared.Roles;

public abstract class SharedJobSystem : EntitySystem
{
    [Dependency] private readonly IPrototypeManager _protoManager = default!;
    private readonly Dictionary<string, string> _inverseTrackerLookup = new();

    public override void Initialize()
    {
        base.Initialize();
        _protoManager.PrototypesReloaded += OnProtoReload;
        SetupTrackerLookup();
    }

    public override void Shutdown()
    {
        base.Shutdown();
        _protoManager.PrototypesReloaded -= OnProtoReload;
        _inverseTrackerLookup.Clear();
    }

    private void OnProtoReload(PrototypesReloadedEventArgs obj)
    {
        _inverseTrackerLookup.Clear();
        SetupTrackerLookup();
    }

    private void SetupTrackerLookup()
    {
        // This breaks if you have N trackers to 1 JobId but future concern.
        foreach (var job in _protoManager.EnumeratePrototypes<JobPrototype>())
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
        DebugTools.Assert(_protoManager.HasIndex<PlayTimeTrackerPrototype>(trackerProto));
        return _inverseTrackerLookup[trackerProto];
    }

    /// <summary>
    /// Tries to get the first corresponding department for this job prototype.
    /// </summary>
    public bool TryGetDepartment(string jobProto, [NotNullWhen(true)] out DepartmentPrototype? departmentPrototype)
    {
        // Not that many departments so we can just eat the cost instead of storing the inverse lookup.
        var departmentProtos = _protoManager.EnumeratePrototypes<DepartmentPrototype>().ToList();
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
}
