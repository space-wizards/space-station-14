using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Map;
using System;
using System.Collections.Generic;
using System.Text;

namespace Content.Server.Interfaces
{
    /// <summary>
    ///     Interface for objects such as radios meant to have an effect when speech is heard. Requires ListeningComponent to be on the attached entity.
    /// </summary>
    public interface IListen
    {
        void HeardSpeech(string speech, IEntity source);

        int GetListenRange();

        GridCoordinates GetListenerPosition();
    }
}
