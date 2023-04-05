using Content.Server.Shuttles.Components;

namespace Content.Server.Cargo.Components;

/// <summary>
/// Lets you remotely control the cargo shuttle.
/// </summary>
[RegisterComponent]
public sealed class CargoPilotConsoleComponent : Component
{
    /// <summary>
    /// <see cref="ShuttleConsoleComponent"/> that we're proxied into.
    /// </summary>
    public EntityUid? Entity;
}
