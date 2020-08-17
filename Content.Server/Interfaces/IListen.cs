using Robust.Shared.Interfaces.GameObjects;

namespace Content.Server.Interfaces
{
    /// <summary>
    ///     Interface for objects such as radios meant to have an effect when speech is heard.
    /// </summary>
    public interface IListen
    {
        void HeardSpeech(string speech, IEntity source);

        int GetListenRange();
    }
}
