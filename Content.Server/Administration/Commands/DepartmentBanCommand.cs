using Content.Server.Administration.Managers;
using Content.Shared.Administration;
using Content.Shared.Roles;
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

        foreach (var job in departmentProto.Roles)
        {
            banManager.CreateJobBan(shell, target, job, reason, minutes);
        }
    }

    public CompletionResult GetCompletion(IConsoleShell shell, string[] args)
    {
        var durOpts = new CompletionOption[]
        {
            new("0", Loc.GetString("cmd-roleban-hint-duration-1")),
            new("1440", Loc.GetString("cmd-roleban-hint-duration-2")),
            new("10080", Loc.GetString("cmd-roleban-hint-duration-3")),
        };

        return args.Length switch
        {
            1 => CompletionResult.FromHintOptions(CompletionHelper.SessionNames(),
                Loc.GetString("cmd-roleban-hint-1")),
            2 => CompletionResult.FromHintOptions(CompletionHelper.PrototypeIDs<DepartmentPrototype>(),
                Loc.GetString("cmd-roleban-hint-2")),
            3 => CompletionResult.FromHint(Loc.GetString("cmd-roleban-hint-3")),
            4 => CompletionResult.FromHintOptions(durOpts, Loc.GetString("cmd-roleban-hint-4")),
            _ => CompletionResult.Empty
        };
    }

}
