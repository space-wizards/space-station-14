namespace Content.Server.Explosion.Components;

/// <summary>
/// This is used for entities that want the more generic 'trigger' behavior after a step trigger occurs.
/// Not done by default, since it's not useful for everything and might cause weird behavior. But it is useful for a lot of stuff like mousetraps.
/// </summary>
[RegisterComponent]
public sealed partial class TriggerOnStepTriggerComponent : Component
{
}
