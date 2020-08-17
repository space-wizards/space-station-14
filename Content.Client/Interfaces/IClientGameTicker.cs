using System;
using System.Collections.Generic;

namespace Content.Client.Interfaces
{
    public interface IClientGameTicker
    {
        bool IsGameStarted { get; }
        string ServerInfoBlob { get; }
        bool AreWeReady { get; }
        DateTime StartTime { get; }
        bool Paused { get; }
        Dictionary<string, bool> Ready { get; }

        void Initialize();
        event Action InfoBlobUpdated;
        event Action LobbyStatusUpdated;
        event Action LobbyReadyUpdated;
    }
}
