using System.Text;
using Content.Server.Administration.Managers;
using Content.Server.Administration.Notes;
using Content.Shared.Administration;
using Content.Shared.Database;
using Content.Shared.Roles;
using Robust.Server.Player;
using Robust.Shared.Console;
using Robust.Shared.Prototypes;

namespace Content.Server.Administration.Commands;

[AdminCommand(AdminFlags.Ban)]
public sealed class DepartmentBanCommand : IConsoleCommand
{
    public string Command => "departmentban";
    public string Description => Loc.GetString("cmd-departmentban-desc");
    public string Help => Loc.GetString("cmd-departmentban-help");

    public async void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        string target;
        string department;
        string reason;
        uint minutes;
        var severity = NoteSeverity.Medium;

        switch (args.Length)
        {
            case 3:
                target = args[0];
                department = args[1];
                reason = args[2];
                minutes = 0;
                break;
            case 4:
                target = args[0];
                department = args[1];
                reason = args[2];

                if (!uint.TryParse(args[3], out minutes))
                {
                    shell.WriteError(Loc.GetString("cmd-roleban-minutes-parse", ("time", args[3]), ("help", Help)));
                    return;
                }

                break;
            case 5:
                target = args[0];
                department = args[1];
                reason = args[2];

                if (!uint.TryParse(args[3], out minutes))
                {
                    shell.WriteError(Loc.GetString("cmd-roleban-minutes-parse", ("time", args[3]), ("help", Help)));
                    return;
                }

                if (!Enum.TryParse(args[4], ignoreCase: true, out severity))
                {
                    shell.WriteLine(Loc.GetString("cmd-roleban-severity-parse", ("severity", args[4]), ("help", Help)));
                    return;
                }

                break;
            default:
                shell.WriteError(Loc.GetString("cmd-roleban-arg-count"));
                shell.WriteLine(Help);
                return;
        }

        var protoManager = IoCManager.Resolve<IPrototypeManager>();

        if (!protoManager.TryIndex<DepartmentPrototype>(department, out var departmentProto))
        {
            return;
        }

        var banManager = IoCManager.Resolve<RoleBanManager>();

        // I know this code is horrible. This is temporary, I promise.
        foreach (var job in departmentProto.Roles)
        {
            banManager.CreateJobBan(shell, target, job, reason, minutes, severity, false);
        }

        var playerLocator = IoCManager.Resolve<IPlayerLocator>();
        var located = await playerLocator.LookupIdByNameOrIdAsync(target);
        if (located?.LastAddress is null)
            return;
        var adminNotesManager = IoCManager.Resolve<IAdminNotesManager>();
        var banMessage = new StringBuilder($"Banned from {department} ");
        if (minutes == 0)
        {
            banMessage.Append("permanently");
        }
        else
        {
            var banLength = TimeSpan.FromMinutes(minutes);
            if (banLength.Days > 0)
                banMessage.Append($"{banLength.TotalDays} days");
            else if (banLength.Hours > 0)
                banMessage.Append($"{banLength.TotalHours} hours");
            else
                banMessage.Append($"{minutes} minutes");
        }

        banMessage.Append(" - ");
        banMessage.Append(reason);

        if (shell.Player is not IPlayerSession player)
        {
            Logger.WarningS("admin.notes", "While creating a department ban, player was null. A note could not be added.");
            return;
        }

        await adminNotesManager.AddNote(player, located.UserId, NoteType.Note, banMessage.ToString(), severity, false, null);
    }

    public CompletionResult GetCompletion(IConsoleShell shell, string[] args)
    {
        var durOpts = new CompletionOption[]
        {
            new("0", Loc.GetString("cmd-roleban-hint-duration-1")),
            new("1440", Loc.GetString("cmd-roleban-hint-duration-2")),
            new("4320", Loc.GetString("cmd-roleban-hint-duration-3")),
            new("10080", Loc.GetString("cmd-roleban-hint-duration-4")),
            new("20160", Loc.GetString("cmd-roleban-hint-duration-5")),
            new("43800", Loc.GetString("cmd-roleban-hint-duration-6")),
        };

        var severities = new CompletionOption[]
        {
            new("none", Loc.GetString("admin-note-editor-severity-none")),
            new("minor", Loc.GetString("admin-note-editor-severity-low")),
            new("medium", Loc.GetString("admin-note-editor-severity-medium")),
            new("high", Loc.GetString("admin-note-editor-severity-high")),
        };

        return args.Length switch
        {
            1 => CompletionResult.FromHintOptions(CompletionHelper.SessionNames(),
                Loc.GetString("cmd-roleban-hint-1")),
            2 => CompletionResult.FromHintOptions(CompletionHelper.PrototypeIDs<DepartmentPrototype>(),
                Loc.GetString("cmd-roleban-hint-2")),
            3 => CompletionResult.FromHint(Loc.GetString("cmd-roleban-hint-3")),
            4 => CompletionResult.FromHintOptions(durOpts, Loc.GetString("cmd-roleban-hint-4")),
            5 => CompletionResult.FromHintOptions(severities, Loc.GetString("cmd-roleban-hint-5")),
            _ => CompletionResult.Empty
        };
    }

}
