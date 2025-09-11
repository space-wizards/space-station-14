namespace Content.Client.Sprite;

/// <summary>
/// This handles inheriting colors when an entity is spawned. Because entity spawns can be predicted, but can also not
/// be, we have to deal with this universally.
/// </summary>
public sealed class InheritColorOnSpawnSystem : EntitySystem;
