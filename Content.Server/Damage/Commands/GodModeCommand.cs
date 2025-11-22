using Content.Server.Administration;
using Content.Shared.Administration;
using Content.Shared.Damage.Systems;
using Robust.Shared.Console;

namespace Content.Server.Damage.Commands
{
    [AdminCommand(AdminFlags.Fun)]
    public sealed class GodModeCommand : LocalizedEntityCommands
    {
        [Dependency] private readonly SharedGodmodeSystem _godmodeSystem = default!;

        public override string Command => "godmode";

        public override void Execute(IConsoleShell shell, string argStr, string[] args)
        {
            var player = shell.Player;
            EntityUid entity;

            switch (args.Length)
            {
                case 0:
                    if (player == null)
                    {
                        shell.WriteLine(Loc.GetString("cmd-godmode-only-player-run"));
                        return;
                    }

                    if (player.AttachedEntity == null)
                    {
                        shell.WriteLine(Loc.GetString("cmd-godmode-no-entity"));
                        return;
                    }

                    entity = player.AttachedEntity.Value;
                    break;
                case 1:
                    if (!NetEntity.TryParse(args[0], out var idNet) || !EntityManager.TryGetEntity(idNet, out var id))
                    {
                        shell.WriteLine(Loc.GetString("cmd-godmode-invalid-entity-uid", ("uid", args[0])));
                        return;
                    }

                    if (!EntityManager.EntityExists(id))
                    {
                        shell.WriteLine(Loc.GetString("cmd-godmode-entity-not-found", ("id", id)));
                        return;
                    }

                    entity = id.Value;
                    break;
                default:
                    shell.WriteLine(Help);
                    return;
            }

            var enabled = _godmodeSystem.ToggleGodmode(entity);

            var name = EntityManager.GetComponent<MetaDataComponent>(entity).EntityName;

            shell.WriteLine(enabled
                ? Loc.GetString("cmd-godmode-enabled", ("name", name), ("entity", entity))
                : Loc.GetString("cmd-godmode-disabled", ("name", name), ("entity", entity)));
        }
    }
}
