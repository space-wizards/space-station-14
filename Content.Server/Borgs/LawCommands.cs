using Content.Server.Administration;
using Content.Shared.Administration;
using Content.Shared.Borgs;
using Robust.Shared.Console;
using Content.Server.Players;
using Robust.Server.Player;

namespace Content.Server.Borgs;

[AdminCommand(AdminFlags.Logs)]
public sealed class ListLawsCommand : IConsoleCommand
{
    public string Command => "lslaws";
    public string Description => Loc.GetString("command-lslaws-description");
    public string Help => Loc.GetString("command-lslaws-help");
    public async void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        var entityManager = IoCManager.Resolve<IEntityManager>();
        var player = shell.Player as IPlayerSession;
        EntityUid? entity = null;

        if (args.Length == 0 && player != null)
        {
            entity = player.ContentData()?.Mind?.CurrentEntity;
        }
        else if (IoCManager.Resolve<IPlayerManager>().TryGetPlayerDataByUsername(args[0], out var data))
        {
            entity = data.ContentData()?.Mind?.CurrentEntity;
        }
        else if (EntityUid.TryParse(args[0], out var foundEntity))
        {
            entity = foundEntity;
        }

        if (entity == null)
        {
            shell.WriteLine("Can't find entity.");
            return;
        }

        if (!entityManager.TryGetComponent<LawsComponent>(entity, out var laws))
        {
            shell.WriteLine("Entity has no laws.");
            return;
        }

        shell.WriteLine($"Laws for {entityManager.ToPrettyString(entity.Value)}:");
        foreach (var law in laws.Laws)
        {
            shell.WriteLine(law);
        }
    }
}

[AdminCommand(AdminFlags.Fun)]
public sealed class ClearLawsCommand : IConsoleCommand
{
    public string Command => "lawclear";
    public string Description => Loc.GetString("command-lawclear-description");
    public string Help => Loc.GetString("command-lawclear-help");
    public async void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        var entityManager = IoCManager.Resolve<IEntityManager>();
        var player = shell.Player as IPlayerSession;
        EntityUid? entity = null;

        if (args.Length == 0 && player != null)
        {
            entity = player.ContentData()?.Mind?.CurrentEntity;
        }
        else if (IoCManager.Resolve<IPlayerManager>().TryGetPlayerDataByUsername(args[0], out var data))
        {
            entity = data.ContentData()?.Mind?.CurrentEntity;
        }
        else if (EntityUid.TryParse(args[0], out var foundEntity))
        {
            entity = foundEntity;
        }

        if (entity == null)
        {
            shell.WriteLine("Can't find entity.");
            return;
        }

        if (!entityManager.TryGetComponent<LawsComponent>(entity.Value, out var laws))
        {
            shell.WriteLine("Entity has no laws component to clear");
            return;
        }

        entityManager.EntitySysManager.GetEntitySystem<LawsSystem>().ClearLaws(entity.Value, laws);
    }
}

[AdminCommand(AdminFlags.Fun)]
public sealed class AddLawCommand : IConsoleCommand
{
    public string Command => "lawadd";
    public string Description => Loc.GetString("command-lawadd-description");
    public string Help => Loc.GetString("command-lawadd-help");
    public async void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        var entityManager = IoCManager.Resolve<IEntityManager>();
        var player = shell.Player as IPlayerSession;
        EntityUid? entity = null;

        if (args.Length < 2 || args.Length > 3)
        {
            shell.WriteLine("Wrong number of arguments.");
            return;
        }

        if (IoCManager.Resolve<IPlayerManager>().TryGetPlayerDataByUsername(args[0], out var data))
        {
            entity = data.ContentData()?.Mind?.CurrentEntity;
        }
        else if (EntityUid.TryParse(args[0], out var foundEntity))
        {
            entity = foundEntity;
        }

        if (entity == null)
        {
            shell.WriteLine("Can't find entity.");
            return;
        }

        var laws = entityManager.EnsureComponent<LawsComponent>(entity.Value);

        if (args.Length == 2)
            entityManager.EntitySysManager.GetEntitySystem<LawsSystem>().AddLaw(entity.Value, args[1], component: laws);
        else if (args.Length == 3 && int.TryParse(args[2], out var index))
            entityManager.EntitySysManager.GetEntitySystem<LawsSystem>().AddLaw(entity.Value, args[1], index, laws);
        else
            shell.WriteLine("Third argument must be an integer.");
    }
}

[AdminCommand(AdminFlags.Fun)]
public sealed class RemoveLawCommand : IConsoleCommand
{
    public string Command => "lawrm";
    public string Description => Loc.GetString("command-lawrm-description");
    public string Help => Loc.GetString("command-lawrm-help");
    public async void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        var entityManager = IoCManager.Resolve<IEntityManager>();
        var player = shell.Player as IPlayerSession;
        EntityUid? entity = null;

        if (args.Length < 1 || args.Length > 2)
        {
            shell.WriteLine("Wrong number of arguments.");
            return;
        }

        if (IoCManager.Resolve<IPlayerManager>().TryGetPlayerDataByUsername(args[0], out var data))
        {
            entity = data.ContentData()?.Mind?.CurrentEntity;
        }
        else if (EntityUid.TryParse(args[0], out var foundEntity))
        {
            entity = foundEntity;
        }

        if (entity == null)
        {
            shell.WriteLine("Can't find entity.");
            return;
        }

        if (!entityManager.TryGetComponent<LawsComponent>(entity, out var laws))
        {
            shell.WriteLine("Entity has no laws to remove!");
            return;
        }

        if (args[1] == null || !int.TryParse(args[1], out var index))
            entityManager.EntitySysManager.GetEntitySystem<LawsSystem>().RemoveLaw(entity.Value);
        else
            entityManager.EntitySysManager.GetEntitySystem<LawsSystem>().RemoveLaw(entity.Value, index);
    }
}
