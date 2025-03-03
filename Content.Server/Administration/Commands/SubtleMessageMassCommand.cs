using System.Linq;
using Content.Server.Administration.Managers;
using Content.Shared.Administration;
using Robust.Server.Player;
using Robust.Shared.Configuration;
using Robust.Shared.Console;
using Content.Server.Prayer;


namespace Content.Server.Administration.Commands;

[AdminCommand(AdminFlags.Admin)]
public sealed class SubtleMessageMassCommand : LocalizedEntityCommands
{
    [Dependency] private readonly IPlayerLocator _locator = default!;
    [Dependency] private readonly IBanManager _bans = default!;
    [Dependency] private readonly IConfigurationManager _cfg = default!;
    [Dependency] private readonly PrayerSystem _prayerSystem = default!;
    [Dependency] private readonly IPlayerManager _playerManager = default!;

    public override string Command => "massmsg";
    public override string Description => Loc.GetString("massmsg-command-description");
    public override string Help => Loc.GetString("massmsg-command-help-text", ("command", Command));

    public override async void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        if (args.Length < 3)
        {
            shell.WriteLine(Loc.GetString("cmd-ban-invalid-arguments"));
            shell.WriteLine(Help);
            return;
        }

        var player = shell.Player;
        if (player == null)
        {
            shell.WriteLine("You cannot use this command from the server console.");
            return;
        }

        var message = args[0];
        var popupMessage = args[1];

        string[]? allTargets;
        if (args[2] == "all")
        {
            allTargets = CompletionHelper.SessionNames()
                                         .Select(option => option.Value)
                                         .ToArray();
        }
        else
        {
            allTargets = args.Skip(2).Where(arg => !string.IsNullOrWhiteSpace(arg)).ToArray();
        }

        foreach (var target in allTargets)
        {
            var trimmedTarget = target.Trim();
            if (string.IsNullOrWhiteSpace(trimmedTarget))
                continue;

            var located = await _locator.LookupIdByNameOrIdAsync(trimmedTarget);

            if (located == null)
            {
                shell.WriteError(Loc.GetString("massmsg-player-unable"));
                continue;
            }

            var targetSession = _playerManager.GetSessionById(located.UserId);
            _prayerSystem.SendSubtleMessage(targetSession, player, message, popupMessage);
        }
    }

    public override CompletionResult GetCompletion(IConsoleShell shell, string[] args)
    {
        if (args.Length >= 3)
            return CompletionResult.FromHintOptions(
                CompletionHelper.SessionNames(),
                Loc.GetString("massmsg-command-hint")
            );

        if (args.Length == 1)
            return CompletionResult.FromHint(Loc.GetString("massmsg-command-hint-one-args"));

        if (args.Length == 2)
        {
            var option = new CompletionOption[]
            {
                new(Loc.GetString("prayer-popup-subtle-default"), Loc.GetString("default")),
            };

            return CompletionResult.FromHintOptions(
                option,
                Loc.GetString("massmsg-command-hint-second-args")
            );
        }

        return CompletionResult.Empty;
    }
}
