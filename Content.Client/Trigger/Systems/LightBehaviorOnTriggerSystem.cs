using Content.Client.Light.EntitySystems;
using Content.Shared.Trigger;
using Content.Shared.Trigger.Components.Effects;
using Robust.Shared.Timing;

namespace Content.Client.Trigger.Systems;

/// <summary>
/// This handles...
/// </summary>
public sealed class LightBehaviorOnTriggerSystem : XOnTriggerSystem<LightBehaviorOnTriggerComponent>
{
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly LightBehaviorSystem _light = default!;

    protected override void OnTrigger(Entity<LightBehaviorOnTriggerComponent> ent, EntityUid target, ref TriggerEvent args)
    {
        if (_timing.IsFirstTimePredicted)
            _light.StartLightBehaviour(target, ent.Comp.Behavior);
    }
}
