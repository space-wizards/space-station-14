using Content.Server.Administration;
using Content.Server.Atmos.EntitySystems;
using Content.Server.Atmos.Piping.Unary.Components;
using Content.Server.NodeContainer;
using Content.Server.NodeContainer.EntitySystems;
using Content.Server.NodeContainer.NodeGroups;
using Content.Server.NodeContainer.Nodes;
using Content.Shared.Administration;
using Content.Shared.Atmos;
using Robust.Shared.Console;
using Robust.Shared.Toolshed;

namespace Content.Server.Atmos.Piping.Unary.EntitySystems;

public sealed class GasPipeFillSystem : EntitySystem
{
    [Dependency] private readonly AtmosphereSystem _atmos = default!;
    [Dependency] private readonly NodeContainerSystem _nodeContainer = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<PipeFillComponent, NodeGroupsRebuilt>(OnNodeUpdate);
    }

    private void OnNodeUpdate(EntityUid uid, PipeFillComponent comp, ref NodeGroupsRebuilt args)
    {
        if (_nodeContainer.TryGetNode(uid, comp.NodeName, out PipeNode? tank) && tank.NodeGroup is PipeNet net)
        {
            _atmos.Merge(net.Air, comp.Air);
        }

        RemComp<PipeFillComponent>(uid); // only fire once, and fail dumb.
    }

    // Commands for arbitrary pipe gas changes

    [AdminCommand(AdminFlags.Mapping)]
    public sealed class AdjustPipeMoleCommand : IConsoleCommand
    {
        [Dependency] private readonly IEntityManager _entManager = default!;
        [Dependency] private readonly IEntitySystemManager _entSystemManager = default!;

        public string Command => "adjpipemole";
        public string Description => "adjust the moles of a given gas in a PipeNet";
        public string Help => "adjpipemole <pipe EntId> <node name> <Gas> <moles>";

        public CompletionResult GetCompletion(IConsoleShell shell, string[] args)
        {
            if (args.Length == 1)
                return CompletionResult.FromHint("Entity NetID (a pipe).");

            if (args.Length == 2)
                return CompletionResult.FromHint("the node name in the NodeContainerComponent to fill.");

            if (args.Length == 3)
                return CompletionResult.FromHintOptions(Enum.GetNames(typeof(Gas)), "Gas.");

            if (args.Length == 4)
                return CompletionResult.FromHint("float");

            return CompletionResult.Empty;
        }

        public void Execute(IConsoleShell shell, string argStr, string[] args)
        {
            if (args.Length != 4)
                return;

            if (!NetEntity.TryParse(args[0], out var netEnt) ||
                !_entManager.TryGetEntity(netEnt, out var euid) ||
                !(AtmosCommandUtils.TryParseGasID(args[2], out var gasId)) ||
                !float.TryParse(args[3], out var moles)
                )
                return;

            var _nodeContainer = _entSystemManager.GetEntitySystem<NodeContainerSystem>();

            if (!_entManager.TryGetComponent<NodeContainerComponent>(euid, out var nodeCont))
                return;

            if (_nodeContainer.TryGetNode(nodeCont, args[1], out PipeNode? pipe) && pipe.NodeGroup is PipeNet net)
                net.Air.AdjustMoles(gasId, moles);
        }
    }

    [AdminCommand(AdminFlags.Mapping)]
    public sealed class AdjustPipeAirCommand : IConsoleCommand
    {
        [Dependency] private readonly IEntityManager _entManager = default!;
        [Dependency] private readonly IEntitySystemManager _entSystemManager = default!;

        public string Command => "adjpipeair";
        public string Description => "set a pipe's pipenet contents to the standard air mix.";
        public string Help => "adjpipeair <pipe EntId> <node name>";

        public CompletionResult GetCompletion(IConsoleShell shell, string[] args)
        {
            if (args.Length == 1)
                return CompletionResult.FromHint("Entity NetID (a pipe).");

            if (args.Length == 2)
                return CompletionResult.FromHint("the node name in the NodeContainerComponent to fill.");

            return CompletionResult.Empty;
        }

        public void Execute(IConsoleShell shell, string argStr, string[] args)
        {
            if (args.Length != 2)
                return;

            if (!NetEntity.TryParse(args[0], out var netEnt) ||
                !_entManager.TryGetEntity(netEnt, out var euid)
                )
                return;

            var _nodeContainer = _entSystemManager.GetEntitySystem<NodeContainerSystem>();

            if (!_entManager.TryGetComponent<NodeContainerComponent>(euid, out var nodeCont))
                return;

            if (_nodeContainer.TryGetNode(nodeCont, args[1], out PipeNode? pipe) && pipe.NodeGroup is PipeNet net)
            {
                var volScalar = (net.Air.Volume / Atmospherics.CellVolume) * 2; // 2x tile atmos so it can fill stuff, but atmos nerds have room to mald.
                net.Air.Clear();
                net.Air.AdjustMoles(Gas.Oxygen, Atmospherics.OxygenMolesStandard * volScalar);
                net.Air.AdjustMoles(Gas.Nitrogen, Atmospherics.NitrogenMolesStandard * volScalar);
            }
        }

        // Toolshed needs a parser for PipeNet and I cannot be arsed right now. IConsoleCommands it is.
        /*
        [ToolshedCommand, AdminCommand(AdminFlags.Mapping)]
        public sealed class PipeFillCommand : ToolshedCommand
        {
            [CommandImplementation("adjustgas")]
            public void PipeFill([CommandArgument] PipeNet netId, [CommandArgument] string gas, [CommandArgument] float moles)
            {
                AtmosCommandUtils.TryParseGasID(gas, out var gasId);
                netId.Air.AdjustMoles(gasId, moles);
            }
        }
        */
    }
}
