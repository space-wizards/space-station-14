using Content.Server.Administration;
using Content.Shared.Administration;
using Content.Shared.Anomaly.Components;
using Robust.Shared.Console;

namespace Content.Server.Anomaly;

public sealed partial class AnomalySystem
{
    [Dependency] private readonly IConsoleHost _consoleHost = default!;

    public void InitializeCommands()
    {
        _consoleHost.RegisterCommand("pulseanomaly", Loc.GetString("cmd-pulseanomaly-desc"), Loc.GetString("cmd-pulseanomaly-help"),
            PulseAnomalyCommand,
            GetAnomalyCompletion);

        _consoleHost.RegisterCommand("supercriticalanomaly", Loc.GetString("cmd-supercriticalanomaly-desc"), Loc.GetString("cmd-supercriticalanomaly-help"),
            SupercriticalAnomalyCommand,
            GetAnomalyCompletion);
    }

    [AdminCommand(AdminFlags.Fun)]
    private void PulseAnomalyCommand(IConsoleShell shell, string argstr, string[] args)
    {
        if (args.Length != 1)
            shell.WriteError(Loc.GetString("shell-need-exactly-one-argument"));

        if (!NetEntity.TryParse(args[0], out var uidNet) || !TryGetEntity(uidNet, out var uid))
            return;

        if (!TryComp<AnomalyComponent>(uid, out var anomaly))
            return;

        DoAnomalyPulse(uid.Value, anomaly);
    }

    [AdminCommand(AdminFlags.Fun)]
    private void SupercriticalAnomalyCommand(IConsoleShell shell, string argstr, string[] args)
    {
        if (args.Length != 1)
            shell.WriteError(Loc.GetString("shell-need-exactly-one-argument"));

        if (!NetEntity.TryParse(args[0], out var uidNet) || !TryGetEntity(uidNet, out var uid))
            return;

        if (!TryComp<AnomalyComponent>(uid, out var anomaly))
            return;

        StartSupercriticalEvent((uid.Value, anomaly));
    }

    private CompletionResult GetAnomalyCompletion(IConsoleShell shell, string[] args)
    {
        return args.Length != 1
            ? CompletionResult.Empty
            : CompletionResult.FromHintOptions(CompletionHelper.Components<AnomalyComponent>(args[0]), "<uid>");
    }
}
