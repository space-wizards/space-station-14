using System.Collections.Generic;
using Robust.Server.Interfaces.Player;

namespace Content.Server.GameTicking
{
    /// <summary>
    ///     A round-start setup preset, such as which antagonists to spawn.
    /// </summary>
    public abstract class GamePreset
    {
        public abstract bool Start(IReadOnlyList<IPlayerSession> players);
        public virtual string ModeTitle => "Sandbox";
        public virtual string Description => "Secret!";
    }
}
