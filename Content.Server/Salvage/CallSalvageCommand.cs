using Content.Shared.Administration;
using Content.Server.Administration;
using Robust.Shared.Console;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Localization;

namespace Content.Server.Salvage
{
    [AdminCommand(AdminFlags.Admin)]
    public class CallSalvageCommand : IConsoleCommand
    {
        public string Command => "callsalvage";
        public string Description => "Starts salvage.";
        public string Help => "Usage: callsalvage";

        public void Execute(IConsoleShell shell, string argStr, string[] args)
        {
            if (args.Length != 0)
            {
                shell.WriteError(Loc.GetString("shell-wrong-arguments-number"));
                return;
            }
            shell.WriteLine(EntitySystem.Get<SalvageSystem>().CallSalvage());
        }
    }

    [AdminCommand(AdminFlags.Admin)]
    public class RecallSalvageCommand : IConsoleCommand
    {
        public string Command => "recallsalvage";
        public string Description => "Forcibly recalls salvage.";
        public string Help => "Usage: recallsalvage";

        public void Execute(IConsoleShell shell, string argStr, string[] args)
        {
            if (args.Length != 0)
            {
                shell.WriteError(Loc.GetString("shell-wrong-arguments-number"));
                return;
            }
            shell.WriteLine(EntitySystem.Get<SalvageSystem>().ReturnSalvage());
        }
    }

    [AdminCommand(AdminFlags.Admin)]
    public class RecallSalvageNowCommand : IConsoleCommand
    {
        public string Command => "recallsalvagefinishnow";
        public string Description => "Forcibly stops salvage immediately (will delete - good for testing).";
        public string Help => "Usage: recallsalvagefinishnow";

        public void Execute(IConsoleShell shell, string argStr, string[] args)
        {
            if (args.Length != 0)
            {
                shell.WriteError(Loc.GetString("shell-wrong-arguments-number"));
                return;
            }
            var salvageSystem = EntitySystem.Get<SalvageSystem>();
            if ((salvageSystem.State != SalvageSystemState.Active) && (salvageSystem.State != SalvageSystemState.LettingGo))
            {
                shell.WriteError("Incorrect state (must be in active or letting-go state)");
                return;
            }
            salvageSystem.DeleteSalvage();
        }
    }
}

