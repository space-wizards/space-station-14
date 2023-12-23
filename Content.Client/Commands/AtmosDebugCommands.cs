using Content.Client.Atmos.EntitySystems;
using Content.Shared.Atmos;
using JetBrains.Annotations;
using Robust.Shared.Console;

namespace Content.Client.Commands;

[UsedImplicitly]
internal sealed class AtvRangeCommand : IConsoleCommand
{
    [Dependency] private readonly IEntitySystemManager _entitySystemManager = default!;

    // ReSharper disable once StringLiteralTypo
    public string Command => "atvrange";
    public string Description => Loc.GetString("atmos-debug-command-range-description");
    public string Help => Loc.GetString("atmos-debug-command-range-help", ("command", Command));

    public void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        if (args.Length != 2)
        {
            shell.WriteLine(Help);
            return;
        }
        if (!float.TryParse(args[0], out var xStart))
        {
            shell.WriteError(Loc.GetString("atmos-debug-command-range-error-start"));
            return;
        }
        if (!float.TryParse(args[1], out var xEnd))
        {
            shell.WriteError(Loc.GetString("atmos-debug-command-range-error-end"));
            return;
        }
        if (xStart == xEnd)
        {
            shell.WriteError(Loc.GetString("atmos-debug-command-range-error-zero"));
            return;
        }
        var sys = _entitySystemManager.GetEntitySystem<AtmosDebugOverlaySystem>();
        sys.CfgBase = xStart;
        sys.CfgScale = xEnd - xStart;
    }
}

[UsedImplicitly]
internal sealed class AtvModeCommand : IConsoleCommand
{
    [Dependency] private readonly IEntitySystemManager _entitySystemManager = default!;

    // ReSharper disable once StringLiteralTypo
    public string Command => "atvmode";
    public string Description => Loc.GetString("atmos-debug-command-mode-description");
    public string Help => Loc.GetString("atmos-debug-command-mode-help", ("command", Command));

    public void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        if (args.Length < 1)
        {
            shell.WriteLine(Help);
            return;
        }
        if (!Enum.TryParse<AtmosDebugOverlayMode>(args[0], out var xMode))
        {
            shell.WriteError(Loc.GetString("atmos-debug-command-mode-error-invalid"));
            return;
        }
        int xSpecificGas = 0;
        float xBase = 0;
        float xScale = Atmospherics.MolesCellStandard * 2;
        if (xMode == AtmosDebugOverlayMode.GasMoles)
        {
            if (args.Length != 2)
            {
                shell.WriteError(Loc.GetString("atmos-debug-command-mode-error-target-gas"));
                return;
            }
            if (!AtmosCommandUtils.TryParseGasID(args[1], out xSpecificGas))
            {
                shell.WriteError(Loc.GetString("atmos-debug-command-mode-error-oor"));
                return;
            }
        }
        else
        {
            if (args.Length != 1)
            {
                shell.WriteLine(Loc.GetString("atmos-debug-command-mode-error-info"));
                return;
            }
            if (xMode == AtmosDebugOverlayMode.Temperature)
            {
                // Red is 100C, Green is 20C, Blue is -60C
                xBase = Atmospherics.T20C + 80;
                xScale = -160;
            }
        }
        var sys = _entitySystemManager.GetEntitySystem<AtmosDebugOverlaySystem>();
        sys.CfgMode = xMode;
        sys.CfgSpecificGas = xSpecificGas;
        sys.CfgBase = xBase;
        sys.CfgScale = xScale;
    }
}

[UsedImplicitly]
internal sealed class AtvCBMCommand : IConsoleCommand
{
    [Dependency] private readonly IEntitySystemManager _entitySystemManager = default!;

    // ReSharper disable once StringLiteralTypo
    public string Command => "atvcbm";
    public string Description => Loc.GetString("atmos-debug-command-cbm-description");
    public string Help => Loc.GetString("atmos-debug-command-cbm-help", ("command", Command));

    public void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        if (args.Length != 1)
        {
            shell.WriteLine(Help);
            return;
        }
        if (!bool.TryParse(args[0], out var xFlag))
        {
            shell.WriteError(Loc.GetString("atmos-debug-command-cbm-error"));
            return;
        }
        var sys = _entitySystemManager.GetEntitySystem<AtmosDebugOverlaySystem>();
        sys.CfgCBM = xFlag;
    }
}
