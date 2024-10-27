using Robust.Shared.Serialization;

namespace Content.Shared._EinsteinEngine.Language.Events
{
    /// <summary>
    ///     Sent to the client when its list of languages changes.
    ///     The client should in turn update its HUD and relevant systems.
    /// </summary>
    [Serializable, NetSerializable]
    public sealed class LanguagesUpdatedMessage(string currentLanguage, List<string> spoken, List<string> understood) : EntityEventArgs
    {
        public string CurrentLanguage = currentLanguage;
        public List<string> Spoken = spoken;
        public List<string> Understood = understood;
    }
}
