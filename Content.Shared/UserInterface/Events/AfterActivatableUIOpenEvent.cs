using Robust.Shared.Players;

namespace Content.Shared.UserInterface.Events;

[ByRefEvent]
public readonly record struct AfterActivatableUIOpenEvent(EntityUid User, ICommonSession Session)
{
    public readonly EntityUid User = User;
    public readonly ICommonSession Session = Session;
}
