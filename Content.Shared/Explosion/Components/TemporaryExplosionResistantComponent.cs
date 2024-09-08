using Content.Shared.Explosion;
using Content.Shared.Explosion.EntitySystems;

namespace Content.Shared.Explosion.Components;

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
