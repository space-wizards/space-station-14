using Content.Shared.Damage;

namespace Content.Server.Body.Components;

// TODO replace with better simulation of organs.

/// <summary>
///     If this organ is removed from a body,
///     then kill the body.
/// </summary>
[RegisterComponent]
public class VitalOrganComponent : Component
{
}
