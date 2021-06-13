using Content.Server.Atmos.Piping.Components;
using Content.Server.GameObjects.Components.Atmos.Piping;
using Content.Server.GameObjects.Components.Atmos.Piping.Binary;
using Content.Server.GameObjects.Components.NodeContainer;
using Content.Server.GameObjects.Components.NodeContainer.Nodes;
using Content.Server.NodeContainer;
using JetBrains.Annotations;
using Robust.Shared.GameObjects;

namespace Content.Server.GameObjects.EntitySystems.Atmos.Piping.Binary
{
    [UsedImplicitly]
    public class GasPortSystem : EntitySystem
    {
        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<GasPortComponent, AtmosDeviceUpdateEvent>(OnPortUpdated);
        }

        private void OnPortUpdated(EntityUid uid, GasPortComponent port, AtmosDeviceUpdateEvent args)
        {
            if (!ComponentManager.TryGetComponent(uid, out NodeContainerComponent? nodeContainer))
                return;

            if (!nodeContainer.TryGetNode(port.PipeName, out PipeNode? pipe)
                || !nodeContainer.TryGetNode(port.ConnectedName, out PipeNode? connected))
                return;

            // Clear before use, always!
            port.Buffer.Clear();
            port.Buffer.Volume = pipe.Air.Volume + connected.Air.Volume;

            port.Buffer.Merge(pipe.Air);
            port.Buffer.Merge(connected.Air);

            pipe.Air.Clear();
            pipe.AssumeAir(port.Buffer);
            pipe.Air.Multiply(pipe.Air.Volume / port.Buffer.Volume);

            connected.Air.Clear();
            connected.AssumeAir(port.Buffer);
            connected.Air.Multiply(connected.Air.Volume / port.Buffer.Volume);
        }
    }
}
