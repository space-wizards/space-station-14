using Content.Server.Administration;
using Content.Server.Hands.Systems;
using Content.Shared.Administration;
using Content.Shared.Hands.Components;
using Robust.Shared.Console;

namespace Content.Server.Body.Commands
{
    [AdminCommand(AdminFlags.Fun)]
    sealed class AddHandCommand : LocalizedCommands
    {
        [Dependency] private readonly IEntityManager _entManager = default!;

        private static int _handIdAccumulator;

        public override string Command => "addhand";

        public override void Execute(IConsoleShell shell, string argStr, string[] args)
        {
            var player = shell.Player;

            EntityUid entity;

            switch (args.Length)
            {
                case 0:
                    if (player == null)
                    {
                        shell.WriteLine(Loc.GetString("cmd-addhand-only-player-run-without-args"));
                        return;
                    }

                    if (player.AttachedEntity == null)
                    {
                        shell.WriteLine(Loc.GetString("cmd-addhand-no-entity"));
                        return;
                    }

                    entity = player.AttachedEntity.Value;
                    break;
                case 1:
                    {
                        if (NetEntity.TryParse(args[0], out var uidNet) && _entManager.TryGetEntity(uidNet, out var uid))
                        {
                            if (!_entManager.EntityExists(uid))
                            {
                                shell.WriteLine(Loc.GetString("cmd-addhand-no-entity-uid", ("uid", uid)));
                                return;
                            }

                            entity = uid.Value;
                        }
                        else
                        {
                            if (player == null)
                            {
                                shell.WriteLine(Loc.GetString("cmd-addhand-no-entity-server-terminal"));
                                return;
                            }

                            if (player.AttachedEntity == null)
                            {
                                shell.WriteLine(Loc.GetString("cmd-addhand-no-entity"));
                                return;
                            }

                            entity = player.AttachedEntity.Value;
                        }

                        break;
                    }

                default:
                    shell.WriteLine(Help);
                    return;
            }

            _entManager.System<HandsSystem>().AddHand(entity, $"cmd-{_handIdAccumulator++}", HandLocation.Middle);

            shell.WriteLine(Loc.GetString("cmd-addhand-added-hand", ("entity", _entManager.GetComponent<MetaDataComponent>(entity).EntityName)));
        }
    }
}
