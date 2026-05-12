using System.Linq;
using Content.Server.Database;
using Content.Server.Players.JobWhitelist;
using Content.Shared.Administration;
using Content.Shared.Roles;
using Robust.Server.Player;
using Robust.Shared.Console;
using Robust.Shared.Prototypes;

namespace Content.Server.Administration.Commands;

[AdminCommand(AdminFlags.Ban)]
public sealed class JobWhitelistAddCommand : LocalizedCommands
{
    [Dependency] private readonly IServerDbManager _db = default!;
    [Dependency] private readonly JobWhitelistManager _jobWhitelist = default!;
    [Dependency] private readonly IPlayerLocator _playerLocator = default!;
    [Dependency] private readonly IPlayerManager _players = default!;
    [Dependency] private readonly IPrototypeManager _prototypes = default!;

    public override string Command => "jobwhitelistadd";

    public override async void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        if (args.Length != 2)
        {
            shell.WriteError(Loc.GetString("shell-wrong-arguments-number-need-specific",
                ("properAmount", 2),
                ("currentAmount", args.Length)));
            shell.WriteLine(Help);
            return;
        }

        var player = args[0].Trim();
        var job = new ProtoId<JobPrototype>(args[1].Trim());
        if (!_prototypes.TryIndex(job, out var jobPrototype))
        {
            shell.WriteError(Loc.GetString("cmd-jobwhitelist-job-does-not-exist", ("job", job.Id)));
            shell.WriteLine(Help);
            return;
        }

        var data = await _playerLocator.LookupIdByNameAsync(player);
        if (data != null)
        {
            var guid = data.UserId;
            var isWhitelisted = await _db.IsJobWhitelisted(guid, job);
            if (isWhitelisted)
            {
                shell.WriteLine(Loc.GetString("cmd-jobwhitelistadd-already-whitelisted",
                    ("player", player),
                    ("jobId", job.Id),
                    ("jobName", jobPrototype.LocalizedName)));
                return;
            }

            _jobWhitelist.AddWhitelist(guid, job);
            shell.WriteLine(Loc.GetString("cmd-jobwhitelistadd-added",
                ("player", player),
                ("jobId", job.Id),
                ("jobName", jobPrototype.LocalizedName)));
            return;
        }

        shell.WriteError(Loc.GetString("cmd-jobwhitelist-player-not-found", ("player", player)));
    }

    public override CompletionResult GetCompletion(IConsoleShell shell, string[] args)
    {
        if (args.Length == 1)
        {
            return CompletionResult.FromHintOptions(
                _players.Sessions.Select(s => s.Name),
                Loc.GetString("cmd-jobwhitelist-hint-player"));
        }

        if (args.Length == 2)
        {
            return CompletionResult.FromHintOptions(
                _prototypes.EnumeratePrototypes<JobPrototype>().Select(p => p.ID),
                Loc.GetString("cmd-jobwhitelist-hint-job"));
        }

        return CompletionResult.Empty;
    }
}

[AdminCommand(AdminFlags.Ban)]
public sealed class GetJobWhitelistCommand : LocalizedCommands
{
    [Dependency] private readonly IServerDbManager _db = default!;
    [Dependency] private readonly IPlayerLocator _playerLocator = default!;
    [Dependency] private readonly IPlayerManager _players = default!;

    public override string Command => "jobwhitelistget";

    public override async void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        if (args.Length == 0)
        {
            shell.WriteError("This command needs at least one argument.");
            shell.WriteLine(Help);
            return;
        }

        var player = string.Join(' ', args).Trim();
        var data = await _playerLocator.LookupIdByNameAsync(player);
        if (data != null)
        {
            var guid = data.UserId;
            var whitelists = await _db.GetJobWhitelists(guid);
            if (whitelists.Count == 0)
            {
                shell.WriteLine(Loc.GetString("cmd-jobwhitelistget-whitelisted-none", ("player", player)));
                return;
            }

            shell.WriteLine(Loc.GetString("cmd-jobwhitelistget-whitelisted-for",
                ("player", player),
                ("jobs", string.Join(", ", whitelists))));
            return;
        }

        shell.WriteError(Loc.GetString("cmd-jobwhitelist-player-not-found", ("player", player)));
    }

    public override CompletionResult GetCompletion(IConsoleShell shell, string[] args)
    {
        if (args.Length == 1)
        {
            return CompletionResult.FromHintOptions(
                _players.Sessions.Select(s => s.Name),
                Loc.GetString("cmd-jobwhitelist-hint-player"));
        }

        return CompletionResult.Empty;
    }
}

[AdminCommand(AdminFlags.Ban)]
public sealed class RemoveJobWhitelistCommand : LocalizedCommands
{
    [Dependency] private readonly IServerDbManager _db = default!;
    [Dependency] private readonly JobWhitelistManager _jobWhitelist = default!;
    [Dependency] private readonly IPlayerLocator _playerLocator = default!;
    [Dependency] private readonly IPlayerManager _players = default!;
    [Dependency] private readonly IPrototypeManager _prototypes = default!;

    public override string Command => "jobwhitelistremove";

    public override async void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        if (args.Length != 2)
        {
            shell.WriteError(Loc.GetString("shell-wrong-arguments-number-need-specific",
                ("properAmount", 2),
                ("currentAmount", args.Length)));
            shell.WriteLine(Help);
            return;
        }

        var player = args[0].Trim();
        var job = new ProtoId<JobPrototype>(args[1].Trim());
        if (!_prototypes.TryIndex(job, out var jobPrototype))
        {
            shell.WriteError(Loc.GetString("cmd-jobwhitelist-job-does-not-exist", ("job", job)));
            shell.WriteLine(Help);
            return;
        }

        var data = await _playerLocator.LookupIdByNameAsync(player);
        if (data != null)
        {
            var guid = data.UserId;
            var isWhitelisted = await _db.IsJobWhitelisted(guid, job);
            if (!isWhitelisted)
            {
                shell.WriteError(Loc.GetString("cmd-jobwhitelistremove-was-not-whitelisted",
                    ("player", player),
                    ("jobId", job.Id),
                    ("jobName", jobPrototype.LocalizedName)));
                return;
            }

            _jobWhitelist.RemoveWhitelist(guid, job);
            shell.WriteLine(Loc.GetString("cmd-jobwhitelistremove-removed",
                ("player", player),
                ("jobId", job.Id),
                ("jobName", jobPrototype.LocalizedName)));
            return;
        }

        shell.WriteError(Loc.GetString("cmd-jobwhitelist-player-not-found", ("player", player)));
    }

    public override CompletionResult GetCompletion(IConsoleShell shell, string[] args)
    {
        if (args.Length == 1)
        {
            return CompletionResult.FromHintOptions(
                _players.Sessions.Select(s => s.Name),
                Loc.GetString("cmd-jobwhitelist-hint-player"));
        }

        if (args.Length == 2)
        {
            return CompletionResult.FromHintOptions(
                _prototypes.EnumeratePrototypes<JobPrototype>().Select(p => p.ID),
                Loc.GetString("cmd-jobwhitelist-hint-job"));
        }

        return CompletionResult.Empty;
    }
}
