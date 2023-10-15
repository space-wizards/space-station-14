using Robust.Shared.Players;

namespace Content.Shared.UserInterface;

public sealed class AfterActivatableUIOpenEvent : EntityEventArgs
{
    public readonly EntityUid User;
    public readonly ICommonSession Session;

    public AfterActivatableUIOpenEvent(EntityUid who, ICommonSession session)
    {
        User = who;
        Session = session;
    }
}
