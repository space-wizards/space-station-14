using Content.Shared.DeviceLinking.Components;
using Content.Shared.DeviceLinking.Events;
using Content.Shared.EntityEffects;

namespace Content.Shared.DeviceLinking.Systems;

public sealed partial class EntityEffectOnDeviceOverloadSystem : EntitySystem
{
    [Dependency] private SharedEntityEffectsSystem _effects = default!;

    [SubscribeLocalEvent]
    private void OnEffectsOverload(Entity<EntityEffectOnDeviceOverloadComponent> ent, ref DeviceLinkOverloadedEvent args)
    {
        _effects.ApplyEffects(ent, ent.Comp.Effects);
    }
}
