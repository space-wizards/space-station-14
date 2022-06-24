using Robust.Shared.Players;

namespace Content.Shared.Movement.Events;

public sealed class RelayMoveInputEvent : EntityEventArgs
{
    public ICommonSession Session { get; }

    public RelayMoveInputEvent(ICommonSession session)
    {
        Session = session;
    }
}