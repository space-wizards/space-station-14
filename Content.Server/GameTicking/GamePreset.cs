using System.Collections.Generic;
using Robust.Server.Interfaces.Player;
using Content.Shared.Preferences;

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
        public Dictionary<string, HumanoidCharacterProfile> readyProfiles;
    }
}
