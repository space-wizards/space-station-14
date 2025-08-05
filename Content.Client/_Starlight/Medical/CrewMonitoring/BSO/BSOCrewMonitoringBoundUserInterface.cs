using Content.Shared.Medical.CrewMonitoring;
using Robust.Client.UserInterface;
using System.Linq;
using Content.Shared.Access;
using Content.Shared.Access.Components;
using Content.Shared.Access.Systems;
using Content.Shared.Containers.ItemSlots;
using Robust.Client.UserInterface;
using Robust.Shared.Prototypes;
using static Content.Shared.Access.Components.AccessOverriderComponent;
using Content.Shared.Implants.Components;
namespace Content.Client.Medical.CrewMonitoring.BSO;

public class BSOCrewMonitoringBoundUserInterface : BoundUserInterface
{
    [ViewVariables]
    protected CrewMonitoringWindow? _menu;

    public BSOCrewMonitoringBoundUserInterface(EntityUid owner, Enum uiKey) : base(owner, uiKey)
    {
        _accessOverriderSystem = EntMan.System<SharedAccessOverriderSystem>();
    }
    protected readonly SharedAccessOverriderSystem _accessOverriderSystem = default!;

    protected override void Open()
    {
        base.Open();
        EntityUid? gridUid = null;
        var stationName = string.Empty;

        if (EntMan.TryGetComponent<TransformComponent>(Owner, out var xform))
        {
            gridUid = xform.GridUid;

            if (EntMan.TryGetComponent<MetaDataComponent>(gridUid, out var metaData))
            {
                stationName = metaData.EntityName;
            }
        }

        _menu = this.CreateWindow<CrewMonitoringWindow>();
        _menu.Set(stationName, gridUid);
    }

    protected override void UpdateState(BoundUserInterfaceState state)
    {
        base.UpdateState(state);
        switch (state)
        {
            case CrewMonitoringState st:
                EntMan.TryGetComponent<TransformComponent>(Owner, out var xform);
                var commandDepartmentSensors = st.Sensors
                    .Where(sensor => sensor.JobDepartments.Contains("Command"))
                    .ToList();
                //also ALWAYS include the trackers
                //this is jank as there isnt a direct indication of a tracker in the suit sensor status
                //so we need to check the component directly
                foreach (var sensor in st.Sensors)
                {
                    //get the client entity
                    var clientEntity = EntMan.GetEntity(sensor.SuitSensorUid);
                    if (EntMan.TryGetComponent<SubdermalImplantComponent>(clientEntity, out var suitSensor))
                    {
                        commandDepartmentSensors.Add(sensor);
                    }
                }
                //remove duplicates
                commandDepartmentSensors = commandDepartmentSensors.Distinct().ToList();
                _menu?.ShowSensors(commandDepartmentSensors, Owner, xform?.Coordinates);
                break;
        }
    }
}
