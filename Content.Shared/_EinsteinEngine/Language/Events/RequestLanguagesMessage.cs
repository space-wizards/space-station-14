using Robust.Shared.Serialization;

namespace Content.Shared._EinsteinEngine.Language.Events
{
    /// <summary>
    ///     Sent from the client to the server when it needs to learn the list of languages its entity knows.
    ///     This event should always be followed by a <see cref="LanguagesUpdatedMessage"/>, unless the client doesn't have an entity.
    /// </summary>
    [Serializable, NetSerializable]
    public sealed class RequestLanguagesMessage : EntityEventArgs;
}
