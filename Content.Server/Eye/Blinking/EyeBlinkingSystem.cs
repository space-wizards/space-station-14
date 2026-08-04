using Content.Shared.Body;
using Content.Shared.Eye.Blinking;

namespace Content.Server.Eye.Blinking;

/// <inheritdoc/>
public sealed partial class EyeBlinkingSystem : SharedEyeBlinkingSystem
{
    [SubscribeLocalEvent]
    private void OnComponentInit(Entity<EyeBlinkingComponent> ent, ref ComponentInit args)
    {
        var body = ent.Owner;
        if (Comp<OrganComponent>(ent).Body is { } b)
            body = b;
        Dirty(ent);
    }
}
