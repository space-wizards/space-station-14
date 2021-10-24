using System;
using System.Linq;
using Content.Server.Commands;
using Content.Shared.Administration;
using Robust.Shared.Console;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;

namespace Content.Server.Administration.Commands.BQL
{
    [AdminCommand(AdminFlags.Admin)]
    public class ForAllCommand : IConsoleCommand
    {
        public string Command => "forall";
        public string Description => "Runs a command over all entities with a given component";
        public string Help => "Usage: forall <comp> <command...>";
        public void Execute(IConsoleShell shell, string argStr, string[] args)
        {
            if (args.Length < 2)
            {
                shell.WriteLine(Help);
                return;
            }

            var entityManager = IoCManager.Resolve<IEntityManager>();
            var (command, entities) = BqlParser.DoEntityQuery(argStr[6..], entityManager);

            foreach (var ent in entities.ToList())
            {
                var cmds = CommandUtils.SubstituteEntityDetails(shell, ent, command).Split(";");
                foreach (var cmd in cmds)
                {
                    shell.ExecuteCommand(cmd);
                }
            }
        }
    }
}
