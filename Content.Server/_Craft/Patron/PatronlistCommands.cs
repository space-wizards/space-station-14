using System.Linq;
using Content.Server.Administration;
using Content.Shared.Administration;
using Content.Server.Database;
using Robust.Server.Player;
using Robust.Shared.Console;
using Robust.Shared.Prototypes;

namespace Content.Server.Patron
{
    [AdminCommand(AdminFlags.Host)]
    public sealed class PatronAddCommand : EntitySystem, IConsoleCommand
    {
        public string Command => "patronadd";
        public string Description => Loc.GetString("cmd-patronadd-desc");
        public string Help => Loc.GetString("cmd-patronadd-help");

        public async void Execute(IConsoleShell shell, string argStr, string[] args)
        {
            var locator = IoCManager.Resolve<IPlayerLocator>();
            var dbMan = IoCManager.Resolve<IServerDbManager>();
            var protoMan = IoCManager.Resolve<IPrototypeManager>();

            if (args.Length == 0)
            {
                shell.WriteLine($"Invalid amount of arguments.{Help}");
                return;
            }

            string target = args[0];
            string[]? items = null;

            if (args.Length > 1)
            {
                var itemlist = new List<string>();
                for(int i = 1; i < args.Length; i++)
                {
                    var item = args[i];
                    if (!protoMan.HasIndex<EntityPrototype>(item))
                    {
                        shell.WriteError(Loc.GetString("cmd-patron-err-invalidproto",("Proto",item)));
                        continue;
                    }
                    itemlist.Add(item);
                }

                items = itemlist.ToArray();
            }

            var located = await locator.LookupIdByNameAsync(target);
            if (located == null)
            {
                shell.WriteError(Loc.GetString("cmd-patron-err-playerloc"));
                return;
            }

            var userId = located.UserId;

            if (await dbMan.IsInPatronlistAsync(userId))
            {
                if (items is not null)
                    await dbMan.AddPatronItemsAsync(userId,items);
                else
                {
                    shell.WriteError(Loc.GetString("cmd-patron-err-plyexistnoargs"));
                    return;
                }
            }
            else
            {
                if (items is not null)
                    await dbMan.InitPatronlistAsync(userId, items);
                else
                    await dbMan.InitPatronlistAsync(userId);
            }
        }
        public CompletionResult GetCompletion(IConsoleShell shell, string[] args)
        {
            if (args.Length == 1)
            {
                var playerMgr = IoCManager.Resolve<IPlayerManager>();
                var options = playerMgr.ServerSessions.Select(c => c.Name).OrderBy(c => c).ToArray();
                return CompletionResult.FromHintOptions(options, Loc.GetString("cmd-patron-hint-player"));
            }

            if (args.Length > 1)
            {
                var protos = CompletionHelper.PrototypeIDs<EntityPrototype>();
                return CompletionResult.FromHintOptions(protos, Loc.GetString("cmd-patron-hint-proto"));
            }

            return CompletionResult.Empty;
        }
    }

    [AdminCommand(AdminFlags.Host)]
    public sealed class PatronRemoveCommand : EntitySystem, IConsoleCommand
    {
        public string Command => "patronremove";
        public string Description => Loc.GetString("cmd-patronremove-desc");
        public string Help => Loc.GetString("cmd-patronremove-help");

        public async void Execute(IConsoleShell shell, string argStr, string[] args)
        {
            var locator = IoCManager.Resolve<IPlayerLocator>();
            var dbMan = IoCManager.Resolve<IServerDbManager>();

            if (args.Length == 0)
            {
                shell.WriteLine($"Invalid amount of arguments.{Help}");
                return;
            }

            string target = args[0];
            string[]? items = null;
            bool deleteComply = false;

            if (args.Length > 1)
            {
                if (args[1] == "-nukepatron")
                    deleteComply = true;
                else
                {
                    var itemlist = new List<string>();
                    for (int i = 1; i < args.Length; i++)
                        itemlist.Add(args[i]);

                    items = itemlist.ToArray();
                }
            }

            var located = await locator.LookupIdByNameAsync(target);
            if (located == null)
            {
                shell.WriteError(Loc.GetString("cmd-patron-err-playerloc"));
                return;
            }

            var userId = located.UserId;

            if (await dbMan.IsInPatronlistAsync(userId))
            {
                if (items is not null)
                    await dbMan.RemovePatronItemsAsync(userId, items);
                else if (deleteComply)
                    await dbMan.RemovePatronlistAsync(userId);
                else
                {
                    shell.WriteError(Loc.GetString("cmd-patron-err-notnukecomply"));
                    return;
                }
            }
            else
            {
                shell.WriteError(Loc.GetString("cmd-patron-err-plynotexist"));
                return;
            }
        }
        public CompletionResult GetCompletion(IConsoleShell shell, string[] args)
        {
            if (args.Length == 1)
            {
                var playerMgr = IoCManager.Resolve<IPlayerManager>();
                var options = playerMgr.ServerSessions.Select(c => c.Name).OrderBy(c => c).ToArray();
                return CompletionResult.FromHintOptions(options, Loc.GetString("cmd-patron-hint-player"));
            }

            if (args.Length == 2)
                if (args[1].Length > 0 && args[1][0] == '-')
                    return CompletionResult.Empty;

            if (args.Length > 1)
            {
                var protos = CompletionHelper.PrototypeIDs<EntityPrototype>();
                return CompletionResult.FromHintOptions(protos, Loc.GetString("cmd-patron-hint-proto"));
            }

            return CompletionResult.Empty;
        }
    }

    [AdminCommand(AdminFlags.Host)]
    public sealed class PatronGetItemsCommand : EntitySystem, IConsoleCommand
    {
        public string Command => "patron_getitems";
        public string Description => Loc.GetString("cmd-patron-getitems-desc");
        public string Help => Loc.GetString("cmd-patron-getitems-help");

        public async void Execute(IConsoleShell shell, string argStr, string[] args)
        {
            var locator = IoCManager.Resolve<IPlayerLocator>();
            var dbMan = IoCManager.Resolve<IServerDbManager>();

            if (args.Length == 0)
            {
                shell.WriteLine($"Invalid amount of arguments.{Help}");
                return;
            }

            string target = args[0];
            var located = await locator.LookupIdByNameAsync(target);
            if (located == null)
            {
                shell.WriteError(Loc.GetString("cmd-patron-err-playerloc"));
                return;
            }

            var userId = located.UserId;

            if (await dbMan.IsInPatronlistAsync(userId))
            {
                var items = await dbMan.GetPatronItemsAsync(userId);
                if (items is not null)
                {
                    foreach (var item in items)
                        shell.WriteLine(item);
                }
                else
                {
                    shell.WriteError(Loc.GetString("cmd-patron-err-noplyitemlist"));
                    return;
                }
            }
            else
            {
                shell.WriteError(Loc.GetString("cmd-patron-err-plynotexist"));
                return;
            }
        }
        public CompletionResult GetCompletion(IConsoleShell shell, string[] args)
        {
            if (args.Length == 1)
            {
                var playerMgr = IoCManager.Resolve<IPlayerManager>();
                var options = playerMgr.ServerSessions.Select(c => c.Name).OrderBy(c => c).ToArray();
                return CompletionResult.FromHintOptions(options, Loc.GetString("cmd-patronadd-hint-player"));
            }
            return CompletionResult.Empty;
        }
    }
}
