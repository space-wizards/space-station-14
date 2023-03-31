using Robust.Shared.Timing;
using Robust.Shared.Utility;
using Robust.Shared.Map;

namespace Content.Replay.Manager;

// This partial class contains code for generating implicit component states.
public sealed partial class ReplayManager
{
    /// <summary>
    ///     Cached implicit entity states.
    /// </summary>
    private Dictionary<string, (List<ComponentChange>, HashSet<ushort>)> _implicitData = new();

    private ushort _metaId;

    private EntityState AddImplicitData(EntityState entState)
    {
        var prototype = GetPrototype(entState);
        if (prototype == null)
            return entState;

        var (list, set) = GetImplicitData(prototype);
        return MergeStates(entState, list, set);
    }

    private (List<ComponentChange>, HashSet<ushort>) GetImplicitData(string prototype)
    {
        if (_implicitData.TryGetValue(prototype, out var result))
            return result;

        var list = new List<ComponentChange>();
        var set = new HashSet<ushort>();
        _implicitData[prototype] = (list, set);

        var entCount = _entMan.EntityCount;
        var uid = _entMan.SpawnEntity(prototype, MapCoordinates.Nullspace);

        foreach (var (netId, component) in _entMan.GetNetComponents(uid))
        {
            if (!component.NetSyncEnabled)
                continue;

            var state = _entMan.GetComponentState(_entMan.EventBus, component, null, GameTick.Zero);
            DebugTools.Assert(state is not IComponentDeltaState delta || delta.FullState);
            list.Add(new ComponentChange(netId, state, GameTick.Zero));
            set.Add(netId);
        }

        _entMan.DeleteEntity(uid);
        DebugTools.Assert(entCount == _entMan.EntityCount);
        return (list, set);
    }

    private string? GetPrototype(EntityState entState)
    {
        foreach (var comp in entState.ComponentChanges.Span)
        {
            if (comp.NetID == _metaId)
            {
                var state = (MetaDataComponentState) comp.State;
                return state.PrototypeId;
            }
        }

        throw new Exception("Missing metadata component");
    }
}
