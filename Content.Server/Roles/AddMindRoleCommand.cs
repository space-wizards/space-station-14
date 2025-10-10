using Content.Server.Administration;
using Content.Shared.Administration;
using Content.Shared.Mind;
using Content.Shared.Roles;
using Content.Shared.Players;
using Robust.Server.Player;
using Robust.Shared.Console;
using Robust.Shared.Prototypes;

namespace Content.Server.Roles;

/// <summary>
/// Admin command to add a MindRole (e.g., MindRoleAssassin) to a player's mind.
/// Usage: addmindrole <username> <mindRoleProtoId>
/// Example: addmindrole Bob MindRoleAssassin
/// </summary>
[AdminCommand(AdminFlags.Admin)]
public sealed class AddMindRoleCommand : LocalizedEntityCommands
{
    [Dependency] private readonly IPlayerManager _playerManager = default!;
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly SharedRoleSystem _roles = default!;

    public override string Command => "addmindrole";

    public override void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        if (args.Length != 2)
        {
            shell.WriteLine(Loc.GetString("shell-wrong-arguments-number-need-specific",
                ("properAmount", 2),
                ("currentAmount", args.Length)));
            return;
        }

        var username = args[0];
        var mindRoleProto = args[1];

        if (!_playerManager.TryGetPlayerDataByUsername(username, out var data))
        {
            shell.WriteLine(Loc.GetString("cmd-addrole-mind-not-found"));
            return;
        }

        var mindId = data.ContentData()?.Mind;
        if (mindId is null)
        {
            shell.WriteLine(Loc.GetString("cmd-addrole-mind-not-found"));
            return;
        }

        // Validate that the prototype exists and is an entity with a MindRole component
        if (!_prototypeManager.TryIndex<EntityPrototype>(mindRoleProto, out var entProto))
        {
            shell.WriteLine($"MindRole prototype not found: {mindRoleProto}");
            return;
        }

        if (!entProto.Components.ContainsKey("MindRole"))
        {
            shell.WriteLine($"Prototype {mindRoleProto} is not a MindRole entity (missing MindRole component)");
            return;
        }

        _roles.MindAddRole(mindId.Value, mindRoleProto);
        shell.WriteLine($"Added mind role '{mindRoleProto}' to {username}.");
    }
}
