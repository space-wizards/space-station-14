using Content.Server.Administration;
using Content.Server.NPC.HTN;
using Content.Shared.Administration;
using Robust.Shared.Console;

namespace Content.Server.NPC.Commands
{
    [AdminCommand(AdminFlags.Fun)]
    public sealed class AddNpcCommand : LocalizedEntityCommands
    {
        public override string Command => "addnpc";

        public override void Execute(IConsoleShell shell, string argStr, string[] args)
        {
            if (args.Length != 2)
            {
                shell.WriteLine(Loc.GetString("shell-wrong-arguments-number-need-specific",
                    ("$properAmount", 2),
                    ("currentAmount", args.Length)));
                return;
            }

            var nent = new NetEntity(int.Parse(args[0]));

            if (!EntityManager.TryGetEntity(nent, out var entId))
            {
                shell.WriteError(Loc.GetString($"shell-could-not-find-entity-with-uid", ("uid", args[0])));
                return;
            }

            if (EntityManager.HasComponent<HTNComponent>(entId))
            {
                shell.WriteError(Loc.GetString($"cmd-addnpc-entity-has-component"));
                return;
            }

            var comp = EntityManager.AddComponent<HTNComponent>(entId.Value);
            comp.RootTask = new HTNCompoundTask()
            {
                Task = args[1],
            };
            shell.WriteLine(Loc.GetString($"cmd-addnpc-success"));
        }
    }
}
