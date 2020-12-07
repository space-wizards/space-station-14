using System.Collections.Generic;
using Content.Shared.Preferences;
using Robust.Server.Interfaces.Player;
using Robust.Shared.Network;
using Robust.Shared.Interfaces.GameObjects;

namespace Content.Server.GameTicking
{
    /// <summary>
    ///     A round-start setup preset, such as which antagonists to spawn.
    /// </summary>
    public abstract class GamePreset
    {
        public abstract bool Start(IReadOnlyList<IPlayerSession> readyPlayers, bool force = false);
        public virtual string ModeTitle => "Sandbox";
        public virtual string Description => "Secret!";
        public virtual bool DisallowLateJoin => false;
        public Dictionary<NetUserId, HumanoidCharacterProfile> readyProfiles;

        public virtual void OnGameStarted() { }

        /// <summary>
        /// Called when a player is spawned in (this includes, but is not limited to, before Start)
        /// </summary>
        public virtual void OnSpawnPlayerCompleted(IPlayerSession session, IEntity mob, bool lateJoin) { }

        public virtual string GetRoundEndDescription() => "";
    }
}
