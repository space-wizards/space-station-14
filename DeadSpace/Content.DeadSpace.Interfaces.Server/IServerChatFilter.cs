using System.Diagnostics.CodeAnalysis;
using Robust.Shared.GameObjects;
using Robust.Shared.Player;

namespace Content.DeadSpace.Interfaces.Server;

public interface IServerChatFilter
{
    public string ReplaceWords(string message);
    public bool NotAllowedMessage(EntityUid source, string message);
    public bool NotAllowedMessage(ICommonSession source, string message);
    public bool NotAllowedMessage(string message);
}
