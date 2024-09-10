using Content.Shared.Explosion;
using Content.Shared.Explosion.EntitySystems;

namespace Content.Shared.Explosion.Components;

/// <summary>
/// Applies temporary explosion resistance to the entity.
/// Protects from the direct damage, but may not protect from things like fire.
/// </summary>
[RegisterComponent, Access(typeof(TemporaryExplosionResistantSystem))]
public sealed partial class TemporaryExplosionResistantComponent : Component
{
	[DataField(required: true)]
	public TimeSpan EndsAt;

	public TemporaryExplosionResistantComponent(TimeSpan endsAt)
	{
		EndsAt = endsAt;
	}
}
