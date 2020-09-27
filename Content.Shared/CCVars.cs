using Robust.Shared;
using Robust.Shared.Configuration;

namespace Content.Shared
{
    // ReSharper disable once InconsistentNaming
    [CVarDefs]
    public sealed class CCVars : CVars
    {
        public static readonly CVarDef<bool>
            GameLobbyEnabled = CVarDef.Create("game.lobbyenabled", false, CVar.ARCHIVE);

        public static readonly CVarDef<int>
            GameLobbyDuration = CVarDef.Create("game.lobbyduration", 20, CVar.ARCHIVE);

        public static readonly CVarDef<string>
            GameLobbyDefaultPreset = CVarDef.Create("game.defaultpreset", "Suspicion", CVar.ARCHIVE);

        public static readonly CVarDef<string>
            GameLobbyFallbackPreset = CVarDef.Create("game.fallbackpreset", "Sandbox", CVar.ARCHIVE);

        public static readonly CVarDef<bool>
            GameLobbyEnableWin = CVarDef.Create("game.enablewin", true, CVar.ARCHIVE);
    }
}
