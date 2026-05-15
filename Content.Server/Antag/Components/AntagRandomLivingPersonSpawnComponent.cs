using Content.Shared.Mind;
using Robust.Shared.Map;

namespace Content.Server.Antag.Components;

/// <summary>
/// Spawns this rule's antag on a random living crew member using <see cref="TargetSystem"/>'s <c>GetAllAliveHumans</c>.
/// Requires <see cref="AntagSelectionComponent"/>.
/// </summary>
[RegisterComponent]
public sealed partial class AntagRandomLivingPersonSpawnComponent : Component
{
    /// <summary>
    /// Entity that was picked.
    /// </summary>
    public EntityUid? Target;

    /// <summary>
    /// The entity's mind.
    /// </summary>
    public Entity<MindComponent>? Mind;
}
