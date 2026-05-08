using Content.Server._FinalStand.Economy;
using Content.Server.Administration;
using Content.Shared.Administration;
using Robust.Shared.Console;

namespace Content.Server._FinalStand.Commands;

[AdminCommand(AdminFlags.Debug)]
public sealed class FSWalletCommand : LocalizedEntityCommands
{
    [Dependency] private readonly FSPlayerWalletSystem _wallet = default!;

    public override string Command => "fswallet";
    public override string Description => "DEBUG: dump all player wallet states (credits + prestige points)";
    public override string Help => "fswallet";

    public override void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        shell.WriteLine("=== FSWallet state ===");
        _wallet.DumpWallets(shell);
    }
}
