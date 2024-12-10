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
using Robust.Shared.Map.Events;
using Robust.Shared.Toolshed;

namespace Content.Server.Atmos.Piping.Unary.EntitySystems;

public sealed class GasPipeFillSystem : EntitySystem
{
    [Dependency] private readonly AtmosphereSystem _atmos = default!;
    [Dependency] private readonly SharedMapSystem _mapSystem = default!;
    [Dependency] private readonly NodeContainerSystem _nodeContainer = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<PipeFillComponent, NodeGroupsRebuilt>(OnNodeUpdate);

        SubscribeLocalEvent<BeforeSaveEvent>(OnMapSave);
        SubscribeLocalEvent<AfterSaveEvent>(OnMapSaved);
    }

    private void OnNodeUpdate(EntityUid uid, PipeFillComponent comp, ref NodeGroupsRebuilt args)
    {
        foreach (var gasmix in comp.AirDict)
        {
            if (_nodeContainer.TryGetNode(uid, gasmix.Key, out PipeNode? tank) && tank.NodeGroup is PipeNet net)
            {
                _atmos.Merge(net.Air, gasmix.Value);
            }
        }

        comp.HasFired = true; // only fire once, and fail dumb.
    }

    /// <summary>
    /// This clusterfuck saves pipenets on entities because thats just what we gotta do I guess. Someone please save me. My god.
    /// </summary>
    private void OnMapSave(BeforeSaveEvent ev)
    {
        var enumerator = AllEntityQuery<PipeFillComponent, NodeContainerComponent>();
        while (enumerator.MoveNext(out var uid, out var pipeFill, out var nodeContainer))
        {
            if (!TryComp(uid, out TransformComponent? xform) ||
                !_mapSystem.TryGetMap(xform.MapID, out var mapEnt) ||
                mapEnt != ev.Map
                )
                continue;

            // build the dictionary for AirDict
            var nextAirDict = new Dictionary<string, GasMixture>();

            foreach (var node in nodeContainer.Nodes)
            {
                if (_nodeContainer.TryGetNode(uid, node.Key, out PipeNode? pipeNode) && pipeNode.NodeGroup is PipeNet net)
                {
                    // copy nodeshare worth of gas to savedGas
                    var savedGas = new GasMixture();
                    var nodeShare = pipeNode.Volume / net.Air.Volume;

                    foreach (Gas gastype in Enum.GetValues(typeof(Gas)))
                    {
                        savedGas.AdjustMoles(gastype, net.Air.GetMoles(gastype) * nodeShare);
                    }

                    savedGas.Temperature = net.Air.Temperature;

                    nextAirDict.Add(node.Key, savedGas);
                }
            }
            pipeFill.AirDict = nextAirDict;

            // set HasFired false so gas gets added when the map is loaded and PipeNets update.
            pipeFill.HasFired = false;
        }
    }

    private void OnMapSaved(AfterSaveEvent ev)
    {
        var enumerator = AllEntityQuery<PipeFillComponent, NodeContainerComponent>();
        while (enumerator.MoveNext(out var uid, out var pipeFill, out var nodeContainer))
        {
            if (!TryComp(uid, out TransformComponent? xform) ||
                !_mapSystem.TryGetMap(xform.MapID, out var mapEnt) ||
                mapEnt != ev.Map
                )
                continue;

            // set HasFired true so that if you continue mapping it wont refire.
            pipeFill.HasFired = true;
        }
    }

    // Commands for arbitrary pipe gas changes

    /// <summary>
    /// A console command that lets you manually set the moles of gas in a pipenet.
    /// </summary>
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

    /// <summary>
    /// A console command that fills a pipenet with standard air mix.
    /// </summary>
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
                var volScalar = (net.Air.Volume / Atmospherics.CellVolume) * 3; // 3x tile atmos so it can fill stuff, but atmos nerds have room to mald.
                net.Air.Clear();
                net.Air.AdjustMoles(Gas.Oxygen, Atmospherics.OxygenMolesStandard * volScalar);
                net.Air.AdjustMoles(Gas.Nitrogen, Atmospherics.NitrogenMolesStandard * volScalar);
            }
        }
    }

    /// <summary>
    /// A console command that emtpies a pipenet.
    /// </summary>
    [AdminCommand(AdminFlags.Mapping)]
    public sealed class AdjustPipeEmptyCommand : IConsoleCommand
    {
        [Dependency] private readonly IEntityManager _entManager = default!;
        [Dependency] private readonly IEntitySystemManager _entSystemManager = default!;

        public string Command => "adjpipeempty";
        public string Description => "set a pipe's pipenet contents to empty.";
        public string Help => "adjpipeempty <pipe EntId> <node name>";

        public CompletionResult GetCompletion(IConsoleShell shell, string[] args)
        {
            if (args.Length == 1)
                return CompletionResult.FromHint("Entity NetID (a pipe).");

            if (args.Length == 2)
                return CompletionResult.FromHint("the node name in the NodeContainerComponent to empty.");

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
                net.Air.Clear();
            }
        }
    }

    /// <summary>
    /// A console command that emtpies a pipenet.
    /// </summary>
    [AdminCommand(AdminFlags.Mapping)]
    public sealed class AdjustPipeTempCommand : IConsoleCommand
    {
        [Dependency] private readonly IEntityManager _entManager = default!;
        [Dependency] private readonly IEntitySystemManager _entSystemManager = default!;

        public string Command => "adjpipetemp";
        public string Description => "set a pipe's pipenet temperature.";
        public string Help => "adjpipetemp <pipe EntId> <node name> <float>";

        public CompletionResult GetCompletion(IConsoleShell shell, string[] args)
        {
            if (args.Length == 1)
                return CompletionResult.FromHint("Entity NetID (a pipe).");

            if (args.Length == 2)
                return CompletionResult.FromHint("the node name in the NodeContainerComponent to adjust.");

            if (args.Length == 3)
                return CompletionResult.FromHint($"float, default: {Atmospherics.T0C}");

            return CompletionResult.Empty;
        }

        public void Execute(IConsoleShell shell, string argStr, string[] args)
        {
            if (args.Length != 3)
                return;

            if (!NetEntity.TryParse(args[0], out var netEnt) ||
                !_entManager.TryGetEntity(netEnt, out var euid) ||
                !float.TryParse(args[2], out var temp)
                )
                return;

            temp = Math.Clamp(temp, Atmospherics.TCMB, Atmospherics.Tmax);

            var _nodeContainer = _entSystemManager.GetEntitySystem<NodeContainerSystem>();

            if (!_entManager.TryGetComponent<NodeContainerComponent>(euid, out var nodeCont))
                return;

            if (_nodeContainer.TryGetNode(nodeCont, args[1], out PipeNode? pipe) && pipe.NodeGroup is PipeNet net)
            {
                net.Air.Clear();
            }
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
