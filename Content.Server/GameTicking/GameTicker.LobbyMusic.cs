using Robust.Shared.Audio;
using Robust.Shared.Random;
using Robust.Shared.Utility;

namespace Content.Server.GameTicking
{
    public sealed partial class GameTicker
    {
        private const string LobbyMusicCollection = "LobbyMusic";

        [ViewVariables]
        private bool _lobbyMusicInitialized = false;

        [ViewVariables]
        private SoundCollectionPrototype _lobbyMusicCollection = default!;

        [ViewVariables]
        public string? LobbySong { get; private set; }

        private void InitializeLobbyMusic()
        {
            DebugTools.Assert(!_lobbyMusicInitialized);
            _lobbyMusicCollection = _prototypeManager.Index<SoundCollectionPrototype>(LobbyMusicCollection);

            // Now that the collection is set, the lobby music has been initialized and we can choose a random song.
            _lobbyMusicInitialized = true;

            ChooseRandomLobbySong();
        }

        /// <summary>
        ///     Sets the current lobby song, or stops it if null.
        /// </summary>
        /// <param name="song">The lobby song to play, or null to stop any lobby songs.</param>
        public void SetLobbySong(string? song)
        {
            DebugTools.Assert(_lobbyMusicInitialized);

            if (song == null)
            {
                LobbySong = null;
                return;
                // TODO GAMETICKER send song stop event
            }

            LobbySong = song;
            // TODO GAMETICKER send song change event
        }

        /// <summary>
        ///     Plays a random song from the LobbyMusic sound collection.
        /// </summary>
        public void ChooseRandomLobbySong()
        {
            DebugTools.Assert(_lobbyMusicInitialized);
            SetLobbySong(_robustRandom.Pick(_lobbyMusicCollection.PickFiles).ToString());
        }

        /// <summary>
        ///     Stops the current lobby song being played.
        /// </summary>
        public void StopLobbySong()
        {
            SetLobbySong(null);
        }
    }
}
