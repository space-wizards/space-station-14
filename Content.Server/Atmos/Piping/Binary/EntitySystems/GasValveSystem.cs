using Content.Shared.Atmos.Nodes;
using Content.Shared.Atmos.Nodes.Handlers;
using Content.Shared.Atmos.Piping.Binary.Components;
using Content.Shared.Atmos.Piping.Binary.Systems;
using Content.Shared.Audio;
using Content.Shared.NodeContainer.Systems;

namespace Content.Server.Atmos.Piping.Binary.EntitySystems;

public sealed partial class GasValveSystem : SharedGasValveSystem
{
    [Dependency] private SharedAmbientSoundSystem _ambientSoundSystem = default!;
    [Dependency] private NodeContainerSystem _nodeContainer = default!;
    [Dependency] private PipeNodeHandler _pipeHandler = default!;

    public override void Set(EntityUid uid, GasValveComponent component, bool value)
    {
        base.Set(uid, component, value);

        if (_nodeContainer.TryGetNodes(uid, component.InletName, component.OutletName, out PipeNode? inlet, out PipeNode? outlet))
        {
            if (component.Open)
            {
                _pipeHandler.AddAlwaysReachable(inlet, outlet);
                _pipeHandler.AddAlwaysReachable(outlet, inlet);
                _ambientSoundSystem.SetAmbience(uid, true);
            }
            else
            {
                _pipeHandler.RemoveAlwaysReachable(inlet, outlet);
                _pipeHandler.RemoveAlwaysReachable(outlet, inlet);
                _ambientSoundSystem.SetAmbience(uid, false);
            }
        }
    }
}
