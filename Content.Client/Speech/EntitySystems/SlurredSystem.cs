using Content.Shared.Speech.Components;
using Content.Shared.Speech.EntitySystems;

namespace Content.Client.Speech.EntitySystems;

public sealed class SlurredSystem : SharedSlurredSystem
{
    protected override string AccentuateInternal(EntityUid uid, SlurredAccentComponent comp, string message)
    {
        return message;
    }
}
