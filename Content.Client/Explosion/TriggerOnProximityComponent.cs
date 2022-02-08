using Content.Shared.Explosion;

namespace Content.Client.Explosion;

[RegisterComponent, Friend(typeof(TriggerSystem))]
public sealed class TriggerOnProximityComponent : SharedTriggerOnProximityComponent {}
