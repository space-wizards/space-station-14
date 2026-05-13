using Content.Server.Administration;
using Content.Server.Atmos.EntitySystems;
using Content.Shared.Administration;
using Robust.Shared.Console;

namespace Content.Server.Atmos.Commands
{
    [AdminCommand(AdminFlags.Debug)]
    public sealed partial class ListGasesCommand : LocalizedEntityCommands
    {
        [Dependency] private AtmosphereSystem _atmosSystem = default!;

        public override string Command => "listgases";

        public override void Execute(IConsoleShell shell, string argStr, string[] args)
        {
            foreach (var gasPrototype in _atmosSystem.Gases)
            {
                var gasName = Loc.GetString(gasPrototype.Name);
                shell.WriteLine(Loc.GetString("cmd-listgases-gas", ("gas", gasName), ("id", gasPrototype.ID)));
            }
        }
    }

}

