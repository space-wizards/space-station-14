using Content.Shared.Atmos.Components;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared.Atmos.Consoles;

public abstract class SharedAtmosMonitoringConsoleSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<AtmosMonitoringConsoleComponent, ComponentGetState>(OnGetState);
    }

    private void OnGetState(EntityUid uid, AtmosMonitoringConsoleComponent component, ref ComponentGetState args)
    {
        Dictionary<Vector2i, Dictionary<AtmosMonitoringConsoleSubnet, ulong>> chunks;

        // Should this be a full component state or a delta-state?
        if (args.FromTick <= component.CreationTick || component.ForceFullUpdate)
        {
            component.ForceFullUpdate = false;

            // Full state
            chunks = new(component.AtmosPipeChunks.Count);

            foreach (var (origin, chunk) in component.AtmosPipeChunks)
            {
                chunks.Add(origin, chunk.AtmosPipeData);
            }

            args.State = new AtmosMonitoringConsoleState(chunks, component.AtmosDevices);

            return;
        }

        chunks = new();

        foreach (var (origin, chunk) in component.AtmosPipeChunks)
        {
            if (chunk.LastUpdate < args.FromTick)
                continue;

            chunks.Add(origin, chunk.AtmosPipeData);
        }

        args.State = new AtmosMonitoringConsoleDeltaState(chunks, component.AtmosDevices, new(component.AtmosPipeChunks.Keys));
    }

    #region: System messages

    [Serializable, NetSerializable]
    protected sealed class AtmosMonitoringConsoleState(
        Dictionary<Vector2i, Dictionary<AtmosMonitoringConsoleSubnet, ulong>> chunks,
        Dictionary<NetEntity, AtmosDeviceNavMapData> atmosDevices)
        : ComponentState
    {
        public Dictionary<Vector2i, Dictionary<AtmosMonitoringConsoleSubnet, ulong>> Chunks = chunks;
        public Dictionary<NetEntity, AtmosDeviceNavMapData> AtmosDevices = atmosDevices;
    }

    [Serializable, NetSerializable]
    protected sealed class AtmosMonitoringConsoleDeltaState(
        Dictionary<Vector2i, Dictionary<AtmosMonitoringConsoleSubnet, ulong>> modifiedChunks,
        Dictionary<NetEntity, AtmosDeviceNavMapData> atmosDevices,
        HashSet<Vector2i> allChunks)
        : ComponentState, IComponentDeltaState<AtmosMonitoringConsoleState>
    {
        public Dictionary<Vector2i, Dictionary<AtmosMonitoringConsoleSubnet, ulong>> ModifiedChunks = modifiedChunks;
        public Dictionary<NetEntity, AtmosDeviceNavMapData> AtmosDevices = atmosDevices;
        public HashSet<Vector2i> AllChunks = allChunks;

        public void ApplyToFullState(AtmosMonitoringConsoleState state)
        {
            foreach (var key in state.Chunks.Keys)
            {
                if (!AllChunks!.Contains(key))
                    state.Chunks.Remove(key);
            }

            foreach (var (index, data) in ModifiedChunks)
            {
                state.Chunks[index] = new Dictionary<AtmosMonitoringConsoleSubnet, ulong>(data);
            }

            state.AtmosDevices.Clear();
            foreach (var (nuid, atmosDevice) in AtmosDevices)
            {
                state.AtmosDevices.Add(nuid, atmosDevice);
            }
        }

        public AtmosMonitoringConsoleState CreateNewFullState(AtmosMonitoringConsoleState state)
        {
            var chunks = new Dictionary<Vector2i, Dictionary<AtmosMonitoringConsoleSubnet, ulong>>(state.Chunks.Count);

            foreach (var (index, data) in state.Chunks)
            {
                if (!AllChunks!.Contains(index))
                    continue;

                if (ModifiedChunks.ContainsKey(index))
                    chunks[index] = new Dictionary<AtmosMonitoringConsoleSubnet, ulong>(ModifiedChunks[index]);

                else
                    chunks[index] = new Dictionary<AtmosMonitoringConsoleSubnet, ulong>(state.Chunks[index]);
            }

            return new AtmosMonitoringConsoleState(chunks, new(AtmosDevices));
        }
    }

    #endregion
}
