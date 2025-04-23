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
namespace Content.Client.Medical.CrewMonitoring.Brigmedic;

public class BrigmedicCrewMonitoringBoundUserInterface : BoundUserInterface
{
    [ViewVariables]
    protected CrewMonitoringWindow? _menu;

    public BrigmedicCrewMonitoringBoundUserInterface(EntityUid owner, Enum uiKey) : base(owner, uiKey)
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
                var securityDepartmentSensors = st.Sensors
                    .Where(sensor => sensor.JobDepartments.Contains("Security"))
                    .ToList();
                _menu?.ShowSensors(securityDepartmentSensors, Owner, xform?.Coordinates);
                break;
        }
    }
}
