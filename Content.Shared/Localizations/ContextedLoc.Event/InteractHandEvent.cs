using Content.Shared.Interaction;
using Content.Shared.Localizations.Context;

namespace Content.Shared.Localizations;

public static partial class ContextedLoc
{
    public static IContext GetContext(InteractHandEvent contextEvent, EntityUid item) => new UserItemTarget(contextEvent.User, item, contextEvent.Target);
}
