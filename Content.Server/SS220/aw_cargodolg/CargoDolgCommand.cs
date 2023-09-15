// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt
// Created special for SS200 with love by Alan Wake (https://github.com/aw-c)

using Content.Server.Administration;
using Content.Shared.Administration;
using Robust.Shared.Console;
using Content.Server.Cargo.Systems;
using Content.Server.Cargo.Components;
using System.Linq;

namespace Content.Server.Cargo.Commands
{
    [AdminCommand(AdminFlags.Admin)]
    public sealed class CargoMoneyCommand : IConsoleCommand
    {
        [Dependency] private readonly IEntitySystemManager _entitySystemManager = default!;
        [Dependency] private readonly IEntityManager _entityManager = default!;

        public string Command => "cargomoney";
        public string Description => "Grant access to manipulate cargo's money.";
        public string Help => $"Usage: {Command} <set || add || rem> <amount>";

        public void Execute(IConsoleShell shell, string argStr, string[] args)
        {
            if (args.Length == 2)
            {
                bool bSet = false;

                if (int.TryParse(args[1], out var toAdd))
                {
                    switch (args[0])
                    {
                        case "set":
                            bSet = true;
                            break;
                        case "add":
                            break;
                        case "rem":
                            toAdd = -toAdd;
                            break;
                        default:
                            goto invalidArgs;
                    }

                    ProccessMoney(shell, toAdd, bSet);
                    return;
                }
            }
        invalidArgs:
            shell.WriteLine("Expected invalid arguments!");
        }

        public CompletionResult GetCompletion(IConsoleShell shell, string[] args)
        {
            CompletionResult res = CompletionResult.Empty;
            switch (args.Length)
            {
                case 1:
                    res = CompletionResult.FromHint("set || add || rem");
                    break;
                case 2:
                    res = CompletionResult.FromHint("amount");
                    break;
            }

            return res;
        }

        private void ProccessMoney(IConsoleShell shell, int money, bool bSet)
        {
            var cargoSystem = _entitySystemManager.GetEntitySystem<CargoSystem>();
            var bankQuery = _entityManager.EntityQueryEnumerator<StationBankAccountComponent>();

            bankQuery.MoveNext(out var owner, out var bankComponent);
            if (!_entityManager.EntityExists(owner) || bankComponent is null)
                return;

            var currentMoney = bankComponent.Balance;

            cargoSystem.UpdateBankAccount(owner, bankComponent, -currentMoney);
            cargoSystem.UpdateBankAccount(owner, bankComponent, bSet ? money : currentMoney + money);

            shell.WriteLine($"Successfully changed cargo's money to {bankComponent.Balance}");
        }
    }
}
