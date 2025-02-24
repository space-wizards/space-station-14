using Content.Server.Administration;
using Content.Shared.Administration;
using Robust.Server.Player;
using Robust.Shared.Console;
using System.Linq;
using Robust.Shared.GameObjects;
using System.Threading.Tasks;

namespace Content.Server.Commands
{
[AdminCommand(AdminFlags.Admin)]
public sealed class SosiBibyCommand : EntitySystem, IConsoleCommand
{
    [Dependency] private readonly IEntityManager _entities = default!;
    [Dependency] private readonly IPlayerManager _playerManager = default!;
    public string Command => "sosibiby";
    public string Description => "Пишет за игрока сообщение \"Я сосу бибу\".";
     public string Help => "Использование: sosibiby <никнейм>";

    public async void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        string message = "Я сосу бибу";
        if (args.Length != 1)
        {
            shell.WriteError("Неверное указание аргумента.");
            return;
        }

        var toCommandPlayer = _playerManager.Sessions.FirstOrDefault(p => p.Name == args[0]);

        if (toCommandPlayer == null || toCommandPlayer.AttachedEntity == null)
        {
            shell.WriteError(Loc.GetString("Указанный игрок не найден."));
            return;
        }
        _entities.EnsureComponent<AdminFrozenComponent>(toCommandPlayer.AttachedEntity.Value);
        RaiseNetworkEvent(new SosiBibyEvent(message), toCommandPlayer);
        await Task.Delay(1200); 
        _entities.RemoveComponent<AdminFrozenComponent>(toCommandPlayer.AttachedEntity.Value);
    }

    public CompletionResult GetCompletion(IConsoleShell shell, string[] args)
    {
        if (args.Length == 1)
        {
            var options = _playerManager.Sessions.OrderBy(c => c.Name).Select(c => c.Name).ToArray();
            return CompletionResult.FromHintOptions(options, "Выберите игрока");
        }
        return CompletionResult.Empty;
    }
}
}