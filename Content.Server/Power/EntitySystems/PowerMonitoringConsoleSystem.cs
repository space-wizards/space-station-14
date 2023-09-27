using Content.Shared.Power;
using Content.Server.NodeContainer;
using Content.Server.NodeContainer.EntitySystems;
using Content.Server.NodeContainer.Nodes;
using Content.Server.Power.Components;
using Content.Server.Power.NodeGroups;
using JetBrains.Annotations;
using Robust.Server.GameObjects;
using Content.Shared.Construction.Components;
using System.Numerics;
using Robust.Shared.Map;
using Content.Server.NodeContainer.NodeGroups;
using System.Threading.Tasks;
using System.Linq;

namespace Content.Server.Power.EntitySystems;

[UsedImplicitly]
internal sealed class PowerMonitoringConsoleSystem : EntitySystem
{
    private float _updateTimer = 0.0f;
    private const float UpdateTime = 1.0f;

    [Dependency] private readonly EntityManager _entityManager = default!;
    [Dependency] private readonly NodeContainerSystem _nodeContainer = default!;
    [Dependency] private readonly UserInterfaceSystem _userInterfaceSystem = default!;
    [Dependency] private readonly IMapManager _mapManager = default!;

    public override void Update(float frameTime)
    {
        _updateTimer += frameTime;
        if (_updateTimer >= UpdateTime)
        {
            _updateTimer -= UpdateTime;

            var query = EntityQueryEnumerator<PowerMonitoringConsoleComponent>();
            while (query.MoveNext(out var uid, out var component))
            {
                UpdateUIState(uid, component);
            }
        }
    }

    public void UpdateUIState(EntityUid target, PowerMonitoringConsoleComponent? pmcComp = null, NodeContainerComponent? ncComp = null)
    {
        if (!Resolve(target, ref pmcComp))
            return;

        if (!Resolve(target, ref ncComp))
            return;

        if (!_userInterfaceSystem.TryGetUi(target, PowerMonitoringConsoleUiKey.Key, out var bui))
            return;

        var consoleXform = _entityManager.GetComponent<TransformComponent>(target);
        if (consoleXform?.GridUid == null)
            return;

        var sources = new List<PowerMonitoringConsoleEntry>();
        var loads = new List<PowerMonitoringConsoleEntry>();

        //PowerConsumerComponent
        //BatteryChargerComponent

        //PowerSupplierComponent
        //BatteryDischargerComponent

        var query = AllEntityQuery<PowerNetworkBatteryComponent, TransformComponent>();
        while (query.MoveNext(out var uid, out var networkBattery, out var xform))
        {
            if (xform.Anchored)
            {
                var metaData = MetaData(networkBattery.Owner);
                var prototype = metaData.EntityPrototype?.ID ?? "";

                loads.Add(new PowerMonitoringConsoleEntry(_entityManager.GetNetEntity(uid), GetNetCoordinates(xform.Coordinates), metaData.EntityName, prototype, networkBattery.NetworkBattery.CurrentReceiving, true));
            }
        }

        // Sort
        loads.Sort(CompareLoadOrSources);
        sources.Sort(CompareLoadOrSources);

        // Actually set state.
        if (_userInterfaceSystem.TryGetUi(target, PowerMonitoringConsoleUiKey.Key, out bui))
           _userInterfaceSystem.SetUiState(bui, new PowerMonitoringConsoleBoundInterfaceState(loads.ToArray(), true, 10f));
    }

    private int CompareLoadOrSources(PowerMonitoringConsoleEntry x, PowerMonitoringConsoleEntry y)
    {
        return -x.Size.CompareTo(y.Size);
    }
}
