using System.Collections.Generic;
using Robust.Server.Player;
using Robust.Shared.Map;

namespace Content.Server.Tabletop
{
    /// <summary>
    /// A struct for storing data about a running tabletop game.
    /// </summary>
    public struct TabletopSession
    {
        /// <summary>
        /// The map ID associated with this tabletop game session.
        /// </summary>
        public MapId MapId;

        /// <summary>
        /// The set of players currently playing this tabletop game.
        /// </summary>
        private readonly HashSet<IPlayerSession> _currentPlayers;

        /// <param name="mapId">The map ID associated with this tabletop game.</param>
        public TabletopSession(MapId mapId)
        {
            MapId = mapId;
            _currentPlayers = new();
        }

        /// <summary>
        /// Returns true if the given player is currently playing this tabletop game.
        /// </summary>
        public bool IsPlaying(IPlayerSession playerSession)
        {
            return _currentPlayers.Contains(playerSession);
        }

        /// <summary>
        /// Store that this player has started playing this tabletop game. If the player was already playing, nothing
        /// happens.
        /// </summary>
        public void StartPlaying(IPlayerSession playerSession)
        {
            _currentPlayers.Add(playerSession);
        }

        /// <summary>
        /// Store that this player has stopped playing this tabletop game. If the player was not playing, nothing
        /// happens.
        /// </summary>
        public void StopPlaying(IPlayerSession playerSession)
        {
            _currentPlayers.Remove(playerSession);
        }
    }
}
