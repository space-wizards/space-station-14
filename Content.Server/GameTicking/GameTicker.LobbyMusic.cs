using Content.Shared.Audio;
using Robust.Shared.ContentPack;
using Robust.Shared.IoC;
using Robust.Shared.Log;
using Robust.Shared.Random;
using Robust.Shared.Utility;
using Robust.Shared.ViewVariables;

namespace Content.Server.GameTicking
{
    public partial class GameTicker
    {
        [Dependency] private readonly IResourceManager _resourceManager = default!;
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

            if (!_resourceManager.ContentFileExists(song))
            {
                Logger.ErrorS("ticker", $"Tried to set lobby song to \"{song}\", which doesn't exist!");
                return;
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
            SetLobbySong(_robustRandom.Pick(_lobbyMusicCollection.PickFiles));
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
