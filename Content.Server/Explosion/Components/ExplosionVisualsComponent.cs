using Content.Shared.Explosion;

namespace Content.Server.Explosion;

[RegisterComponent]
[ComponentReference(typeof(SharedExplosionVisualsComponent))]
public sealed partial class ExplosionVisualsComponent : SharedExplosionVisualsComponent
{
}
