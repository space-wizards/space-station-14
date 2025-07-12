using Content.Server.Speech;
using Content.Shared.StatusEffectNew;
using Content.Shared.StatusEffectNew.Components;

namespace Content.Server.StatusEffectNew;

/// <inheritdoc/>
public sealed partial class StatusEffectsSystem : SharedStatusEffectsSystem
{
    public override void Initialize()
    {
        base.Initialize();

        // Serverside relays
        SubscribeLocalEvent<StatusEffectContainerComponent, AccentGetEvent>(RelayStatusEffectEvent);
    }
}
