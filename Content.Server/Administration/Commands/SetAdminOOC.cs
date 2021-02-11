#nullable enable

using Content.Server.Database;
using Content.Server.Interfaces;
using Content.Shared.Administration;
using Robust.Server.Player;
using Robust.Shared.Console;
using Robust.Shared.IoC;
using Robust.Shared.Localization;
using Robust.Shared.Maths;

namespace Content.Server.Administration.Commands
{
    [AdminCommand(AdminFlags.Admin)]
    internal class SetAdminOOC : IConsoleCommand
    {
        public string Command => "setadminooc";
        public string Description => Loc.GetString($"Sets the color of your OOC messages. Color must be in hex format, example: {Command} #c43b23");
        public string Help => Loc.GetString($"Usage: {Command} <color>");

        public void Execute(IConsoleShell shell, string argStr, string[] args)
        {
            if (!(shell.Player is IPlayerSession))
            {
                shell.WriteError(Loc.GetString("Only players can use this command"));
                return;
            }

            if (args.Length < 1)
                return;

            var colorArg = string.Join(" ", args).Trim();
            if (string.IsNullOrEmpty(colorArg))
                return;

            var color = Color.TryFromHex(colorArg);
            if (!color.HasValue)
            {
                shell.WriteError(Loc.GetString("Invalid color hex!"));
                return;
            }

            var userId = shell.Player.UserId;
            // Save the DB
            var dbMan = IoCManager.Resolve<IServerDbManager>();
            dbMan.SaveAdminOOCColorAsync(userId, color.Value);
            // Update the cached preference
            var prefManager = IoCManager.Resolve<IServerPreferencesManager>();
            var prefs = prefManager.GetPreferences(userId);
            prefs.AdminOOCColor = color.Value;
        }
    }
}
