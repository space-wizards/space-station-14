using Robust.Shared.Console;
using Robust.Shared.Interfaces.Network;

namespace Content.Server.Interfaces.Chat
{
    public interface IChatCommand : ICommand
    {
        void Execute(IChatManager manager, INetChannel client, params string[] args);
    }
}
