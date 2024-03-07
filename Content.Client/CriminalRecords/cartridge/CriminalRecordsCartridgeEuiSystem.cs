using Content.Shared.CriminalRecords;
using Content.Shared.Roles;
using Robust.Shared.Prototypes;

namespace Content.Client.CriminalRecords;

public sealed class CriminalRecordsCartridgeEuiSystem : EntitySystem
{
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    public override void Initialize()
    {
        base.Initialize();

        //BuildDepartmentLookup();
        SubscribeLocalEvent<PrototypesReloadedEventArgs>(OnPrototypesReload);
    }

    /// <summary>
    ///     Requests a crew manifest from the server.
    /// </summary>
    /// <param name="netEntity">EntityUid of the entity we're requesting the crew manifest from.</param>
    public void RequestCriminalRecords(NetEntity netEntity)
    {
        RaiseNetworkEvent(new RequestCriminalRecordsCartridgeMessa(netEntity));
    }

    private void OnPrototypesReload(PrototypesReloadedEventArgs args)
    {
        // f (args.WasModified<DepartmentPrototype>())
        //   BuildDepartmentLookup();
    }

    // private void BuildDepartmentLookup()
    // {
    //     _jobDepartmentLookup.Clear();
    //     _departments.Clear();
    //     foreach (var department in _prototypeManager.EnumeratePrototypes<DepartmentPrototype>())
    //     {
    //         _departments.Add(department.ID);

    //         for (var i = 1; i <= department.Roles.Count; i++)
    //         {
    //             if (!_jobDepartmentLookup.TryGetValue(department.Roles[i - 1], out var departments))
    //             {
    //                 departments = new();
    //                 _jobDepartmentLookup.Add(department.Roles[i - 1], departments);
    //             }

    //             departments.Add(department.ID, i);
    //         }
    //     }
    // }

    // public int GetDepartmentOrder(string department, string jobPrototype)
    // {
    //     if (!Departments.Contains(department))
    //     {
    //         return -1;
    //     }

    //     if (!_jobDepartmentLookup.TryGetValue(jobPrototype, out var departments))
    //     {
    //         return -1;
    //     }

    //     return departments.TryGetValue(department, out var order)
    //         ? order
    //         : -1;
    // }
}
