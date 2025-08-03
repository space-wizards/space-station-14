using Content.Server.Atmos.EntitySystems;
using Content.Server.Atmos.Piping.Components;
using Content.Server.NodeContainer.EntitySystems;
using Content.Server.NodeContainer.Nodes;
using Content.Shared.Atmos;
using Content.Shared.NodeContainer;
using System.Linq;

namespace Content.Server.Atmos.Piping.EntitySystems;

public sealed partial class GasPipeManifoldSystem : EntitySystem
{
    [Dependency] private readonly NodeContainerSystem _nodeContainer = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<GasPipeManifoldComponent, ComponentInit>(OnCompInit);
        SubscribeLocalEvent<GasPipeManifoldComponent, GasAnalyzerScanEvent>(OnAnalyzed);
    }

    private void OnCompInit(Entity<GasPipeManifoldComponent> ent, ref ComponentInit args)
    {
        if (!TryComp<NodeContainerComponent>(ent, out var nodeContainer))
            return;

        foreach (var inletName in ent.Comp.InletNames)
        {
            if (!_nodeContainer.TryGetNode(nodeContainer, inletName, out PipeNode? inlet))
                continue;

            foreach (var outletName in ent.Comp.OutletNames)
            {
                if (!_nodeContainer.TryGetNode(nodeContainer, outletName, out PipeNode? outlet))
                    continue;

                inlet.AddAlwaysReachable(outlet);
                outlet.AddAlwaysReachable(inlet);
            }
        }
    }

    private void OnAnalyzed(Entity<GasPipeManifoldComponent> ent, ref GasAnalyzerScanEvent args)
    {
        // All inlets and outlets have the same gas mixture

        args.GasMixtures = new List<(string, GasMixture?)>();

        if (!TryComp<NodeContainerComponent>(ent, out var nodeContainer))
            return;

        var pipeNames = ent.Comp.InletNames.Union(ent.Comp.OutletNames);

        foreach (var pipeName in pipeNames)
        {
            if (!_nodeContainer.TryGetNode(nodeContainer, pipeName, out PipeNode? pipe))
                continue;

            var pipeLocal = pipe.Air.Clone();
            pipeLocal.Multiply(pipe.Volume / pipe.Air.Volume);
            pipeLocal.Volume = pipe.Volume;

            args.GasMixtures.Add((Name(ent), pipeLocal));
            break;
        }
    }
}
