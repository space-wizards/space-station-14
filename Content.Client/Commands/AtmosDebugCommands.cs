using JetBrains.Annotations;
using Content.Shared.Atmos;
using System;
using Content.Client.Atmos.EntitySystems;
using Robust.Shared.Console;
using Robust.Shared.GameObjects;

namespace Content.Client.Commands
{
    [UsedImplicitly]
    internal sealed class AtvRangeCommand : IConsoleCommand
    {
        public string Command => "atvrange";
        public string Description => "Sets the atmos debug range (as two floats, start [red] and end [blue])";
        public string Help => "atvrange <start> <end>";
        public void Execute(IConsoleShell shell, string argStr, string[] args)
        {
            if (args.Length != 2)
            {
                shell.WriteLine(Help);
                return;
            }
            if (!float.TryParse(args[0], out var xStart))
            {
                shell.WriteLine("Bad float START");
                return;
            }
            if (!float.TryParse(args[1], out var xEnd))
            {
                shell.WriteLine("Bad float END");
                return;
            }
            if (xStart == xEnd)
            {
                shell.WriteLine("Scale cannot be zero, as this would cause a division by zero in AtmosDebugOverlay.");
                return;
            }
            var sys = EntitySystem.Get<AtmosDebugOverlaySystem>();
            sys.CfgBase = xStart;
            sys.CfgScale = xEnd - xStart;
        }
    }

    [UsedImplicitly]
    internal sealed class AtvModeCommand : IConsoleCommand
    {
        public string Command => "atvmode";
        public string Description => "Sets the atmos debug mode. This will automatically reset the scale.";
        public string Help => "atvmode <TotalMoles/GasMoles/Temperature> [<gas ID (for GasMoles)>]";
        public void Execute(IConsoleShell shell, string argStr, string[] args)
        {
            if (args.Length < 1)
            {
                shell.WriteLine(Help);
                return;
            }
            if (!Enum.TryParse<AtmosDebugOverlayMode>(args[0], out var xMode))
            {
                shell.WriteLine("Invalid mode");
                return;
            }
            int xSpecificGas = 0;
            float xBase = 0;
            float xScale = Atmospherics.MolesCellStandard * 2;
            if (xMode == AtmosDebugOverlayMode.GasMoles)
            {
                if (args.Length != 2)
                {
                    shell.WriteLine("A target gas must be provided for this mode.");
                    return;
                }
                if (!AtmosCommandUtils.TryParseGasID(args[1], out xSpecificGas))
                {
                    shell.WriteLine("Gas ID not parsable or out of range.");
                    return;
                }
            }
            else
            {
                if (args.Length != 1)
                {
                    shell.WriteLine("No further information is required for this mode.");
                    return;
                }
                if (xMode == AtmosDebugOverlayMode.Temperature)
                {
                    // Red is 100C, Green is 20C, Blue is -60C
                    xBase = Atmospherics.T20C + 80;
                    xScale = -160;
                }
            }
            var sys = EntitySystem.Get<AtmosDebugOverlaySystem>();
            sys.CfgMode = xMode;
            sys.CfgSpecificGas = xSpecificGas;
            sys.CfgBase = xBase;
            sys.CfgScale = xScale;
        }
    }

    [UsedImplicitly]
    internal sealed class AtvCBMCommand : IConsoleCommand
    {
        public string Command => "atvcbm";
        public string Description => "Changes from red/green/blue to greyscale";
        public string Help => "atvcbm <true/false>";
        public void Execute(IConsoleShell shell, string argStr, string[] args)
        {
            if (args.Length != 1)
            {
                shell.WriteLine(Help);
                return;
            }
            if (!bool.TryParse(args[0], out var xFlag))
            {
                shell.WriteLine("Invalid flag");
                return;
            }
            var sys = EntitySystem.Get<AtmosDebugOverlaySystem>();
            sys.CfgCBM = xFlag;
        }
    }
}
