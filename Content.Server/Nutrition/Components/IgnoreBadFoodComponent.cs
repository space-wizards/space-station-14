using Content.Shared.Nutrition.EntitySystems;

namespace Content.Server.Nutrition.Components;

/// <summary>
/// This component allows NPC mobs to eat food with <see cref="BadIngestableComponent">.
/// See MobMouseAdmeme for usage.
/// </summary>
[RegisterComponent, Access(typeof(IngestionSystem))]
public sealed partial class IgnoreBadIngestableComponent : Component
{
}
