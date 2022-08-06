using Content.Shared.Explosion;

namespace Content.Client.Explosion;

[RegisterComponent, Access(typeof(TriggerSystem))]
public sealed class TriggerOnProximityComponent : SharedTriggerOnProximityComponent {}
