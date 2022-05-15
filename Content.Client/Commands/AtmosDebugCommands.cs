using JetBrains.Annotations;
using Content.Shared.Atmos;
using Content.Client.Atmos.EntitySystems;
using Robust.Shared.Console;

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
    internal sealed class AtvStyleCommand : IConsoleCommand
    {
        public string Command => "atvstyle";
        public string Description => "Sets the atmos debug overlay display style.";
        public string Help => Command + " <" + string.Join("|", Enum.GetNames(typeof(AtmosDebugStyle))) + ">";
        public void Execute(IConsoleShell shell, string argStr, string[] args)
        {
            if (args.Length != 1)
            {
                shell.WriteLine(Help);
                return;
            }
            if (!Enum.TryParse<AtmosDebugStyle>(args[0], out var style))
            {
                shell.WriteLine("Invalid style");
                return;
            }
            var sys = EntitySystem.Get<AtmosDebugOverlaySystem>();
            sys.CfgStyle = style;
        }
    }

    [UsedImplicitly]
    internal sealed class AtvModeCommand : IConsoleCommand
    {
        public string Command => "atvmode";
        public string Description => "Adjust what to display in the atmos debug overlay.";
        public string Help => Command + " <" + string.Join("|", Enum.GetNames(typeof(AtmosDebugShowMode))) + "> [<gas ID (for GasMoles)>]";
        public void Execute(IConsoleShell shell, string argStr, string[] args)
        {
            var sys = EntitySystem.Get<AtmosDebugOverlaySystem>();

            if (args.Length < 1)
            {
                shell.WriteLine(Help);
                shell.WriteLine("The current modes are: " + sys.CfgMode);
                return;
            }
            if (!Enum.TryParse<AtmosDebugShowMode>(args[0], out var xMode))
            {
                shell.WriteLine("Invalid mode");
                return;
            }

            sys.SwitchMode(xMode);

            if (sys.CfgStyle == AtmosDebugStyle.Graph)
                return;

            if (xMode == AtmosDebugShowMode.GasMoles)
            {
                if (args.Length == 2)
                {
                    if (!AtmosCommandUtils.TryParseGasID(args[1], out var xSpecificGas))
                    {
                        shell.WriteLine("Gas ID not parsable or out of range.");
                        return;
                    }
                    sys.CfgSpecificGas = xSpecificGas;
                }
            }
            else
            {
                if (args.Length != 1)
                {
                    shell.WriteLine("No further information is required for this mode.");
                }
            }
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
