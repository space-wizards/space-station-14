namespace Content.Shared._Starlight.Language.Events;

/// <summary>
///     Raised on an entity when its list of Languages changes.
/// </summary>
/// <remarks>
///     This is raised both on the server and on the client.
///     The client raises it broadcast after receiving a new language comp state from the server.
/// </remarks>
public sealed class LanguagesUpdateEvent : EntityEventArgs
{
}