using Robust.Shared.Players;

public sealed class CommunicationConsoleUsed : EntityEventArgs
{
    public ICommonSession Session { get; }

    public CommunicationConsoleUsed(ICommonSession playerSession)
    {
        Session = playerSession;
    }
}
