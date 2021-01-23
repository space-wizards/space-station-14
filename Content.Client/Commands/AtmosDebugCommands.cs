using JetBrains.Annotations;
using Content.Client.GameObjects.EntitySystems;
using Robust.Shared.GameObjects.Systems;
using Content.Shared.Atmos;
using System;
using Robust.Client.Console;

namespace Content.Client.Commands
{
    [UsedImplicitly]
    internal sealed class AtvRangeCommand : IClientCommand
    {
        public string Command => "atvrange";
        public string Description => "Sets the atmos debug range (as two floats, start [red] and end [blue])";
        public string Help => "atvrange <start> <end>";
        public bool Execute(IClientConsoleShell shell, string[] args)
        {
            if (args.Length != 2)
            {
                shell.WriteLine(Help);
                return false;
            }
            if (!float.TryParse(args[0], out var xStart))
            {
                shell.WriteLine("Bad float START");
                return false;
            }
            if (!float.TryParse(args[1], out var xEnd))
            {
                shell.WriteLine("Bad float END");
                return false;
            }
            if (xStart == xEnd)
            {
                shell.WriteLine("Scale cannot be zero, as this would cause a division by zero in AtmosDebugOverlay.");
                return false;
            }
            var sys = EntitySystem.Get<AtmosDebugOverlaySystem>();
            sys.CfgBase = xStart;
            sys.CfgScale = xEnd - xStart;
            return false;
        }
    }

    [UsedImplicitly]
    internal sealed class AtvModeCommand : IClientCommand
    {
        public string Command => "atvmode";
        public string Description => "Sets the atmos debug mode. This will automatically reset the scale.";
        public string Help => "atvmode <TotalMoles/GasMoles/Temperature> [<gas ID (for GasMoles)>]";
        public bool Execute(IClientConsoleShell shell, string[] args)
        {
            if (args.Length < 1)
            {
                shell.WriteLine(Help);
                return false;
            }
            if (!Enum.TryParse<AtmosDebugOverlayMode>(args[0], out var xMode))
            {
                shell.WriteLine("Invalid mode");
                return false;
            }
            int xSpecificGas = 0;
            float xBase = 0;
            float xScale = Atmospherics.MolesCellStandard * 2;
            if (xMode == AtmosDebugOverlayMode.GasMoles)
            {
                if (args.Length != 2)
                {
                    shell.WriteLine("A target gas must be provided for this mode.");
                    return false;
                }
                if (!AtmosCommandUtils.TryParseGasID(args[1], out xSpecificGas))
                {
                    shell.WriteLine("Gas ID not parsable or out of range.");
                    return false;
                }
            }
            else
            {
                if (args.Length != 1)
                {
                    shell.WriteLine("No further information is required for this mode.");
                    return false;
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
            return false;
        }
    }

    [UsedImplicitly]
    internal sealed class AtvCBMCommand : IClientCommand
    {
        public string Command => "atvcbm";
        public string Description => "Changes from red/green/blue to greyscale";
        public string Help => "atvcbm <true/false>";
        public bool Execute(IClientConsoleShell shell, string[] args)
        {
            if (args.Length != 1)
            {
                shell.WriteLine(Help);
                return false;
            }
            if (!bool.TryParse(args[0], out var xFlag))
            {
                shell.WriteLine("Invalid flag");
                return false;
            }
            var sys = EntitySystem.Get<AtmosDebugOverlaySystem>();
            sys.CfgCBM = xFlag;
            return false;
        }
    }
}
