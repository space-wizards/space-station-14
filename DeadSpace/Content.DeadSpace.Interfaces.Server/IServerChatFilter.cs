using System.Diagnostics.CodeAnalysis;
using Robust.Shared.GameObjects;
using Robust.Shared.Player;

namespace Content.DeadSpace.Interfaces.Server;

public interface IServerChatFilter
{
    string ReplaceWords(string message);
    bool NotAllowedMessage(EntityUid source, string message);
    bool NotAllowedMessage(ICommonSession source, string message);
    bool NotAllowedMessage(string message);
}
