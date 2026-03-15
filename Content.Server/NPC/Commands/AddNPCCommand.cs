using Content.Server.Administration;
using Content.Server.NPC.HTN;
using Content.Shared.Administration;
using Robust.Shared.Console;

namespace Content.Server.NPC.Commands
{
    [AdminCommand(AdminFlags.Fun)]
    public sealed class AddNPCCommand : LocalizedCommands
    {
        [Dependency] private readonly IEntityManager _entities = default!;

        public override string Command => "addnpc";

        public override void Execute(IConsoleShell shell, string argStr, string[] args)
        {
            if (args.Length != 2)
            {
                shell.WriteError(Loc.GetString("shell-wrong-arguments-number"));
                return;
            }

            var nent = new NetEntity(int.Parse(args[0]));

            if (!_entities.TryGetEntity(nent, out var entId))
            {
                shell.WriteError(Loc.GetString("shell-invalid-entity-uid", ("uid", args[0])));
                return;
            }

            if (_entities.HasComponent<HTNComponent>(entId))
            {
                shell.WriteError(Loc.GetString("cmd-addnpc-already-has-npc"));
                return;
            }

            var comp = _entities.AddComponent<HTNComponent>(entId.Value);
            comp.RootTask = new HTNCompoundTask()
            {
                Task = args[1]
            };
            shell.WriteLine(Loc.GetString("cmd-addnpc-added"));
        }
    }
}
