using System.Linq;
using System.Text;
using Content.Server.Administration;
using Content.Server.Body.Components;
using Content.Shared._EE.Plasmaman;
using Content.Shared.Administration;
using Content.Shared.Atmos;
using Content.Shared.Atmos.Components;
using Content.Shared.Body;
using Content.Shared.Body.Components;
using Content.Shared.Body.Systems;
using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Metabolism;
using Robust.Shared.Console;
using Robust.Shared.Containers;

namespace Content.Server._EE.Plasmaman;

[AdminCommand(AdminFlags.Debug)]
public sealed class PlasmamanDebugCommand : IConsoleCommand
{
    [Dependency] private readonly IEntityManager _entManager = default!;

    public string Command => "plasmamandebug";
    public string Description => "Dumps Plasmaman breathing pipeline (saturation, internals, tank, lungs).";
    public string Help => $"Usage: {Command} [<entityUid>] — defaults to your attached entity.";

    public void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        EntityUid entity;

        if (args.Length == 0)
        {
            if (shell.Player?.AttachedEntity is not { } attached)
            {
                shell.WriteLine("No entity argument and no attached entity.");
                return;
            }

            entity = attached;
        }
        else if (NetEntity.TryParse(args[0], out var netUid) && _entManager.TryGetEntity(netUid, out var resolved))
        {
            entity = resolved.Value;
        }
        else if (int.TryParse(args[0], out var raw))
        {
            entity = new EntityUid(raw);
        }
        else
        {
            shell.WriteLine($"Cannot parse '{args[0]}' as entity uid.");
            return;
        }

        if (!_entManager.EntityExists(entity))
        {
            shell.WriteLine($"Entity {entity} does not exist.");
            return;
        }

        var sb = new StringBuilder();
        sb.AppendLine($"=== Plasmaman debug for {entity} ({_entManager.GetComponent<MetaDataComponent>(entity).EntityName}) ===");

        if (_entManager.TryGetComponent<RespiratorComponent>(entity, out var resp))
        {
            sb.AppendLine($"Respirator: Saturation={resp.Saturation:F2} (min {resp.MinSaturation}, max {resp.MaxSaturation}), SuffocationCycles={resp.SuffocationCycles}, Status={resp.Status}, BreathVolume={resp.BreathVolume}, UpdateInterval={resp.UpdateInterval}, Multiplier={resp.UpdateIntervalMultiplier}, AdjustedInterval={resp.AdjustedUpdateInterval}");
        }
        else
        {
            sb.AppendLine("Respirator: <missing>");
        }

        if (_entManager.TryGetComponent<InternalsComponent>(entity, out var internals))
        {
            sb.AppendLine($"Internals: BreathTools.Count={internals.BreathTools.Count}, GasTankEntity={internals.GasTankEntity}");
            foreach (var bt in internals.BreathTools)
            {
                if (_entManager.TryGetComponent<BreathToolComponent>(bt, out var btComp))
                    sb.AppendLine($"  BreathTool {bt} ({_entManager.GetComponent<MetaDataComponent>(bt).EntityName}): IsFunctional={btComp.IsFunctional}, ConnectedInternalsEntity={btComp.ConnectedInternalsEntity}");
            }

            if (internals.GasTankEntity is { } tank && _entManager.TryGetComponent<GasTankComponent>(tank, out var tankComp))
            {
                sb.AppendLine($"Tank {tank} ({_entManager.GetComponent<MetaDataComponent>(tank).EntityName}): IsConnected={tankComp.IsConnected}, ReleasePressure={tankComp.ReleasePressure}, ReleaseValveOpen={tankComp.ReleaseValveOpen}, TotalMoles={tankComp.Air.TotalMoles:F4}");
                sb.AppendLine($"  Tank moles: {FormatMoles(tankComp.Air)}");
            }
        }
        else
        {
            sb.AppendLine("Internals: <missing>");
        }

        if (_entManager.TryGetComponent<BodyComponent>(entity, out var body) && body.Organs is { } organs)
        {
            var solutionSys = _entManager.System<SharedSolutionContainerSystem>();
            var lungs = organs.ContainedEntities
                .Where(e => _entManager.HasComponent<LungComponent>(e))
                .ToList();

            sb.AppendLine($"Body: organs={organs.ContainedEntities.Count}, lungs found={lungs.Count}");

            foreach (var lung in lungs)
            {
                var lungComp = _entManager.GetComponent<LungComponent>(lung);
                var lungName = _entManager.GetComponent<MetaDataComponent>(lung).EntityName;
                sb.AppendLine($"  Lung {lung} ({lungName}): Air.TotalMoles={lungComp.Air.TotalMoles:F4}, Volume={lungComp.Air.Volume}, Solution={lungComp.SolutionName}");
                sb.AppendLine($"    Lung air moles: {FormatMoles(lungComp.Air)}");

                if (solutionSys.TryGetSolution(lung, lungComp.SolutionName, out _, out var solution))
                {
                    var contents = solution.Contents.Count == 0
                        ? "<empty>"
                        : string.Join(", ", solution.Contents.Select(c => $"{c.Reagent.Prototype}={c.Quantity}"));
                    sb.AppendLine($"    Lung solution ({solution.Volume}/{solution.MaxVolume}): {contents}");
                }
                else
                {
                    sb.AppendLine($"    Lung solution: <not found>");
                }

                if (_entManager.TryGetComponent<MetabolizerComponent>(lung, out var metab))
                {
                    var types = metab.MetabolizerTypes is null ? "<null>" : string.Join(",", metab.MetabolizerTypes);
                    var stages = string.Join(",", metab.Stages);
                    sb.AppendLine($"    Metabolizer: types=[{types}], stages=[{stages}], MaxReagentsProcessable={metab.MaxReagentsProcessable}");
                }
                else
                {
                    sb.AppendLine($"    Metabolizer: <missing>");
                }
            }
        }
        else
        {
            sb.AppendLine("Body: <missing>");
        }

        if (_entManager.TryGetComponent<PlasmamanDashComponent>(entity, out var dash))
        {
            sb.AppendLine($"Dash: Action={dash.Action}, ActionEntity={dash.ActionEntity}, MaxStrength={dash.MaxStrength}, MinStrength={dash.MinStrength}, MinSaturation={dash.MinSaturation}, SaturationCost={dash.SaturationCost}, MaxAtmosMoles={dash.MaxAtmosMoles}");
        }
        else
        {
            sb.AppendLine("Dash: <component missing> — компонент PlasmamanDash не повешен. Проверь body.yml и пересборку сервера.");
        }

        shell.WriteLine(sb.ToString());
    }

    private static string FormatMoles(GasMixture mix)
    {
        var parts = new List<string>();
        for (var i = 0; i < Atmospherics.TotalNumberOfGases; i++)
        {
            var moles = mix.GetMoles(i);
            if (moles <= 0)
                continue;
            parts.Add($"{(Gas) i}={moles:F4}");
        }

        return parts.Count == 0 ? "<empty>" : string.Join(", ", parts);
    }
}
