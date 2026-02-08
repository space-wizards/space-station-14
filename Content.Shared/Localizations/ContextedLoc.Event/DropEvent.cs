using Content.Shared.Interaction.Events;
using Content.Shared.Localizations.Context;

namespace Content.Shared.Localizations;

public static partial class ContextedLoc
{
    public static IContext GetContext(DroppedEvent contextEvent, EntityUid what) => new UserTarget(contextEvent.User, what);
}
