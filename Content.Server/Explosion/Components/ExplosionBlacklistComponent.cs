using Content.Server.Explosion.EntitySystems;

namespace Content.Server.Explosion.Components;

/// <summary>
/// For explosion container recursion, lets specific containers be blacklisted.
/// Lets you prevent things like explosions destroying organs inside of a body.
/// </summary>
[RegisterComponent, Access(typeof(ExplosionSystem))]
public sealed partial class ExplosionBlacklistComponent : Component
{
    /// <summary>
    /// Container names to disable recursion for.
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public HashSet<string> Blacklist;
}
