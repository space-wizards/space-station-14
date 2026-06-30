using Content.Shared.Speech.Components;
using Content.Shared.Speech.EntitySystems;

namespace Content.Client.Speech.EntitySystems;

public sealed class StutteringSystem : SharedStutteringSystem
{
    protected override string AccentuateInternal(EntityUid uid, StutteringAccentComponent comp, string message)
    {
        return message;
    }
}
