using Robust.Shared.Serialization;

namespace Content.Shared._EinsteinEngine.Language.Events
{
    /// <summary>
    ///     Sent from the client to the server when it needs to want to set his currentLangauge.
    ///     Yeah im using this instead of ExecuteCommand... Better right?
    /// </summary>
    [Serializable, NetSerializable]
    public sealed class LanguagesSetMessage(string currentLanguage) : EntityEventArgs
    {
        public string CurrentLanguage = currentLanguage;
    }
}
