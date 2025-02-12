using Content.Shared.Explosion.Components;

namespace Content.Client.Explosion;

[RegisterComponent, Access(typeof(TriggerSystem))]
public sealed partial class TriggerOnProximityComponent : SharedTriggerOnProximityComponent {}
