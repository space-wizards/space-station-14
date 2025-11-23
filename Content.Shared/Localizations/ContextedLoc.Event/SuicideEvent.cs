using Content.Shared.Interaction.Events;
using Content.Shared.Localizations.Context;

namespace Content.Shared.Localizations;

public static partial class ContextedLoc
{
    public static IContext GetContext(SuicideEvent contextEvent) => new Target(contextEvent.Victim);
}
