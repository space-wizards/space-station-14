using Content.Client.Atmos.EntitySystems;
using Content.Shared.Atmos;
using JetBrains.Annotations;
using Robust.Shared.Console;

namespace Content.Client.Commands;

[UsedImplicitly]
internal sealed class AtvRangeCommand : LocalizedCommands
{
    [Dependency] private readonly IEntitySystemManager _entitySystemManager = default!;

    public override string Command => "atvrange";

    public override string Help => LocalizationManager.GetString($"cmd-{Command}-help", ("command", Command));

    public override void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        if (args.Length != 2)
        {
            shell.WriteLine(Help);
            return;
        }
        if (!float.TryParse(args[0], out var xStart))
        {
            shell.WriteError(LocalizationManager.GetString($"cmd-{Command}-error-start"));
            return;
        }
        if (!float.TryParse(args[1], out var xEnd))
        {
            shell.WriteError(LocalizationManager.GetString($"cmd-{Command}-error-end"));
            return;
        }
        if (xStart == xEnd)
        {
            shell.WriteError(LocalizationManager.GetString($"cmd-{Command}-error-zero"));
            return;
        }
        var sys = _entitySystemManager.GetEntitySystem<AtmosDebugOverlaySystem>();
        sys.CfgBase = xStart;
        sys.CfgScale = xEnd - xStart;
    }
}

[UsedImplicitly]
internal sealed class AtvModeCommand : LocalizedCommands
{
    [Dependency] private readonly IEntitySystemManager _entitySystemManager = default!;

    public override string Command => "atvmode";

    public override string Help => LocalizationManager.GetString($"cmd-{Command}-help", ("command", Command));

    public override void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        if (args.Length < 1)
        {
            shell.WriteLine(Help);
            return;
        }
        if (!Enum.TryParse<AtmosDebugOverlayMode>(args[0], out var xMode))
        {
            shell.WriteError(LocalizationManager.GetString($"cmd-{Command}-error-invalid"));
            return;
        }
        int xSpecificGas = 0;
        float xBase = 0;
        float xScale = Atmospherics.MolesCellStandard * 2;
        if (xMode == AtmosDebugOverlayMode.GasMoles)
        {
            if (args.Length != 2)
            {
                shell.WriteError(LocalizationManager.GetString($"cmd-{Command}-error-target-gas"));
                return;
            }
            if (!AtmosCommandUtils.TryParseGasID(args[1], out xSpecificGas))
            {
                shell.WriteError(LocalizationManager.GetString($"cmd-{Command}-error-out-of-range"));
                return;
            }
        }
        else
        {
            if (args.Length != 1)
            {
                shell.WriteLine(LocalizationManager.GetString($"cmd-{Command}-error-info"));
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
internal sealed class AtvCBMCommand : LocalizedCommands
{
    [Dependency] private readonly IEntitySystemManager _entitySystemManager = default!;

    public override string Command => "atvcbm";

    public override string Help => LocalizationManager.GetString($"cmd-{Command}-help", ("command", Command));

    public override void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        if (args.Length != 1)
        {
            shell.WriteLine(Help);
            return;
        }
        if (!bool.TryParse(args[0], out var xFlag))
        {
            shell.WriteError(LocalizationManager.GetString($"cmd-{Command}-error"));
            return;
        }
        var sys = _entitySystemManager.GetEntitySystem<AtmosDebugOverlaySystem>();
        sys.CfgCBM = xFlag;
    }
}
