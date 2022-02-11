namespace Content.Shared.Identity;

/// <remarks>
///     This should maybe be merged with IngestionBlocker but they don't quite mean the same thing,
///     since breath masks block ingestion but don't block identity.
/// </remarks>
[RegisterComponent, Friend(typeof(Systems.IdentitySystem))]
public sealed class IdentityBlockerComponent : Component
{
}
