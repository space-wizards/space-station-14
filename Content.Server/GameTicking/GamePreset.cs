using System.Collections.Generic;
using Content.Shared.Preferences;
using Robust.Server.Interfaces.Player;
using Robust.Shared.Network;

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

        public virtual string GetRoundEndDescription() => "";
    }
}
